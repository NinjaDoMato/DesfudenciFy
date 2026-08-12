<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import api from '@/api/client'
import { formatMoney, type IncomeSource, type IncomeType } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

const items = ref<IncomeSource[]>([])
const incomeTypes = ref<IncomeType[]>([])
const error = ref('')
const showForm = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({ name: '', amount: 0, description: '', isActive: true, incomeTypeId: '' })

const columns: DataTableColumn<IncomeSource>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'incomeTypeName', label: 'Tipo', sortValue: (row) => row.incomeTypeName },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'description', label: 'Descrição', sortValue: (row) => row.description },
  { key: 'actions', label: '', sortable: false },
]

async function load() {
  const [sourcesRes, typesRes] = await Promise.all([
    api.get<IncomeSource[]>('/income-sources'),
    api.get<IncomeType[]>('/lookups/income-types'),
  ])
  items.value = sourcesRes.data
  incomeTypes.value = typesRes.data
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    name: '',
    amount: 0,
    description: '',
    isActive: true,
    incomeTypeId: incomeTypes.value[0]?.id ?? '',
  })
  showForm.value = true
}

function openEdit(item: IncomeSource) {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    amount: item.amount,
    description: item.description,
    isActive: item.isActive,
    incomeTypeId: item.incomeTypeId,
  })
  showForm.value = true
}

async function save() {
  error.value = ''
  try {
    const payload = {
      name: form.name,
      amount: Number(form.amount),
      description: form.description,
      isActive: form.isActive,
      incomeTypeId: form.incomeTypeId,
    }
    if (editingId.value) await api.put(`/income-sources/${editingId.value}`, payload)
    else await api.post('/income-sources', payload)
    showForm.value = false
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro'
  }
}

async function remove(id: string) {
  if (!confirm('Desativar entrada?')) return
  await api.delete(`/income-sources/${id}`)
  await load()
}

onMounted(async () => {
  try { await load() } catch (e) { error.value = e instanceof Error ? e.message : 'Erro' }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div><h1>Entradas</h1><p class="muted">Fontes de renda recorrentes (salário, etc.).</p></div>
      <button class="btn" type="button" @click="openCreate">Nova entrada</button>
    </div>
    <div v-if="error && !showForm" class="error">{{ error }}</div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="name">
        <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton label="Editar" icon="edit" @click="openEdit(row)" />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>
    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false; error = ''">
      <form class="modal" @submit.prevent="save">
        <h2>{{ editingId ? 'Editar' : 'Nova' }} entrada</h2>
        <div v-if="error" class="error">{{ error }}</div>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field">
          <label>Tipo</label>
          <select v-model="form.incomeTypeId" required>
            <option disabled value="">Selecione</option>
            <option v-for="type in incomeTypes" :key="type.id" :value="type.id">{{ type.name }}</option>
          </select>
        </div>
        <div class="field"><label>Valor</label><MoneyInput v-model="form.amount" required /></div>
        <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="3" /></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showForm = false">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
