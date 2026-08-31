<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type Vehicle } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const router = useRouter()
const toast = useToastStore()
const items = ref<Vehicle[]>([])
const showForm = ref(false)
const photoFile = ref<File | null>(null)

const form = reactive({
  name: '',
  model: '',
  year: new Date().getFullYear(),
  paidValue: 0,
  fipeValue: 0,
})

const columns: DataTableColumn<Vehicle>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'model', label: 'Modelo', sortValue: (row) => row.model },
  { key: 'year', label: 'Ano', sortValue: (row) => row.year },
  { key: 'paidValue', label: 'Valor pago', sortValue: (row) => row.paidValue },
  { key: 'fipeValue', label: 'FIPE', sortValue: (row) => row.fipeValue },
  { key: 'actions', label: '', sortable: false },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  const { data } = await api.get<Vehicle[]>('/vehicles')
  items.value = data
}

function openCreate() {
  Object.assign(form, {
    name: '',
    model: '',
    year: new Date().getFullYear(),
    paidValue: 0,
    fipeValue: 0,
  })
  photoFile.value = null
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  photoFile.value = null
}

async function create() {
  try {
    const { data } = await api.post<Vehicle>('/vehicles', {
      name: form.name,
      model: form.model,
      year: Number(form.year),
      paidValue: Number(form.paidValue),
      fipeValue: Number(form.fipeValue),
    })
    if (photoFile.value) {
      const fd = new FormData()
      fd.append('file', photoFile.value)
      await api.post(`/vehicles/${data.id}/photo`, fd)
    }
    closeForm()
    await load()
    await router.push({ name: 'vehicle-detail', params: { id: data.id } })
  } catch (e) {
    toastError(e, 'Erro ao salvar')
  }
}

async function remove(id: string) {
  if (!confirm('Excluir veículo?')) return
  try {
    await api.delete(`/vehicles/${id}`)
    await load()
  } catch (e) {
    toastError(e, 'Erro ao excluir')
  }
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
        <h1>Automóveis</h1>
        <p class="muted">Cadastro e gestão de veículos do patrimônio.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Novo veículo</button>
    </div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="name">
        <template #cell-paidValue="{ row }">{{ formatMoney(row.paidValue) }}</template>
        <template #cell-fipeValue="{ row }">{{ formatMoney(row.fipeValue) }}</template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton
              label="Detalhes"
              icon="details"
              @click="router.push({ name: 'vehicle-detail', params: { id: row.id } })"
            />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>

    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <form class="modal" @submit.prevent="create">
        <h2>Novo veículo</h2>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>Modelo</label><input v-model="form.model" required /></div>
        <div class="field"><label>Ano</label><input v-model.number="form.year" type="number" min="1900" required /></div>
        <div class="field">
          <label>Foto</label>
          <input type="file" accept="image/*" @change="photoFile = ($event.target as HTMLInputElement).files?.[0] || null" />
        </div>
        <div class="field"><label>Valor pago</label><MoneyInput v-model="form.paidValue" /></div>
        <div class="field"><label>Valor FIPE</label><MoneyInput v-model="form.fipeValue" /></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="closeForm">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
