<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import api from '@/api/client'
import type { UserDto } from '@/types'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
const items = ref<UserDto[]>([])
const showForm = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({ email: '', fullName: '', password: '', role: 'User', isActive: true })

const columns: DataTableColumn<UserDto>[] = [
  { key: 'fullName', label: 'Nome', sortValue: (row) => row.fullName },
  { key: 'email', label: 'Email', sortValue: (row) => row.email },
  { key: 'role', label: 'Role', sortValue: (row) => row.role },
  { key: 'isActive', label: 'Ativo', sortValue: (row) => (row.isActive ? 1 : 0) },
  { key: 'actions', label: '', sortable: false },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  const { data } = await api.get<UserDto[]>('/admin/users')
  items.value = data
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { email: '', fullName: '', password: '', role: 'User', isActive: true })
  showForm.value = true
}

function openEdit(item: UserDto) {
  editingId.value = item.id
  Object.assign(form, { email: item.email, fullName: item.fullName, password: '', role: item.role, isActive: item.isActive })
  showForm.value = true
}

async function save() {
  try {
    if (editingId.value) {
      await api.put(`/admin/users/${editingId.value}`, {
        email: form.email,
        fullName: form.fullName,
        role: form.role,
        isActive: form.isActive,
        password: form.password || null,
      })
    } else {
      await api.post('/admin/users', {
        email: form.email,
        fullName: form.fullName,
        role: form.role,
        password: form.password,
      })
    }
    showForm.value = false
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function remove(id: string) {
  if (!confirm('Excluir usuário?')) return
  await api.delete(`/admin/users/${id}`)
  await load()
}

onMounted(async () => {
  try { await load() } catch (e) { toastError(e, 'Erro') }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div><h1>Usuários</h1><p class="muted">Cadastro e permissões (Admin / User).</p></div>
      <button class="btn" type="button" @click="openCreate">Novo usuário</button>
    </div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="fullName">
        <template #cell-role="{ row }"><span class="badge">{{ row.role }}</span></template>
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
        <h2>{{ editingId ? 'Editar' : 'Novo' }} usuário</h2>
        <div class="field"><label>Nome</label><input v-model="form.fullName" required /></div>
        <div class="field"><label>Email</label><input v-model="form.email" type="email" required /></div>
        <div class="field"><label>Senha {{ editingId ? '(opcional)' : '' }}</label><input v-model="form.password" type="password" :required="!editingId" /></div>
        <div class="field">
          <label>Perfil</label>
          <select v-model="form.role"><option>User</option><option>Admin</option></select>
        </div>
        <div v-if="editingId" class="field"><label><input v-model="form.isActive" type="checkbox" /> Ativo</label></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showForm = false">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
