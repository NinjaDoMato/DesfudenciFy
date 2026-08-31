<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type Entry, type Investment, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const route = useRoute()
const router = useRouter()
const toast = useToastStore()
const reserveId = computed(() => String(route.params.id))

const reserve = ref<Reserve | null>(null)
const entries = ref<Entry[]>([])
const investments = ref<Investment[]>([])
const otherReserves = ref<Reserve[]>([])
const loading = ref(true)

const form = reactive({
  name: '',
  description: '',
  goal: 0,
  displayColor: '#38bdf8',
  monthlyGoal: 0,
})

const entryForm = reactive({
  amount: 0,
  observation: '',
  useFreeBalance: false,
})

const entryAmountIsPositive = computed(() => entryForm.amount > 0)
const entryAmountIsNegative = computed(() => entryForm.amount < 0)

const transferForm = reactive({
  amount: 0,
  targetReserveId: '',
  observation: '',
})

const entryColumns: DataTableColumn<Entry>[] = [
  { key: 'occurredAt', label: 'Data', sortValue: (row) => new Date(row.occurredAt) },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'observation', label: 'Observação', sortValue: (row) => row.observation },
  { key: 'actions', label: '', sortable: false },
]

const investmentColumns: DataTableColumn<Investment>[] = [
  { key: 'name', label: 'Investimento', sortValue: (row) => row.name },
  {
    key: 'amount',
    label: 'Valor',
    sortValue: (row) => row.sourceReserves.find((s) => s.reserveId === reserveId.value)?.amount || 0,
  },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  loading.value = true
  try {
    const [reserveRes, entriesRes, investmentsRes, reservesRes] = await Promise.all([
      api.get<Reserve>(`/reserves/${reserveId.value}`),
      api.get<Entry[]>('/entries', { params: { reserveId: reserveId.value } }),
      api.get<Investment[]>('/investments'),
      api.get<Reserve[]>('/reserves'),
    ])

    reserve.value = reserveRes.data
    entries.value = entriesRes.data
    investments.value = investmentsRes.data.filter((inv) =>
      inv.sourceReserves.some((s) => s.reserveId === reserveId.value),
    )
    otherReserves.value = reservesRes.data.filter((r) => r.id !== reserveId.value)

    Object.assign(form, {
      name: reserveRes.data.name,
      description: reserveRes.data.description,
      goal: reserveRes.data.goal,
      displayColor: reserveRes.data.displayColor || '#38bdf8',
      monthlyGoal: reserveRes.data.monthlyGoal || 0,
    })
  } catch (e) {
    toastError(e, 'Erro ao carregar reserva')
  } finally {
    loading.value = false
  }
}

async function saveReserve() {
  try {
    const { data } = await api.put<Reserve>(`/reserves/${reserveId.value}`, {
      name: form.name,
      description: form.description,
      goal: Number(form.goal),
      displayColor: form.displayColor,
      monthlyGoal: Number(form.monthlyGoal) || null,
    })
    reserve.value = data
    toast.success('Reserva atualizada.')
  } catch (e) {
    toastError(e, 'Erro ao atualizar')
  }
}

async function addEntry() {
  try {
    await api.post('/entries', {
      amount: Number(entryForm.amount),
      observation: entryForm.observation,
      destination: 'Reserve',
      reserveId: reserveId.value,
      useFreeBalance: entryForm.useFreeBalance,
    })
    entryForm.amount = 0
    entryForm.observation = ''
    entryForm.useFreeBalance = false
    await load()
    toast.success('Lançamento adicionado.')
  } catch (e) {
    toastError(e, 'Erro ao adicionar lançamento')
  }
}

async function removeEntry(id: string) {
  if (!confirm('Tem certeza que deseja remover este lançamento?')) return
  try {
    await api.delete(`/entries/${id}`)
    await load()
  } catch (e) {
    toastError(e, 'Erro ao remover lançamento')
  }
}

function fillFullBalance() {
  if (!reserve.value) return
  transferForm.amount = reserve.value.availableValue
}

async function transfer() {
  try {
    await api.post('/entries/transfer', {
      amount: Number(transferForm.amount),
      observation: transferForm.observation || 'Transferência entre reservas',
      sourceDestination: 'Reserve',
      sourceReserveId: reserveId.value,
      targetDestination: 'Reserve',
      targetReserveId: transferForm.targetReserveId,
    })
    transferForm.amount = 0
    transferForm.targetReserveId = ''
    transferForm.observation = ''
    await load()
    toast.success('Transferência realizada.')
  } catch (e) {
    toastError(e, 'Erro na transferência')
  }
}

onMounted(load)

watch(reserveId, () => {
  void load()
})

watch(
  () => entryForm.amount,
  (amount) => {
    entryForm.useFreeBalance = amount > 0
  },
)
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Gerenciar</p>
        <h1>{{ reserve?.name || 'Reserva' }}</h1>
        <p class="muted">Dados, lançamentos, transferências e investimentos vinculados.</p>
      </div>
      <button class="btn secondary" type="button" @click="router.push({ name: 'reserves' })">Voltar</button>
    </div>

    <p v-if="loading" class="muted">Carregando...</p>

    <template v-else-if="reserve">
      <div class="detail-layout">
        <form class="panel" @submit.prevent="saveReserve">
          <h2>Dados</h2>
          <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
          <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="3" /></div>
          <div class="field"><label>Meta</label><MoneyInput v-model="form.goal" /></div>
          <div class="field"><label>Meta mensal</label><MoneyInput v-model="form.monthlyGoal" /></div>
          <div class="field"><label>Cor</label><input v-model="form.displayColor" type="color" /></div>
          <div class="kpi-row">
            <div class="kpi"><div class="label">Saldo disponível</div><div class="value">{{ formatMoney(reserve.availableValue) }}</div></div>
            <div class="kpi"><div class="label">Investido</div><div class="value">{{ formatMoney(reserve.investedValue) }}</div></div>
            <div class="kpi"><div class="label">Atual</div><div class="value">{{ formatMoney(reserve.currentValue) }}</div></div>
          </div>
          <div class="actions">
            <button class="btn" type="submit">Atualizar reserva</button>
          </div>
        </form>

        <div class="side-stack">
          <div class="panel">
            <h2>Lançamentos</h2>
            <form class="entry-add" @submit.prevent="addEntry">
              <div class="field"><label>Valor</label><MoneyInput v-model="entryForm.amount" allow-negative required /></div>
              <div class="field"><label>Observação</label><input v-model="entryForm.observation" maxlength="100" /></div>
              <label v-if="entryAmountIsPositive" class="checkbox-field">
                <input v-model="entryForm.useFreeBalance" type="checkbox" />
                Debitar do Saldo Livre
              </label>
              <label v-else-if="entryAmountIsNegative" class="checkbox-field">
                <input v-model="entryForm.useFreeBalance" type="checkbox" />
                Mover para Saldo Livre
              </label>
              <button class="btn" type="submit">Adicionar</button>
            </form>
            <DataTable
              :rows="entries"
              :columns="entryColumns"
              row-key="id"
              :page-size="5"
              initial-sort-key="occurredAt"
              initial-sort-dir="desc"
              empty-text="Nenhum lançamento nesta reserva."
            >
              <template #cell-occurredAt="{ row }">{{ new Date(row.occurredAt).toLocaleString('pt-BR') }}</template>
              <template #cell-amount="{ row }">
                <span :style="{ color: row.amount >= 0 ? 'var(--success)' : 'var(--danger)' }">{{ formatMoney(row.amount) }}</span>
              </template>
              <template #cell-actions="{ row }">
                <IconButton label="Excluir" icon="delete" variant="danger" @click="removeEntry(row.id)" />
              </template>
            </DataTable>
          </div>

          <div class="panel">
            <h2>Transferência entre reservas</h2>
            <form @submit.prevent="transfer">
              <div class="field">
                <label>Valor</label>
                <div class="inline-actions">
                  <MoneyInput v-model="transferForm.amount" required />
                  <button class="btn secondary" type="button" @click="fillFullBalance">Transferir todo saldo</button>
                </div>
              </div>
              <div class="field">
                <label>Reserva destino</label>
                <select v-model="transferForm.targetReserveId" required>
                  <option disabled value="">Selecione a reserva destino</option>
                  <option v-for="item in otherReserves" :key="item.id" :value="item.id">
                    {{ item.name }} ({{ formatMoney(item.availableValue) }})
                  </option>
                </select>
              </div>
              <div class="field"><label>Observação</label><input v-model="transferForm.observation" /></div>
              <button class="btn" type="submit">Transferir</button>
            </form>
          </div>

          <div class="panel">
            <div class="page-header">
              <h2>Investimentos</h2>
              <RouterLink class="btn secondary" :to="{ name: 'investments', query: { reserveId } }">
                Ver investimentos
              </RouterLink>
            </div>
            <DataTable
              :rows="investments"
              :columns="investmentColumns"
              row-key="id"
              :page-size="5"
              initial-sort-key="name"
              empty-text="Nenhum investimento vinculado."
            >
              <template #cell-amount="{ row }">
                {{ formatMoney(row.sourceReserves.find((s) => s.reserveId === reserveId)?.amount || 0) }}
              </template>
            </DataTable>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.eyebrow {
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  font-size: 0.75rem;
  color: var(--accent);
  font-weight: 700;
}

.detail-layout {
  display: grid;
  grid-template-columns: minmax(280px, 360px) 1fr;
  gap: 1rem;
  align-items: start;
}

.side-stack {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.kpi-row {
  display: grid;
  gap: 0.75rem;
  margin: 0.5rem 0 1rem;
}

.entry-add {
  display: grid;
  grid-template-columns: 140px 1fr auto;
  gap: 0.75rem;
  align-items: end;
  margin-bottom: 1rem;
}

.checkbox-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--text-muted);
  grid-column: 1 / -1;
  margin-bottom: 0;
}

.entry-add .field {
  margin-bottom: 0;
}

.inline-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
  align-items: center;
}

.inline-actions :deep(.money-input) {
  flex: 1;
  min-width: 140px;
}

@media (max-width: 1000px) {
  .detail-layout {
    grid-template-columns: 1fr;
  }

  .entry-add {
    grid-template-columns: 1fr;
  }
}
</style>
