<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const router = useRouter()
const toast = useToastStore()
const items = ref<Reserve[]>([])
const nameFilter = ref('')
const showForm = ref(false)
const form = reactive({
  name: '',
  description: '',
  goal: 0,
  displayColor: '#38bdf8',
  monthlyGoal: 0,
})

const columns: DataTableColumn<Reserve>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'currentValue', label: 'Atual', sortValue: (row) => row.currentValue },
  { key: 'investedValue', label: 'Investido', sortValue: (row) => row.investedValue },
  { key: 'availableValue', label: 'Disponível', sortValue: (row) => row.availableValue },
  { key: 'goal', label: 'Meta', sortValue: (row) => row.goal },
  { key: 'actions', label: '', sortable: false },
]

const filteredItems = computed(() => {
  const term = nameFilter.value.trim().toLowerCase()
  if (!term) return items.value
  return items.value.filter((item) => item.name.toLowerCase().includes(term))
})

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  const { data } = await api.get<Reserve[]>('/reserves')
  items.value = data
}

function openCreate() {
  Object.assign(form, { name: '', description: '', goal: 0, displayColor: '#38bdf8', monthlyGoal: 0 })
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function save() {
  try {
    const payload = {
      name: form.name,
      description: form.description,
      goal: Number(form.goal),
      displayColor: form.displayColor,
      monthlyGoal: Number(form.monthlyGoal) || null,
    }
    await api.post('/reserves', payload)
    closeForm()
    await load()
  } catch (e) {
    toastError(e, 'Erro ao salvar')
  }
}

async function remove(id: string) {
  if (!confirm('Excluir reserva?')) return
  try {
    await api.delete(`/reserves/${id}`)
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
        <h1>Reservas</h1>
        <p class="muted">Metas e capital alocado, sem donos.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Nova reserva</button>
    </div>
    <div class="panel">
      <div class="filters filters-inline">
        <div class="field filter-name">
          <label>Nome</label>
          <input v-model="nameFilter" type="search" placeholder="Filtrar por nome" />
        </div>
      </div>
      <DataTable :rows="filteredItems" :columns="columns" row-key="id" initial-sort-key="name">
        <template #cell-name="{ row }">
          <span class="color-dot" :style="{ background: row.displayColor || '#38bdf8' }" />
          {{ row.name }}
        </template>
        <template #cell-currentValue="{ row }">{{ formatMoney(row.currentValue) }}</template>
        <template #cell-investedValue="{ row }">{{ formatMoney(row.investedValue) }}</template>
        <template #cell-availableValue="{ row }">{{ formatMoney(row.availableValue) }}</template>
        <template #cell-goal="{ row }">{{ formatMoney(row.goal) }}</template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton
              label="Detalhes"
              icon="details"
              @click="router.push({ name: 'reserve-detail', params: { id: row.id } })"
            />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>
    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <form class="modal" @submit.prevent="save">
        <h2>Nova reserva</h2>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="3" /></div>
        <div class="field"><label>Meta</label><MoneyInput v-model="form.goal" /></div>
        <div class="field"><label>Meta mensal</label><MoneyInput v-model="form.monthlyGoal" /></div>
        <div class="field"><label>Cor</label><input v-model="form.displayColor" type="color" /></div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="closeForm">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.color-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-right: 0.4rem;
}
</style>
