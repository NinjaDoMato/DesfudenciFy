<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import api from '@/api/client'
import type { BankAccount } from '@/types'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

const items = ref<BankAccount[]>([])
const error = ref('')
const showForm = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({ name: '', description: '', isActive: true })

const columns: DataTableColumn<BankAccount>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'description', label: 'Descrição', sortValue: (row) => row.description || '' },
  { key: 'isActive', label: 'Ativa', sortValue: (row) => (row.isActive ? 1 : 0) },
  { key: 'actions', label: '', sortable: false },
]

async function load() {
  const { data } = await api.get<BankAccount[]>('/admin/bank-accounts')
  items.value = data
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', description: '', isActive: true })
  showForm.value = true
}

function openEdit(item: BankAccount) {
  editingId.value = item.id
  Object.assign(form, item)
  showForm.value = true
}

async function save() {
  error.value = ''
  try {
    if (editingId.value) await api.put(`/admin/bank-accounts/${editingId.value}`, form)
    else await api.post('/admin/bank-accounts', form)
    showForm.value = false
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro'
  }
}

async function remove(id: string) {
  if (!confirm('Excluir conta?')) return
  try {
    await api.delete(`/admin/bank-accounts/${id}`)
    await load()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro'
  }
}

onMounted(async () => {
  try { await load() } catch (e) { error.value = e instanceof Error ? e.message : 'Erro' }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div><h1>Contas bancárias</h1><p class="muted">Contas usadas nos investimentos de renda fixa.</p></div>
      <button class="btn" type="button" @click="openCreate">Nova conta</button>
    </div>
    <div v-if="error && !showForm" class="error">{{ error }}</div>
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
    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false; error = ''">
      <form class="modal" @submit.prevent="save">
        <h2>{{ editingId ? 'Editar' : 'Nova' }} conta</h2>
        <div v-if="error" class="error">{{ error }}</div>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="2" /></div>
        <div class="field"><label><input v-model="form.isActive" type="checkbox" /> Ativa</label></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showForm = false">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
