<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api/client'
import {
  compareDateStrings,
  formatDate,
  formatMoney,
  parseDateForSort,
  todayDateInputValue,
  type Purchase,
  type Reserve,
} from '@/types'
import DateInput from '@/components/DateInput.vue'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const FREE_SOURCE = '__free__'

interface PurchaseRow extends Purchase {
  totalAmount: number
  monthlyAmount: number
  remainingCount: number
  installmentCount: number
  nextDueDate: string | null
  isActive: boolean
}

const router = useRouter()
const toast = useToastStore()
const items = ref<PurchaseRow[]>([])
const reserves = ref<Reserve[]>([])
const showForm = ref(false)
const form = reactive({
  name: '',
  productUrl: '',
  totalAmount: 0,
  installmentCount: 1,
  firstDueDate: todayDateInputValue(),
  sourceId: '',
})

const columns: DataTableColumn<PurchaseRow>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'totalAmount', label: 'Valor total', sortValue: (row) => row.totalAmount },
  { key: 'monthlyAmount', label: 'Parcela', sortValue: (row) => row.monthlyAmount },
  { key: 'remaining', label: 'Restantes', sortValue: (row) => row.remainingCount },
  { key: 'nextDueDate', label: 'Próximo vencimento', sortValue: (row) => parseDateForSort(row.nextDueDate) },
  { key: 'source', label: 'Origem', sortValue: (row) => sourceLabel(row) },
  { key: 'status', label: 'Status', sortValue: (row) => (row.isActive ? 1 : 0) },
  { key: 'actions', label: '', sortable: false },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

function sourceLabel(item: Purchase) {
  if (item.debitSource === 'FreeBalance') return 'Saldo livre'
  return item.reserveName || '-'
}

function debitPayload(sourceId: string) {
  if (sourceId === FREE_SOURCE) {
    return { debitSource: 'FreeBalance', reserveId: null }
  }
  if (sourceId) {
    return { debitSource: 'Reserve', reserveId: sourceId }
  }
  return { debitSource: 'None', reserveId: null }
}

function toRow(item: Purchase): PurchaseRow {
  const remaining = item.installments.filter((installment) => !installment.paid)
  const next = remaining
    .slice()
    .sort((a, b) => compareDateStrings(a.dueDate, b.dueDate))[0]

  return {
    ...item,
    totalAmount: item.installments.reduce((sum, installment) => sum + installment.amount, 0),
    monthlyAmount: next?.amount ?? 0,
    remainingCount: remaining.length,
    installmentCount: item.installments.length,
    nextDueDate: next?.dueDate ?? null,
    isActive: remaining.length > 0,
  }
}

async function load() {
  const [purchases, res] = await Promise.all([
    api.get<Purchase[]>('/purchases'),
    api.get<Reserve[]>('/reserves'),
  ])
  items.value = purchases.data.map(toRow)
  reserves.value = res.data
}

function openCreate() {
  Object.assign(form, {
    name: '',
    productUrl: '',
    totalAmount: 0,
    installmentCount: 1,
    firstDueDate: todayDateInputValue(),
    sourceId: '',
  })
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function save() {
  try {
    await api.post('/purchases', {
      name: form.name,
      productUrl: form.productUrl || null,
      totalAmount: Number(form.totalAmount),
      installmentCount: Number(form.installmentCount),
      firstDueDate: form.firstDueDate,
      ...debitPayload(form.sourceId),
    })
    closeForm()
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function remove(id: string) {
  if (!confirm('Excluir parcelamento?')) return
  await api.delete(`/purchases/${id}`)
  await load()
}

onMounted(async () => {
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
        <h1>Parcelamentos</h1>
        <p class="muted">Compras parceladas e controle de parcelas.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Novo parcelamento</button>
    </div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="nextDueDate">
        <template #cell-totalAmount="{ row }">{{ formatMoney(row.totalAmount) }}</template>
        <template #cell-monthlyAmount="{ row }">{{ formatMoney(row.monthlyAmount) }}</template>
        <template #cell-remaining="{ row }">{{ row.remainingCount }} / {{ row.installmentCount }}</template>
        <template #cell-nextDueDate="{ row }">
          {{ formatDate(row.nextDueDate) || '-' }}
        </template>
        <template #cell-source="{ row }">{{ sourceLabel(row) }}</template>
        <template #cell-status="{ row }">
          <span class="badge" :class="row.isActive ? '' : 'success'">
            {{ row.isActive ? 'Ativo' : 'Quitado' }}
          </span>
        </template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton
              label="Detalhes"
              icon="details"
              @click="router.push({ name: 'purchase-detail', params: { id: row.id } })"
            />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>
    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <form class="modal" @submit.prevent="save">
        <h2>Novo parcelamento</h2>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>URL do produto</label><input v-model="form.productUrl" /></div>
        <div class="field"><label>Valor total</label><MoneyInput v-model="form.totalAmount" required /></div>
        <div class="field"><label>Parcelas</label><input v-model.number="form.installmentCount" type="number" min="1" required /></div>
        <div class="field"><label>Primeiro vencimento</label><DateInput v-model="form.firstDueDate" required /></div>
        <div class="field">
          <label>Origem do pagamento (opcional)</label>
          <select v-model="form.sourceId">
            <option value="">Nenhuma</option>
            <option :value="FREE_SOURCE">Saldo livre</option>
            <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
          <span class="muted hint">Ao pagar uma parcela, o valor é debitado desta origem.</span>
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
