<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import {
  formatMoney,
  type BankAccount,
  type Investment,
  type InvestmentType,
  type Reserve,
} from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

interface AllocationRow {
  sourceId: string
  amount: number
}

type SourceRow = { sourceKey: string; reserveId: string | null; amount: number }

const FREE_SOURCE = '__free__'

const route = useRoute()
const router = useRouter()
const items = ref<Investment[]>([])
const reserves = ref<Reserve[]>([])
const bankAccounts = ref<BankAccount[]>([])
const types = ref<InvestmentType[]>([])
const freeBalanceAvailable = ref(0)
const filters = reactive({
  name: '',
  bankAccountId: '',
  investmentTypeId: '',
  reserveId: '',
})

function applyRouteFilters() {
  const reserveId = route.query.reserveId
  filters.reserveId = typeof reserveId === 'string' ? reserveId : ''
}

watch(
  () => route.query.reserveId,
  () => applyRouteFilters(),
)

const error = ref('')
const showForm = ref(false)
const editingId = ref<string | null>(null)
const showAmount = ref<Investment | null>(null)
const currentAmount = ref(0)
const showDetails = ref<Investment | null>(null)
const showLiquidation = ref<Investment | null>(null)
const liquidating = ref(false)
const originalAllocations = ref<AllocationRow[]>([])

const form = reactive({
  name: '',
  rentability: '',
  startDate: new Date().toISOString().slice(0, 10),
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

const liquidationSummary = computed(() => {
  const item = showLiquidation.value
  if (!item) return null

  const invested = item.startAmount
  const finalValue = item.currentAmount
  const profit = Math.round((finalValue - invested) * 100) / 100
  const distributions = toSourceRows(item).map((source) => {
    const proportion = invested > 0 ? source.amount / invested : 0
    const profitShare = profit > 0 ? Math.round(proportion * profit * 100) / 100 : 0
    return {
      sourceKey: source.sourceKey,
      reserveId: source.reserveId,
      investedAmount: source.amount,
      proportion,
      profitShare,
    }
  })

  return { invested, finalValue, profit, distributions }
})

const detailRows = computed(() => (showDetails.value ? toSourceRows(showDetails.value) : []))

const columns: DataTableColumn<Investment>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'investmentTypeName', label: 'Tipo', sortValue: (row) => row.investmentTypeName },
  { key: 'rentability', label: 'Rentabilidade', sortValue: (row) => row.rentability },
  { key: 'bankAccountName', label: 'Conta', sortValue: (row) => row.bankAccountName },
  { key: 'reserves', label: 'Origens', sortValue: (row) => row.sourceReserves.length },
  { key: 'startAmount', label: 'Inicial', sortValue: (row) => row.startAmount },
  { key: 'currentAmount', label: 'Atual', sortValue: (row) => row.currentAmount },
  { key: 'endDate', label: 'Fim', sortValue: (row) => (row.endDate ? new Date(row.endDate) : null) },
  { key: 'actions', label: '', sortable: false },
]

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

const liquidationColumns: DataTableColumn<{
  sourceKey: string
  reserveId: string | null
  investedAmount: number
  proportion: number
  profitShare: number
}>[] = [
  { key: 'reserveId', label: 'Origem', sortValue: (row) => sourceLabel(row.reserveId) },
  { key: 'investedAmount', label: 'Investido', sortValue: (row) => row.investedAmount },
  { key: 'proportion', label: 'Proporção', sortValue: (row) => row.proportion },
  { key: 'profitShare', label: 'Lucro a lançar', sortValue: (row) => row.profitShare },
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
    startDate: new Date().toISOString().slice(0, 10),
    endDate: '',
    bankAccountId: '',
    investmentTypeId: '',
  })
  allocations.value = [{ sourceId: '', amount: 0 }]
  originalAllocations.value = []
  editingId.value = null
}

function openCreate() {
  error.value = ''
  resetForm()
  showForm.value = true
}

function openEdit(item: Investment) {
  error.value = ''
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    rentability: item.rentability || '',
    startDate: item.startDate.slice(0, 10),
    endDate: item.endDate ? item.endDate.slice(0, 10) : '',
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
  error.value = ''
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
    error.value = 'Informe ao menos uma origem com valor (saldo livre ou reserva).'
    return false
  }

  const freeCount = sourceReserves.filter((s) => !s.reserveId).length
  if (freeCount > 1) {
    error.value = 'Saldo livre só pode aparecer uma vez.'
    return false
  }

  const reserveIds = sourceReserves.map((s) => s.reserveId).filter((id): id is string => !!id)
  if (new Set(reserveIds).size !== reserveIds.length) {
    error.value = 'Cada reserva só pode aparecer uma vez.'
    return false
  }

  for (const source of sourceReserves) {
    const sourceId = sourceIdFromAllocation(source.reserveId)
    if (source.amount > editableAvailable(sourceId)) {
      error.value = `Saldo insuficiente em ${sourceLabel(source.reserveId)}.`
      return false
    }
  }

  return true
}

async function load() {
  const [inv, res, banks, t, free] = await Promise.all([
    api.get<Investment[]>('/investments'),
    api.get<Reserve[]>('/reserves'),
    api.get<BankAccount[]>('/lookups/bank-accounts'),
    api.get<InvestmentType[]>('/lookups/investment-types'),
    api.get<{ amount: number }>('/entries/free-balance'),
  ])
  items.value = inv.data
  reserves.value = res.data
  bankAccounts.value = banks.data
  types.value = t.data
  freeBalanceAvailable.value = free.data.amount
}

const activeBankAccounts = computed(() => bankAccounts.value.filter((x) => x.isActive))
const activeTypes = computed(() => types.value.filter((x) => x.isActive))

async function save() {
  error.value = ''
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
    error.value = e instanceof Error ? e.message : 'Erro'
  }
}

async function updateAmount() {
  if (!showAmount.value) return
  error.value = ''
  try {
    await api.put(`/investments/${showAmount.value.id}/current-amount`, {
      currentAmount: Number(currentAmount.value),
    })
    showAmount.value = null
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro'
  }
}

function openLiquidation(item: Investment) {
  error.value = ''
  showLiquidation.value = item
}

async function confirmLiquidation() {
  if (!showLiquidation.value) return
  liquidating.value = true
  error.value = ''
  try {
    await api.post(`/investments/${showLiquidation.value.id}/liquidate`)
    showLiquidation.value = null
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao liquidar'
  } finally {
    liquidating.value = false
  }
}

onMounted(async () => {
  applyRouteFilters()
  try {
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro'
  }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h1>Investimentos</h1>
        <p class="muted">Renda fixa com saldo livre e/ou reservas. Na liquidação, o lucro é rateado pelo valor investido.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Novo investimento</button>
    </div>
    <div v-if="error && !showForm && !showAmount && !showLiquidation" class="error">{{ error }}</div>
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
          {{ row.endDate ? new Date(row.endDate).toLocaleDateString('pt-BR') : '-' }}
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
        <div v-if="error" class="error">{{ error }}</div>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field">
          <label>Rentabilidade</label>
          <input v-model="form.rentability" maxlength="100" placeholder="Ex.: 100% CDI, 12% a.a." />
          <p class="muted">Apenas registro — não entra em nenhum cálculo.</p>
        </div>
        <div class="field"><label>Início</label><input v-model="form.startDate" type="date" required /></div>
        <div class="field"><label>Fim (opcional)</label><input v-model="form.endDate" type="date" /></div>
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
              ? 'Use saldo livre e/ou reservas. O valor já aplicado neste investimento permanece disponível para realocação.'
              : 'Pode investir com saldo livre, reservas, ou uma combinação dos dois.' }}
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

    <div v-if="showAmount" class="modal-backdrop" @click.self="showAmount = null; error = ''">
      <form class="modal" @submit.prevent="updateAmount">
        <h2>Atualizar valor atual</h2>
        <div v-if="error" class="error">{{ error }}</div>
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

    <div v-if="showLiquidation && liquidationSummary" class="modal-backdrop" @click.self="!liquidating && (showLiquidation = null)">
      <div class="modal wide">
        <h2>Resumo da liquidação</h2>
        <p class="muted">O principal volta ao disponível das origens; o lucro é lançado proporcionalmente (saldo livre ou reserva).</p>
        <div v-if="error" class="error">{{ error }}</div>

        <div class="summary-grid">
          <div class="kpi">
            <div class="label">Investimento</div>
            <div class="value value-sm">{{ showLiquidation.name }}</div>
          </div>
          <div class="kpi">
            <div class="label">Valor investido original</div>
            <div class="value">{{ formatMoney(liquidationSummary.invested) }}</div>
          </div>
          <div class="kpi">
            <div class="label">Valor final</div>
            <div class="value">{{ formatMoney(liquidationSummary.finalValue) }}</div>
          </div>
          <div class="kpi">
            <div class="label">Lucro</div>
            <div class="value" :class="{ profit: liquidationSummary.profit > 0, zero: liquidationSummary.profit <= 0 }">
              {{ formatMoney(liquidationSummary.profit) }}
            </div>
          </div>
        </div>

        <h3 class="section-title">Lançamentos de lucro por origem</h3>
        <DataTable :rows="liquidationSummary.distributions" :columns="liquidationColumns" row-key="sourceKey" :page-size="5">
          <template #cell-reserveId="{ row }">{{ sourceLabel(row.reserveId) }}</template>
          <template #cell-investedAmount="{ row }">{{ formatMoney(row.investedAmount) }}</template>
          <template #cell-proportion="{ row }">{{ (row.proportion * 100).toFixed(2) }}%</template>
          <template #cell-profitShare="{ row }">
            <span :style="{ color: row.profitShare > 0 ? 'var(--success)' : 'var(--muted)' }">
              {{ formatMoney(row.profitShare) }}
            </span>
          </template>
        </DataTable>

        <p v-if="liquidationSummary.profit <= 0" class="muted note">
          Sem lucro positivo: nenhum lançamento de rendimento será criado. O principal investido volta ao saldo disponível ao remover os vínculos.
        </p>
        <p v-else class="muted note">
          Total de lucro a lançar: <strong>{{ formatMoney(liquidationSummary.profit) }}</strong>
        </p>

        <div class="actions" style="margin-top: 1rem">
          <button class="btn" type="button" :disabled="liquidating" @click="confirmLiquidation">
            {{ liquidating ? 'Liquidando...' : 'Confirmar liquidação' }}
          </button>
          <button class="btn secondary" type="button" :disabled="liquidating" @click="showLiquidation = null">Cancelar</button>
        </div>
      </div>
    </div>
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

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.75rem;
  margin: 1rem 0;
}

.value-sm {
  font-size: 1.05rem !important;
}

.profit {
  color: var(--success);
}

.zero {
  color: var(--muted);
}

.section-title {
  margin: 0.5rem 0 0.75rem;
  font-size: 1rem;
}

.note {
  margin-top: 0.85rem;
}

@media (max-width: 700px) {
  .allocation-row {
    grid-template-columns: 1fr;
  }
}
</style>
