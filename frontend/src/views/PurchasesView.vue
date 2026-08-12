<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import api from '@/api/client'
import { formatMoney, type Purchase } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

type PurchaseInstallment = Purchase['installments'][number]

const toast = useToastStore()
const items = ref<Purchase[]>([])
const showForm = ref(false)
const form = reactive({ name: '', productUrl: '', totalAmount: 0, installmentCount: 1, firstDueDate: new Date().toISOString().slice(0, 10) })

const installmentColumns: DataTableColumn<PurchaseInstallment>[] = [
  { key: 'installmentNumber', label: '#', sortValue: (row) => row.installmentNumber },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'dueDate', label: 'Vencimento', sortValue: (row) => new Date(row.dueDate) },
  { key: 'status', label: 'Status', sortValue: (row) => (row.paid ? 1 : 0) },
  { key: 'actions', label: '', sortable: false },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  const { data } = await api.get<Purchase[]>('/purchases')
  items.value = data
}

async function create() {
  try {
    await api.post('/purchases', {
      name: form.name,
      productUrl: form.productUrl || null,
      totalAmount: Number(form.totalAmount),
      installmentCount: Number(form.installmentCount),
      firstDueDate: form.firstDueDate,
    })
    showForm.value = false
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function pay(purchaseId: string, installmentId: string) {
  await api.post(`/purchases/${purchaseId}/installments/${installmentId}/pay`)
  await load()
}

async function remove(id: string) {
  if (!confirm('Excluir parcelamento?')) return
  await api.delete(`/purchases/${id}`)
  await load()
}

onMounted(async () => {
  try { await load() } catch (e) { toastError(e, 'Erro') }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div><h1>Parcelamentos</h1><p class="muted">Compras parceladas e controle de parcelas.</p></div>
      <button class="btn" type="button" @click="showForm = true">Novo parcelamento</button>
    </div>
    <div v-for="item in items" :key="item.id" class="panel">
      <div class="page-header">
        <div>
          <h2 style="margin:0">{{ item.name }}</h2>
          <a v-if="item.productUrl" class="muted" :href="item.productUrl" target="_blank">Link do produto</a>
        </div>
        <button class="btn danger" type="button" @click="remove(item.id)">Excluir</button>
      </div>
      <DataTable
        :rows="item.installments"
        :columns="installmentColumns"
        row-key="id"
        :page-size="5"
        initial-sort-key="installmentNumber"
      >
        <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
        <template #cell-dueDate="{ row }">{{ new Date(row.dueDate).toLocaleDateString('pt-BR') }}</template>
        <template #cell-status="{ row }">{{ row.paid ? 'Pago' : 'Pendente' }}</template>
        <template #cell-actions="{ row }">
          <IconButton
            v-if="!row.paid"
            label="Pagar"
            icon="pay"
            variant="primary"
            @click="pay(item.id, row.id)"
          />
        </template>
      </DataTable>
    </div>
    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false">
      <form class="modal" @submit.prevent="create">
        <h2>Novo parcelamento</h2>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>URL do produto</label><input v-model="form.productUrl" /></div>
        <div class="field"><label>Valor total</label><MoneyInput v-model="form.totalAmount" required /></div>
        <div class="field"><label>Parcelas</label><input v-model.number="form.installmentCount" type="number" min="1" required /></div>
        <div class="field"><label>Primeiro vencimento</label><input v-model="form.firstDueDate" type="date" required /></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showForm = false">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
