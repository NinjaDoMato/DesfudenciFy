<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import {
  formatDate,
  formatMoney,
  parseDateForSort,
  todayDateInputValue,
  toDateInputValue,
  type BankAccount,
  type DashboardTotals,
  type Investment,
  type InvestmentType,
  type Reserve,
} from '@/types'
import DateInput from '@/components/DateInput.vue'
import { computeInvestmentTotals, computeInvestidoTotals, computeReserveTotals } from '@/utils/totals'
import MoneyInput from '@/components/MoneyInput.vue'
import { DisponivelInvestimentoKpi, TotalInvestidoKpi } from '@/components/totals'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import LiquidationModal from '@/components/LiquidationModal.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

interface AllocationRow {
  sourceId: string
  amount: number
}

type SourceRow = { sourceKey: string; reserveId: string | null; amount: number }

const FREE_SOURCE = '__free__'

const route = useRoute()
const router = useRouter()
const toast = useToastStore()
const items = ref<Investment[]>([])
const reserves = ref<Reserve[]>([])
const bankAccounts = ref<BankAccount[]>([])
const types = ref<InvestmentType[]>([])
const dashboardTotals = ref<DashboardTotals | null>(null)
const freeBalanceAvailable = ref(0)
const filters = reactive({
  name: '',
  bankAccountId: '',
  investmentTypeId: '',
  reserveId: '',
})

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

function applyRouteFilters() {
  const reserveId = route.query.reserveId
  filters.reserveId = typeof reserveId === 'string' ? reserveId : ''
}

watch(
  () => route.query.reserveId,
  () => applyRouteFilters(),
)
const showForm = ref(false)
const editingId = ref<string | null>(null)
const showAmount = ref<Investment | null>(null)
const currentAmount = ref(0)
const showDetails = ref<Investment | null>(null)
const showLiquidation = ref<Investment | null>(null)
const originalAllocations = ref<AllocationRow[]>([])

const form = reactive({
  name: '',
  rentability: '',
  startDate: todayDateInputValue(),
  endDate: '',
  bankAccountId: '',
  investmentTypeId: '',
})

const allocations = ref<AllocationRow[]>([{ sourceId: '', amount: 0 }])

const totalAllocated = computed(() =>
  allocations.value.reduce((sum, row) => sum + (Number(row.amount) || 0), 0),
)

function toSourceRows(item: Investment): SourceRow[] {
  return item.sourceReserves.map((s) => ({
    sourceKey: s.reserveId ?? FREE_SOURCE,
    reserveId: s.reserveId ?? null,
    amount: s.amount,
  }))
}

const detailRows = computed(() => (showDetails.value ? toSourceRows(showDetails.value) : []))

const columns: DataTableColumn<Investment>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'investmentTypeName', label: 'Tipo', sortValue: (row) => row.investmentTypeName },
  { key: 'rentability', label: 'Rentabilidade', sortValue: (row) => row.rentability },
  { key: 'bankAccountName', label: 'Conta', sortValue: (row) => row.bankAccountName },
  { key: 'reserves', label: 'Origens', sortValue: (row) => row.sourceReserves.length },
  { key: 'startAmount', label: 'Inicial', sortValue: (row) => row.startAmount },
  { key: 'currentAmount', label: 'Atual', sortValue: (row) => row.currentAmount },
  { key: 'endDate', label: 'Fim', sortValue: (row) => parseDateForSort(row.endDate) },
  { key: 'actions', label: '', sortable: false },
]

const screenTotals = computed(() => computeInvestmentTotals(items.value))
const reserveTotals = computed(() =>
  computeReserveTotals(
    dashboardTotals.value?.totalFreeBalance ?? freeBalanceAvailable.value,
    dashboardTotals.value?.totalInvested ?? 0,
    reserves.value,
  ),
)

const investido = computed(() => {
  if (!dashboardTotals.value) return null
  const d = dashboardTotals.value
  return computeInvestidoTotals(
    d.totalInvested,
    d.totalInvestedFromFree,
    d.totalInvestedFromReserves,
    d.retainedProfit,
  )
})

const filteredItems = computed(() => {
  const nameTerm = filters.name.trim().toLowerCase()
  return items.value.filter((item) => {
    if (nameTerm && !item.name.toLowerCase().includes(nameTerm)) return false
    if (filters.bankAccountId && item.bankAccountId !== filters.bankAccountId) return false
    if (filters.investmentTypeId && item.investmentTypeId !== filters.investmentTypeId) return false
    if (filters.reserveId === FREE_SOURCE) {
      return item.sourceReserves.some((s) => !s.reserveId)
    }
    if (filters.reserveId && !item.sourceReserves.some((s) => s.reserveId === filters.reserveId)) return false
    return true
  })
})

const detailColumns: DataTableColumn<SourceRow>[] = [
  { key: 'reserveId', label: 'Origem', sortValue: (row) => sourceLabel(row.reserveId) },
  { key: 'amount', label: 'Valor investido', sortValue: (row) => row.amount },
  {
    key: 'proportion',
    label: 'Proporção',
    sortValue: (row) => {
      const start = showDetails.value?.startAmount ?? 0
      return start > 0 ? row.amount / start : 0
    },
  },
]

function sourceLabel(reserveId: string | null | undefined) {
  if (!reserveId) return 'Saldo livre'
  return reserves.value.find((r) => r.id === reserveId)?.name || reserveId
}

function sourceIdFromAllocation(reserveId: string | null | undefined) {
  return reserveId || FREE_SOURCE
}

function originalAmountFor(sourceId: string) {
  return originalAllocations.value.find((a) => a.sourceId === sourceId)?.amount ?? 0
}

function editableAvailable(sourceId: string) {
  if (sourceId === FREE_SOURCE) {
    return freeBalanceAvailable.value + originalAmountFor(FREE_SOURCE)
  }
  const base = reserves.value.find((r) => r.id === sourceId)?.availableValue ?? 0
  return base + originalAmountFor(sourceId)
}

function isSourceTaken(sourceId: string, rowIndex: number) {
  return allocations.value.some((row, index) => index !== rowIndex && row.sourceId === sourceId)
}

function availableReservesFor(rowIndex: number) {
  return reserves.value.filter((r) => !isSourceTaken(r.id, rowIndex) || allocations.value[rowIndex]?.sourceId === r.id)
}

function freeSourceAvailable(rowIndex: number) {
  return !isSourceTaken(FREE_SOURCE, rowIndex) || allocations.value[rowIndex]?.sourceId === FREE_SOURCE
}

function resetForm() {
  Object.assign(form, {
    name: '',
    rentability: '',
    startDate: todayDateInputValue(),
    endDate: '',
    bankAccountId: '',
    investmentTypeId: '',
  })
  allocations.value = [{ sourceId: '', amount: 0 }]
  originalAllocations.value = []
  editingId.value = null
}

function openCreate() {
  resetForm()
  showForm.value = true
}

function openEdit(item: Investment) {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    rentability: item.rentability || '',
    startDate: toDateInputValue(item.startDate),
    endDate: toDateInputValue(item.endDate),
    bankAccountId: item.bankAccountId,
    investmentTypeId: item.investmentTypeId,
  })
  allocations.value = item.sourceReserves.map((s) => ({
    sourceId: sourceIdFromAllocation(s.reserveId),
    amount: s.amount,
  }))
  originalAllocations.value = item.sourceReserves.map((s) => ({
    sourceId: sourceIdFromAllocation(s.reserveId),
    amount: s.amount,
  }))
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  resetForm()
}

function addAllocation() {
  allocations.value.push({ sourceId: '', amount: 0 })
}

function removeAllocation(index: number) {
  if (allocations.value.length === 1) return
  allocations.value.splice(index, 1)
}

function formatSources(item: Investment) {
  return item.sourceReserves
    .map((s) => `${sourceLabel(s.reserveId)}: ${formatMoney(s.amount)}`)
    .join(' · ')
}

function buildSourceReserves() {
  return allocations.value
    .filter((row) => row.sourceId && Number(row.amount) > 0)
    .map((row) => ({
      reserveId: row.sourceId === FREE_SOURCE ? null : row.sourceId,
      amount: Number(row.amount),
    }))
}

function validateAllocations(sourceReserves: { reserveId: string | null; amount: number }[]) {
  if (sourceReserves.length === 0) {
    toast.error('Informe ao menos uma origem com valor (saldo livre ou reserva).')
    return false
  }

  const freeCount = sourceReserves.filter((s) => !s.reserveId).length
  if (freeCount > 1) {
    toast.error('Saldo livre só pode aparecer uma vez.')
    return false
  }

  const reserveIds = sourceReserves.map((s) => s.reserveId).filter((id): id is string => !!id)
  if (new Set(reserveIds).size !== reserveIds.length) {
    toast.error('Cada reserva só pode aparecer uma vez.')
    return false
  }

  for (const source of sourceReserves) {
    const sourceId = sourceIdFromAllocation(source.reserveId)
    if (source.amount > editableAvailable(sourceId)) {
      toast.error(`Saldo insuficiente em ${sourceLabel(source.reserveId)}.`)
      return false
    }
  }

  return true
}

async function load() {
  const [inv, res, banks, t, free, dashboard] = await Promise.all([
    api.get<Investment[]>('/investments'),
    api.get<Reserve[]>('/reserves'),
    api.get<BankAccount[]>('/lookups/bank-accounts'),
    api.get<InvestmentType[]>('/lookups/investment-types'),
    api.get<{ amount: number }>('/entries/free-balance'),
    api.get<DashboardTotals>('/dashboard/totals'),
  ])
  items.value = inv.data
  reserves.value = res.data
  bankAccounts.value = banks.data
  types.value = t.data
  freeBalanceAvailable.value = free.data.amount
  dashboardTotals.value = dashboard.data
}

const activeBankAccounts = computed(() => bankAccounts.value.filter((x) => x.isActive))
const activeTypes = computed(() => types.value.filter((x) => x.isActive))

async function save() {
  const sourceReserves = buildSourceReserves()
  if (!validateAllocations(sourceReserves)) return

  const payload = {
    name: form.name,
    rentability: form.rentability.trim(),
    startDate: form.startDate,
    endDate: form.endDate || null,
    bankAccountId: form.bankAccountId,
    investmentTypeId: form.investmentTypeId,
    sourceReserves,
  }

  try {
    if (editingId.value) {
      await api.put(`/investments/${editingId.value}`, payload)
    } else {
      await api.post('/investments', payload)
    }
    closeForm()
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function updateAmount() {
  if (!showAmount.value) return
  try {
    await api.put(`/investments/${showAmount.value.id}/current-amount`, {
      currentAmount: Number(currentAmount.value),
    })
    showAmount.value = null
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

function openLiquidation(item: Investment) {
  showLiquidation.value = item
}

function onLiquidated() {
  showLiquidation.value = null
  void load().catch((error) => {
    toastError(error, 'Erro')
  })
}

onMounted(async () => {
  applyRouteFilters()
  try {
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h1>Investimentos</h1>
        <p class="muted">Investimentos de renda fixa criados usando saldo livre e/ou saldo reservado.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Novo investimento</button>
    </div>
    <div class="grid grid-4">
      <div class="kpi">
        <div class="label">Número de investimentos</div>
        <div class="value">{{ screenTotals.count }}</div>
      </div>
      <DisponivelInvestimentoKpi
        :disponivel-para-investimento="reserveTotals.disponivelParaInvestimento"
        :total-disponivel-reservas="reserveTotals.totalDisponivelReservas"
        :saldo-livre="reserveTotals.saldoLivre"
      />
      <TotalInvestidoKpi v-if="investido" :data="investido" />
    </div>
    <div class="panel">
      <div class="filters">
        <div class="field">
          <label>Nome</label>
          <input v-model="filters.name" type="search" placeholder="Filtrar por nome" />
        </div>
        <div class="field">
          <label>Conta bancária</label>
          <select v-model="filters.bankAccountId">
            <option value="">Todas</option>
            <option v-for="b in bankAccounts" :key="b.id" :value="b.id">{{ b.name }}</option>
          </select>
        </div>
        <div class="field">
          <label>Tipo</label>
          <select v-model="filters.investmentTypeId">
            <option value="">Todos</option>
            <option v-for="t in types" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select>
        </div>
        <div class="field">
          <label>Origem</label>
          <select v-model="filters.reserveId">
            <option value="">Todas</option>
            <option :value="FREE_SOURCE">Saldo livre</option>
            <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>
      </div>
      <DataTable :rows="filteredItems" :columns="columns" row-key="id" initial-sort-key="name">
        <template #cell-reserves="{ row }">
          <button class="linkish" type="button" @click="showDetails = row">
            {{ row.sourceReserves.length }} origem(ns)
          </button>
        </template>
        <template #cell-rentability="{ row }">{{ row.rentability || '-' }}</template>
        <template #cell-startAmount="{ row }">{{ formatMoney(row.startAmount) }}</template>
        <template #cell-currentAmount="{ row }">{{ formatMoney(row.currentAmount) }}</template>
        <template #cell-endDate="{ row }">
          {{ formatDate(row.endDate) || '-' }}
        </template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton
              label="Detalhes"
              icon="details"
              @click="router.push({ name: 'investment-detail', params: { id: row.id } })"
            />
            <IconButton label="Editar" icon="edit" @click="openEdit(row)" />
            <IconButton
              label="Valor atual"
              icon="amount"
              @click="showAmount = row; currentAmount = row.currentAmount"
            />
            <IconButton label="Liquidar" icon="liquidate" variant="primary" @click="openLiquidation(row)" />
          </div>
        </template>
      </DataTable>
    </div>

    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <form class="modal wide" @submit.prevent="save">
        <h2>{{ editingId ? 'Editar' : 'Novo' }} investimento</h2>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field">
          <label>Rentabilidade</label>
          <input v-model="form.rentability" maxlength="100" placeholder="Ex.: 100% CDI, 12% a.a." />
          <p class="muted">Apenas registro — não entra em nenhum cálculo.</p>
        </div>
        <div class="field"><label>Início</label><DateInput v-model="form.startDate" required /></div>
        <div class="field"><label>Fim (opcional)</label><DateInput v-model="form.endDate" /></div>
        <div class="field">
          <label>Conta</label>
          <select v-model="form.bankAccountId" required>
            <option disabled value="">Selecione</option>
            <option v-for="b in activeBankAccounts" :key="b.id" :value="b.id">{{ b.name }}</option>
          </select>
        </div>
        <div class="field">
          <label>Tipo</label>
          <select v-model="form.investmentTypeId" required>
            <option disabled value="">Selecione</option>
            <option v-for="t in activeTypes" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select>
        </div>

        <div class="allocations">
          <div class="allocations-header">
            <h3>Origens do capital</h3>
            <button class="btn secondary" type="button" @click="addAllocation">Adicionar origem</button>
          </div>
          <p class="muted">
            {{ editingId
              ? 'Use saldo livre e/ou montinhos. O valor já aplicado neste investimento permanece disponível para realocação.'
              : 'Pode investir com saldo livre, montinhos, ou uma combinação dos dois.' }}
          </p>
          <div v-for="(row, index) in allocations" :key="index" class="allocation-row">
            <div class="field">
              <label>Origem</label>
              <select v-model="row.sourceId" required>
                <option disabled value="">Selecione</option>
                <option v-if="freeSourceAvailable(index)" :value="FREE_SOURCE">
                  Saldo livre (disp. {{ formatMoney(editableAvailable(FREE_SOURCE)) }})
                </option>
                <option
                  v-for="r in availableReservesFor(index)"
                  :key="r.id"
                  :value="r.id"
                >
                  {{ r.name }} (disp. {{ formatMoney(editableAvailable(r.id)) }})
                </option>
              </select>
            </div>
            <div class="field">
              <label>Valor investido</label>
              <MoneyInput v-model="row.amount" required />
            </div>
            <button
              class="btn danger"
              type="button"
              :disabled="allocations.length === 1"
              @click="removeAllocation(index)"
            >
              Remover
            </button>
          </div>
          <div class="total">
            Total alocado: <strong>{{ formatMoney(totalAllocated) }}</strong>
          </div>
        </div>

        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="closeForm">Cancelar</button>
        </div>
      </form>
    </div>

    <div v-if="showAmount" class="modal-backdrop" @click.self="showAmount = null">
      <form class="modal" @submit.prevent="updateAmount">
        <h2>Atualizar valor atual</h2>
        <div class="field"><label>Valor atual</label><MoneyInput v-model="currentAmount" required /></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showAmount = null">Cancelar</button>
        </div>
      </form>
    </div>

    <div v-if="showDetails" class="modal-backdrop" @click.self="showDetails = null">
      <div class="modal wide">
        <h2>{{ showDetails.name }}</h2>
        <p class="muted">Distribuição por origem (base da liquidação proporcional).</p>
        <DataTable :rows="detailRows" :columns="detailColumns" row-key="sourceKey" :page-size="5">
          <template #cell-reserveId="{ row }">{{ sourceLabel(row.reserveId) }}</template>
          <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
          <template #cell-proportion="{ row }">
            {{ showDetails.startAmount > 0
              ? ((row.amount / showDetails.startAmount) * 100).toFixed(2)
              : '0.00' }}%
          </template>
        </DataTable>
        <p class="muted" style="margin-top: 0.75rem">{{ formatSources(showDetails) }}</p>
        <div class="actions" style="margin-top: 1rem">
          <button class="btn secondary" type="button" @click="showDetails = null">Fechar</button>
          <button class="btn" type="button" @click="showDetails && openEdit(showDetails); showDetails = null">Editar vínculos</button>
        </div>
      </div>
    </div>

    <LiquidationModal
      v-if="showLiquidation"
      :investment="showLiquidation"
      :reserves="reserves"
      @close="showLiquidation = null"
      @liquidated="onLiquidated"
    />
  </div>
</template>

<style scoped>
.wide {
  width: min(720px, 100%);
}

.allocations {
  margin: 0.5rem 0 1rem;
  padding: 1rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: rgba(255, 255, 255, 0.02);
}

.allocations-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.35rem;
}

.allocations h3 {
  margin: 0;
  font-size: 1rem;
}

.allocation-row {
  display: grid;
  grid-template-columns: 1fr 160px auto;
  gap: 0.75rem;
  align-items: end;
  margin-top: 0.75rem;
}

.allocation-row .field {
  margin-bottom: 0;
}

.total {
  margin-top: 0.85rem;
  color: var(--muted);
}

.linkish {
  background: none;
  border: none;
  color: var(--accent);
  cursor: pointer;
  padding: 0;
  font: inherit;
  text-decoration: underline;
}

@media (max-width: 700px) {
  .allocation-row {
    grid-template-columns: 1fr;
  }
}
</style>
