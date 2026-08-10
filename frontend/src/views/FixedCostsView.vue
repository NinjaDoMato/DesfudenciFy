<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type FixedCost, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

const router = useRouter()
const items = ref<FixedCost[]>([])
const reserves = ref<Reserve[]>([])
const error = ref('')
const showForm = ref(false)
const form = reactive({
  name: '',
  description: '',
  amount: 0,
  recurrence: 'Month',
  dueDate: '',
  reserveId: '',
})

const columns: DataTableColumn<FixedCost>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'recurrence', label: 'Recorrência', sortValue: (row) => row.recurrence },
  { key: 'dueDate', label: 'Vencimento', sortValue: (row) => (row.dueDate ? new Date(row.dueDate) : null) },
  { key: 'reserveName', label: 'Reserva', sortValue: (row) => row.reserveName || '' },
  { key: 'payments', label: 'Pagamentos', sortValue: (row) => row.payments.length },
  { key: 'actions', label: '', sortable: false },
]

const recurrenceLabel: Record<string, string> = {
  Day: 'Diária',
  Week: 'Semanal',
  Month: 'Mensal',
  Year: 'Anual',
}

async function load() {
  const [costs, res] = await Promise.all([
    api.get<FixedCost[]>('/fixed-costs'),
    api.get<Reserve[]>('/reserves'),
  ])
  items.value = costs.data
  reserves.value = res.data
}

function openCreate() {
  error.value = ''
  Object.assign(form, {
    name: '',
    description: '',
    amount: 0,
    recurrence: 'Month',
    dueDate: '',
    reserveId: '',
  })
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  error.value = ''
}

async function save() {
  error.value = ''
  try {
    await api.post('/fixed-costs', {
      name: form.name,
      description: form.description,
      amount: Number(form.amount),
      recurrence: form.recurrence,
      dueDate: form.dueDate || null,
      reserveId: form.reserveId || null,
    })
    closeForm()
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro'
  }
}

async function remove(id: string) {
  if (!confirm('Excluir conta fixa?')) return
  await api.delete(`/fixed-costs/${id}`)
  await load()
}

onMounted(async () => {
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
        <h1>Contas fixas</h1>
        <p class="muted">Gastos recorrentes como luz, água, internet.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Nova conta</button>
    </div>
    <div v-if="error && !showForm" class="error">{{ error }}</div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="dueDate">
        <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
        <template #cell-recurrence="{ row }">{{ recurrenceLabel[row.recurrence] || row.recurrence }}</template>
        <template #cell-dueDate="{ row }">
          {{ row.dueDate ? new Date(row.dueDate).toLocaleDateString('pt-BR') : '-' }}
        </template>
        <template #cell-reserveName="{ row }">{{ row.reserveName || '-' }}</template>
        <template #cell-payments="{ row }">{{ row.payments.length }}</template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton
              label="Detalhes"
              icon="details"
              @click="router.push({ name: 'fixed-cost-detail', params: { id: row.id } })"
            />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>
    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <form class="modal" @submit.prevent="save">
        <h2>Nova conta fixa</h2>
        <div v-if="error" class="error">{{ error }}</div>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="2" /></div>
        <div class="field"><label>Valor</label><MoneyInput v-model="form.amount" required /></div>
        <div class="field">
          <label>Recorrência</label>
          <select v-model="form.recurrence">
            <option value="Day">Diária</option>
            <option value="Week">Semanal</option>
            <option value="Month">Mensal</option>
            <option value="Year">Anual</option>
          </select>
        </div>
        <div class="field">
          <label>Data de vencimento</label>
          <input v-model="form.dueDate" type="date" required />
          <span class="muted hint">Usada na dashboard para próximas contas. Avança automaticamente ao pagar.</span>
        </div>
        <div class="field">
          <label>Reserva (opcional)</label>
          <select v-model="form.reserveId">
            <option value="">Nenhuma</option>
            <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="closeForm">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.hint {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.82rem;
}
</style>
