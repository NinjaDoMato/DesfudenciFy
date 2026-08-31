<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import {
  formatMoney,
  type EntryDestination,
  type Reserve,
  type Vehicle,
  type VehicleExpenseType,
} from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

type VehicleExpense = Vehicle['expenses'][number]
type Tone = 'tone-success' | 'tone-danger' | 'tone-warning' | ''

const route = useRoute()
const router = useRouter()
const toast = useToastStore()
const vehicleId = computed(() => String(route.params.id))

const vehicle = ref<Vehicle | null>(null)
const reserves = ref<Reserve[]>([])
const expenseTypes = ref<VehicleExpenseType[]>([])
const loading = ref(true)
const photoFile = ref<File | null>(null)
const photoPreview = ref<string | null>(null)

const form = reactive({
  name: '',
  model: '',
  year: new Date().getFullYear(),
  paidValue: 0,
  fipeValue: 0,
})

const expenseForm = reactive({
  amount: 0,
  expenseTypeId: '',
  observation: '',
  debitCash: false,
  cashDestination: 'FreeBalance' as EntryDestination,
  reserveId: '',
})

const expenseColumns: DataTableColumn<VehicleExpense>[] = [
  { key: 'occurredAt', label: 'Data', sortValue: (row) => new Date(row.occurredAt) },
  { key: 'expenseTypeName', label: 'Tipo', sortValue: (row) => row.expenseTypeName },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'observation', label: 'Obs.', sortValue: (row) => row.observation },
  { key: 'actions', label: '', sortable: false },
]

const defaultExpenseTypeId = computed(() => {
  const docs = expenseTypes.value.find((type) => type.name === 'Documentação')
  return docs?.id ?? expenseTypes.value[0]?.id ?? ''
})

const expensesTone = computed<Tone>(() => {
  if (!vehicle.value) return ''
  return vehicle.value.totalExpenses > 0 ? 'tone-danger' : ''
})

const varianceTone = computed<Tone>(() => {
  if (!vehicle.value) return ''
  const value = vehicle.value.fipeVariance
  if (value > 0) return 'tone-success'
  if (value < 0) return 'tone-danger'
  return 'tone-warning'
})

function syncFormFromVehicle(item: Vehicle) {
  Object.assign(form, {
    name: item.name,
    model: item.model,
    year: item.year,
    paidValue: item.paidValue,
    fipeValue: item.fipeValue,
  })
}

function photoSrc(item: Vehicle) {
  if (!item.photoUrl) return null
  const base = import.meta.env.VITE_API_ORIGIN || ''
  const cacheBust = item.expenses?.length ?? 0
  return `${base}${item.photoUrl}?v=${cacheBust}-${item.id}`
}

function onPhotoSelected(event: Event) {
  const input = event.target
  if (!(input instanceof HTMLInputElement)) return
  const file = input.files?.[0] || null
  photoFile.value = file
  if (photoPreview.value) {
    URL.revokeObjectURL(photoPreview.value)
    photoPreview.value = null
  }
  if (file) {
    photoPreview.value = URL.createObjectURL(file)
  }
}

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  loading.value = true
  try {
    const [vehicleRes, reservesRes, typesRes] = await Promise.all([
      api.get<Vehicle>(`/vehicles/${vehicleId.value}`),
      api.get<Reserve[]>('/reserves'),
      api.get<VehicleExpenseType[]>('/lookups/vehicle-expense-types'),
    ])
    vehicle.value = vehicleRes.data
    reserves.value = reservesRes.data
    expenseTypes.value = typesRes.data
    if (!expenseForm.expenseTypeId) {
      expenseForm.expenseTypeId = defaultExpenseTypeId.value
    }
    syncFormFromVehicle(vehicleRes.data)
  } catch (e) {
    toastError(e, 'Erro ao carregar veículo')
  } finally {
    loading.value = false
  }
}

async function save() {
  try {
    const { data } = await api.put<Vehicle>(`/vehicles/${vehicleId.value}`, {
      name: form.name,
      model: form.model,
      year: Number(form.year),
      paidValue: Number(form.paidValue),
      fipeValue: Number(form.fipeValue),
    })

    if (photoFile.value) {
      const fd = new FormData()
      fd.append('file', photoFile.value)
      const photoRes = await api.post<Vehicle>(`/vehicles/${vehicleId.value}/photo`, fd)
      vehicle.value = photoRes.data
      photoFile.value = null
      if (photoPreview.value) {
        URL.revokeObjectURL(photoPreview.value)
        photoPreview.value = null
      }
    } else {
      vehicle.value = data
    }

    syncFormFromVehicle(vehicle.value)
    toast.success('Veículo atualizado.')
  } catch (e) {
    toastError(e, 'Erro ao atualizar veículo')
  }
}

async function addExpense() {
  try {
    await api.post(`/vehicles/${vehicleId.value}/expenses`, {
      amount: Number(expenseForm.amount),
      expenseTypeId: expenseForm.expenseTypeId,
      observation: expenseForm.observation,
      debitCash: expenseForm.debitCash,
      cashDestination: expenseForm.debitCash ? expenseForm.cashDestination : null,
      reserveId: expenseForm.debitCash && expenseForm.cashDestination === 'Reserve' ? expenseForm.reserveId : null,
    })
    Object.assign(expenseForm, {
      amount: 0,
      expenseTypeId: defaultExpenseTypeId.value,
      observation: '',
      debitCash: false,
      cashDestination: 'FreeBalance',
      reserveId: '',
    })
    await load()
    toast.success('Gasto registrado.')
  } catch (e) {
    toastError(e, 'Erro ao registrar gasto')
  }
}

async function removeExpense(expenseId: string) {
  if (!confirm('Excluir este gasto?')) return
  try {
    await api.delete(`/vehicles/${vehicleId.value}/expenses/${expenseId}`)
    await load()
    toast.success('Gasto removido.')
  } catch (e) {
    toastError(e, 'Erro ao excluir gasto')
  }
}

onMounted(load)

watch(vehicleId, () => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Gerenciar</p>
        <h1>{{ vehicle?.name || 'Veículo' }}</h1>
        <p class="muted">Dados do veículo, custos e variação FIPE.</p>
      </div>
      <div class="actions">
        <button class="btn secondary" type="button" @click="router.push({ name: 'vehicles' })">Voltar</button>
      </div>
    </div>

    <p v-if="loading" class="muted">Carregando...</p>

    <template v-else-if="vehicle">
      <div class="detail-layout">
        <form class="panel" @submit.prevent="save">
          <h2>Dados do veículo</h2>
          <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
          <div class="field"><label>Modelo</label><input v-model="form.model" required /></div>
          <div class="field"><label>Ano</label><input v-model.number="form.year" type="number" min="1900" required /></div>
          <div class="field"><label>Valor pago</label><MoneyInput v-model="form.paidValue" /></div>
          <div class="field"><label>Valor FIPE</label><MoneyInput v-model="form.fipeValue" /></div>
          <div class="field">
            <label>Foto</label>
            <input type="file" accept="image/*" @change="onPhotoSelected" />
          </div>
          <img
            v-if="photoPreview || photoSrc(vehicle)"
            class="property-photo"
            :src="photoPreview || photoSrc(vehicle)!"
            :alt="form.name"
          />

          <div class="actions">
            <button class="btn" type="submit">Atualizar veículo</button>
          </div>
        </form>

        <div class="side-stack">
          <div class="panel overview-panel">
            <div class="overview-heading">
              <h2>Totalizadores</h2>
            </div>

            <section class="overview-section">
              <div class="kpi-row kpi-row-primary">
                <div class="kpi">
                  <div class="label">Total de gasto</div>
                  <div class="value" :class="expensesTone">{{ formatMoney(vehicle.totalExpenses) }}</div>
                </div>
                <div class="kpi">
                  <div class="label">Variação FIPE</div>
                  <div class="value" :class="varianceTone">{{ formatMoney(vehicle.fipeVariance) }}</div>
                  <p class="muted hint">FIPE − (valor pago + custos)</p>
                </div>
              </div>
            </section>
          </div>

          <div class="panel tabs-panel">
            <h2>Custos</h2>
            <div class="tab-content split-launch">
              <form class="launch-panel" @submit.prevent="addExpense">
                <h3>Registrar gasto</h3>
                <div class="field">
                  <label>Tipo</label>
                  <select v-model="expenseForm.expenseTypeId" required>
                    <option disabled value="">Selecione</option>
                    <option v-for="t in expenseTypes" :key="t.id" :value="t.id">{{ t.name }}</option>
                  </select>
                </div>
                <div class="field"><label>Valor</label><MoneyInput v-model="expenseForm.amount" /></div>
                <div class="field"><label>Observação</label><input v-model="expenseForm.observation" required /></div>
                <div class="field">
                  <label><input v-model="expenseForm.debitCash" type="checkbox" /> Debitar do caixa</label>
                </div>
                <template v-if="expenseForm.debitCash">
                  <div class="field">
                    <label>Origem do débito</label>
                    <select v-model="expenseForm.cashDestination">
                      <option value="FreeBalance">Saldo livre</option>
                      <option value="Reserve">Reserva</option>
                    </select>
                  </div>
                  <div v-if="expenseForm.cashDestination === 'Reserve'" class="field">
                    <label>Reserva</label>
                    <select v-model="expenseForm.reserveId" required>
                      <option disabled value="">Selecione</option>
                      <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
                    </select>
                  </div>
                </template>
                <button class="btn" type="submit">Adicionar gasto</button>
              </form>

              <div class="history-panel">
                <h3>Gastos do veículo</h3>
                <DataTable
                  :rows="vehicle.expenses"
                  :columns="expenseColumns"
                  row-key="id"
                  :page-size="5"
                  initial-sort-key="occurredAt"
                  initial-sort-dir="desc"
                  empty-text="Nenhum gasto registrado."
                >
                  <template #cell-occurredAt="{ row }">{{ new Date(row.occurredAt).toLocaleDateString('pt-BR') }}</template>
                  <template #cell-expenseTypeName="{ row }">{{ row.expenseTypeName }}</template>
                  <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
                  <template #cell-actions="{ row }">
                    <IconButton label="Excluir" icon="delete" variant="danger" @click="removeExpense(row.id)" />
                  </template>
                </DataTable>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.eyebrow {
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  font-size: 0.75rem;
  color: var(--accent);
  font-weight: 700;
}

.detail-layout {
  display: grid;
  grid-template-columns: minmax(280px, 380px) 1fr;
  gap: 1rem;
  align-items: start;
}

.side-stack {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-width: 0;
}

.overview-panel {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.overview-heading {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem 0.65rem;
  margin-bottom: 1rem;
}

.overview-heading h2 {
  margin: 0;
}

.overview-section + .overview-section {
  margin-top: 1.1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}

.kpi-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 10rem), 1fr));
  gap: 0.75rem;
}

.kpi {
  width: auto;
  min-width: 0;
  box-sizing: border-box;
  overflow: hidden;
  padding: 0.95rem 1rem;
}

.kpi .label {
  color: var(--muted);
  font-size: 0.82rem;
}

.kpi .value {
  margin-top: 0.35rem;
  font-size: clamp(0.95rem, 1.8vw, 1.25rem);
  font-weight: 700;
  letter-spacing: -0.02em;
  line-height: 1.25;
}

.kpi .value.tone-success { color: var(--success); }
.kpi .value.tone-danger { color: var(--danger); }
.kpi .value.tone-warning { color: var(--warning); }

.hint {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.82rem;
}

.tabs-panel h2 {
  margin-top: 0;
}

.split-launch {
  display: grid;
  grid-template-columns: minmax(220px, 280px) 1fr;
  gap: 1rem;
  align-items: start;
}

.launch-panel h3,
.history-panel h3 {
  margin-top: 0;
}

@media (max-width: 900px) {
  .detail-layout,
  .split-launch {
    grid-template-columns: 1fr;
  }
}
</style>
