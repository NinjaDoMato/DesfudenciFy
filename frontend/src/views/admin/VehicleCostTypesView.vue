<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import api from '@/api/client'
import type { VehicleExpenseType } from '@/types'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
const items = ref<VehicleExpenseType[]>([])
const showForm = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({ name: '', description: '', isActive: true })

const columns: DataTableColumn<VehicleExpenseType>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'description', label: 'Descrição', sortValue: (row) => row.description || '' },
  { key: 'isActive', label: 'Ativo', sortValue: (row) => (row.isActive ? 1 : 0) },
  { key: 'actions', label: '', sortable: false },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  const { data } = await api.get<VehicleExpenseType[]>('/admin/vehicle-expense-types')
  items.value = data
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', description: '', isActive: true })
  showForm.value = true
}

function openEdit(item: VehicleExpenseType) {
  editingId.value = item.id
  Object.assign(form, item)
  showForm.value = true
}

async function save() {
  try {
    if (editingId.value) await api.put(`/admin/vehicle-expense-types/${editingId.value}`, form)
    else await api.post('/admin/vehicle-expense-types', form)
    showForm.value = false
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function remove(id: string) {
  if (!confirm('Excluir tipo?')) return
  try {
    await api.delete(`/admin/vehicle-expense-types/${id}`)
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

onMounted(async () => {
  try { await load() } catch (e) { toastError(e, 'Erro') }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h1>Tipos de custo (Automóveis)</h1>
        <p class="muted">Documentação, impostos, revisão, reparos e outros tipos cadastráveis para gastos do veículo.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Novo tipo</button>
    </div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="name">
        <template #cell-isActive="{ row }">{{ row.isActive ? 'Sim' : 'Não' }}</template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton label="Editar" icon="edit" @click="openEdit(row)" />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>
    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false">
      <form class="modal" @submit.prevent="save">
        <h2>{{ editingId ? 'Editar' : 'Novo' }} tipo</h2>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="2" /></div>
        <div class="field"><label><input v-model="form.isActive" type="checkbox" /> Ativo</label></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showForm = false">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
