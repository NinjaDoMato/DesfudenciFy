<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type Property } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

const router = useRouter()
const items = ref<Property[]>([])
const pageError = ref('')
const modalError = ref('')
const showForm = ref(false)
const photoFile = ref<File | null>(null)
const syncRemaining = ref(true)

const form = reactive({
  name: '',
  address: '',
  initialFinancingAmount: 0,
  installmentAmount: 0,
  remainingInstallments: 0,
  remainingBalance: 0,
})

const columns: DataTableColumn<Property>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'address', label: 'Endereço', sortValue: (row) => row.address },
  { key: 'remainingBalance', label: 'Saldo restante', sortValue: (row) => row.remainingBalance },
  { key: 'remainingInstallments', label: 'Parcelas', sortValue: (row) => row.remainingInstallments },
  { key: 'isRented', label: 'Alugado', sortValue: (row) => (row.isRented ? 1 : 0) },
  { key: 'actions', label: '', sortable: false },
]

async function load() {
  const { data } = await api.get<Property[]>('/properties')
  items.value = data
}

function openCreate() {
  modalError.value = ''
  syncRemaining.value = true
  Object.assign(form, {
    name: '',
    address: '',
    initialFinancingAmount: 0,
    installmentAmount: 0,
    remainingInstallments: 0,
    remainingBalance: 0,
  })
  photoFile.value = null
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  modalError.value = ''
  photoFile.value = null
}

watch(
  () => [form.installmentAmount, form.remainingInstallments],
  () => {
    if (!syncRemaining.value) return
    const installment = Number(form.installmentAmount) || 0
    const count = Number(form.remainingInstallments) || 0
    form.remainingBalance = Math.round(installment * count * 100) / 100
  },
)

watch(
  () => form.remainingBalance,
  (value) => {
    const expected = Math.round((Number(form.installmentAmount) || 0) * (Number(form.remainingInstallments) || 0) * 100) / 100
    if (value !== expected) syncRemaining.value = false
  },
)

async function create() {
  modalError.value = ''
  try {
    const { data } = await api.post<Property>('/properties', {
      name: form.name,
      address: form.address,
      initialFinancingAmount: Number(form.initialFinancingAmount),
      installmentAmount: Number(form.installmentAmount),
      remainingInstallments: Number(form.remainingInstallments),
      remainingBalance: Number(form.remainingBalance),
    })
    if (photoFile.value) {
      const fd = new FormData()
      fd.append('file', photoFile.value)
      await api.post(`/properties/${data.id}/photo`, fd)
    }
    closeForm()
    await load()
    await router.push({ name: 'property-detail', params: { id: data.id } })
  } catch (e) {
    modalError.value = e instanceof Error ? e.message : 'Erro ao salvar'
  }
}

async function remove(id: string) {
  if (!confirm('Excluir imóvel?')) return
  pageError.value = ''
  try {
    await api.delete(`/properties/${id}`)
    await load()
  } catch (e) {
    pageError.value = e instanceof Error ? e.message : 'Erro ao excluir'
  }
}

onMounted(async () => {
  try {
    await load()
  } catch (e) {
    pageError.value = e instanceof Error ? e.message : 'Erro'
  }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h1>Imóveis</h1>
        <p class="muted">Investimentos no mercado imobiliário e financiamento.</p>
      </div>
      <button class="btn" type="button" @click="openCreate">Novo imóvel</button>
    </div>
    <div v-if="pageError" class="error">{{ pageError }}</div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="name">
        <template #cell-remainingBalance="{ row }">{{ formatMoney(row.remainingBalance) }}</template>
        <template #cell-isRented="{ row }">
          <span class="badge" :class="row.isRented ? 'success' : 'danger'">
            {{ row.isRented ? 'Alugado' : 'Não alugado' }}
          </span>
        </template>
        <template #cell-actions="{ row }">
          <div class="actions">
            <IconButton
              label="Detalhes"
              icon="details"
              @click="router.push({ name: 'property-detail', params: { id: row.id } })"
            />
            <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
          </div>
        </template>
      </DataTable>
    </div>

    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <form class="modal" @submit.prevent="create">
        <h2>Novo imóvel</h2>
        <div v-if="modalError" class="error">{{ modalError }}</div>
        <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
        <div class="field"><label>Endereço</label><input v-model="form.address" required /></div>
        <div class="field">
          <label>Foto</label>
          <input type="file" accept="image/*" @change="photoFile = ($event.target as HTMLInputElement).files?.[0] || null" />
        </div>
        <div class="field"><label>Valor inicial financiamento</label><MoneyInput v-model="form.initialFinancingAmount" /></div>
        <div class="field"><label>Valor da parcela</label><MoneyInput v-model="form.installmentAmount" /></div>
        <div class="field"><label>Parcelas restantes</label><input v-model.number="form.remainingInstallments" type="number" min="0" /></div>
        <div class="field">
          <label>Valor restante</label>
          <MoneyInput v-model="form.remainingBalance" />
          <span class="muted" style="display:block;margin-top:0.35rem;font-size:0.82rem">
            Calculado automaticamente por parcelas × valor da parcela.
          </span>
        </div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="closeForm">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
