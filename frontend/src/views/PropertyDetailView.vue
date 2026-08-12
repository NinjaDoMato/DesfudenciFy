<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type EntryDestination, type Property, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

type PropertyAmortization = Property['amortizations'][number]
type PropertyExpense = Property['expenses'][number]
type PropertyRentPayment = Property['rentPayments'][number]
type DetailTab = 'amortization' | 'rent' | 'costs'
type Tone = 'tone-success' | 'tone-danger' | 'tone-warning' | ''

const route = useRoute()
const router = useRouter()
const propertyId = computed(() => String(route.params.id))

const property = ref<Property | null>(null)
const reserves = ref<Reserve[]>([])
const error = ref('')
const success = ref('')
const loading = ref(true)
const activeTab = ref<DetailTab>('amortization')
const photoFile = ref<File | null>(null)
const photoPreview = ref<string | null>(null)

const form = reactive({
  name: '',
  address: '',
  isRented: false,
  appraisedValue: 0,
  rentalAmount: 0,
  initialFinancingAmount: 0,
  installmentAmount: 0,
  remainingInstallments: 0,
  remainingBalance: 0,
})

const amortForm = reactive({
  amount: 0,
  installmentsAmortized: 1,
  observation: '',
  debitCash: true,
  cashDestination: 'FreeBalance' as EntryDestination,
  reserveId: '',
  syncAmountFromInstallments: true,
})

const expenseForm = reactive({
  amount: 0,
  observation: '',
  debitCash: false,
  cashDestination: 'FreeBalance' as EntryDestination,
  reserveId: '',
})

const rentForm = reactive({
  amount: 0,
  observation: '',
  paidAt: new Date().toISOString().slice(0, 10),
})

const amortizationColumns: DataTableColumn<PropertyAmortization>[] = [
  { key: 'paidAt', label: 'Data', sortValue: (row) => new Date(row.paidAt) },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'installmentsAmortized', label: 'Parcelas', sortValue: (row) => row.installmentsAmortized },
  { key: 'observation', label: 'Obs.', sortValue: (row) => row.observation || '' },
  { key: 'actions', label: '', sortable: false },
]

const expenseColumns: DataTableColumn<PropertyExpense>[] = [
  { key: 'occurredAt', label: 'Data', sortValue: (row) => new Date(row.occurredAt) },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'observation', label: 'Obs.', sortValue: (row) => row.observation },
  { key: 'actions', label: '', sortable: false },
]

const rentColumns: DataTableColumn<PropertyRentPayment>[] = [
  { key: 'paidAt', label: 'Data', sortValue: (row) => new Date(row.paidAt) },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'observation', label: 'Obs.', sortValue: (row) => row.observation || '' },
  { key: 'actions', label: '', sortable: false },
]

const projectedRemainingBalance = computed(() => {
  if (!property.value) return 0
  return Math.max(0, Math.round((property.value.remainingBalance - Number(amortForm.amount || 0)) * 100) / 100)
})

const projectedRemainingInstallments = computed(() => {
  if (!property.value) return 0
  return Math.max(0, property.value.remainingInstallments - Number(amortForm.installmentsAmortized || 0))
})

const returnTone = computed<Tone>(() => {
  if (!property.value) return ''
  const value = property.value.propertyReturn
  if (value > 0) return 'tone-success'
  if (value < 0) return 'tone-danger'
  return 'tone-warning'
})

const expensesTone = computed<Tone>(() => {
  if (!property.value) return ''
  return property.value.totalExpenses > 0 ? 'tone-danger' : ''
})

const rentTone = computed<Tone>(() => {
  if (!property.value) return ''
  return property.value.totalRentPaid > 0 ? 'tone-success' : ''
})

const appraisedTone = computed<Tone>(() => {
  if (!property.value) return ''
  const appraised = property.value.appraisedValue
  const financing = property.value.initialFinancingAmount
  const costFloor = financing + property.value.totalExpenses
  if (Math.abs(appraised - financing) < 0.005) return 'tone-warning'
  if (appraised < costFloor) return 'tone-danger'
  if (appraised > financing) return 'tone-success'
  return ''
})

function syncFormFromProperty(item: Property) {
  Object.assign(form, {
    name: item.name,
    address: item.address,
    isRented: item.isRented,
    appraisedValue: item.appraisedValue,
    rentalAmount: item.rentalAmount,
    initialFinancingAmount: item.initialFinancingAmount,
    installmentAmount: item.installmentAmount,
    remainingInstallments: item.remainingInstallments,
    remainingBalance: item.remainingBalance,
  })
}

function syncAmortAmountFromInstallments() {
  if (!property.value || !amortForm.syncAmountFromInstallments) return
  const count = Number(amortForm.installmentsAmortized) || 0
  amortForm.amount = Math.round(property.value.installmentAmount * count * 100) / 100
}

function resetAmortForm() {
  Object.assign(amortForm, {
    amount: property.value?.installmentAmount ?? 0,
    installmentsAmortized: 1,
    observation: '',
    debitCash: true,
    cashDestination: 'FreeBalance',
    reserveId: '',
    syncAmountFromInstallments: true,
  })
}

function photoSrc(item: Property) {
  if (!item.photoUrl) return null
  const base = import.meta.env.VITE_API_ORIGIN || ''
  const cacheBust = item.amortizations?.length ?? 0
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

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [propertyRes, reservesRes] = await Promise.all([
      api.get<Property>(`/properties/${propertyId.value}`),
      api.get<Reserve[]>('/reserves'),
    ])
    property.value = propertyRes.data
    reserves.value = reservesRes.data
    syncFormFromProperty(propertyRes.data)
    if (propertyRes.data.isRented && !Number(rentForm.amount)) {
      rentForm.amount = propertyRes.data.rentalAmount
    }
    if (!Number(amortForm.amount)) {
      amortForm.amount = propertyRes.data.installmentAmount
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao carregar imóvel'
  } finally {
    loading.value = false
  }
}

async function save() {
  error.value = ''
  success.value = ''
  try {
    const { data } = await api.put<Property>(`/properties/${propertyId.value}`, {
      name: form.name,
      address: form.address,
      isRented: form.isRented,
      appraisedValue: Number(form.appraisedValue),
      rentalAmount: Number(form.rentalAmount),
      initialFinancingAmount: Number(form.initialFinancingAmount),
      installmentAmount: Number(form.installmentAmount),
      remainingInstallments: Number(form.remainingInstallments),
      remainingBalance: Number(form.remainingBalance),
    })

    if (photoFile.value) {
      const fd = new FormData()
      fd.append('file', photoFile.value)
      const photoRes = await api.post<Property>(`/properties/${propertyId.value}/photo`, fd)
      property.value = photoRes.data
      photoFile.value = null
      if (photoPreview.value) {
        URL.revokeObjectURL(photoPreview.value)
        photoPreview.value = null
      }
    } else {
      property.value = data
    }

    syncFormFromProperty(property.value)
    success.value = 'Imóvel atualizado.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao atualizar imóvel'
  }
}

async function amortize() {
  error.value = ''
  success.value = ''
  try {
    await api.post(`/properties/${propertyId.value}/amortizations`, {
      amount: Number(amortForm.amount),
      installmentsAmortized: Number(amortForm.installmentsAmortized),
      observation: amortForm.observation,
      debitCash: amortForm.debitCash,
      cashDestination: amortForm.debitCash ? amortForm.cashDestination : null,
      reserveId: amortForm.debitCash && amortForm.cashDestination === 'Reserve' ? amortForm.reserveId : null,
    })
    resetAmortForm()
    await load()
    success.value = 'Amortização registrada.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro na amortização'
  }
}

async function removeAmortization(amortizationId: string) {
  if (!confirm('Excluir esta amortização e estornar os valores?')) return
  error.value = ''
  try {
    await api.delete(`/properties/${propertyId.value}/amortizations/${amortizationId}`)
    await load()
    success.value = 'Amortização removida.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao excluir amortização'
  }
}

async function addExpense() {
  error.value = ''
  success.value = ''
  try {
    await api.post(`/properties/${propertyId.value}/expenses`, {
      amount: Number(expenseForm.amount),
      observation: expenseForm.observation,
      debitCash: expenseForm.debitCash,
      cashDestination: expenseForm.debitCash ? expenseForm.cashDestination : null,
      reserveId: expenseForm.debitCash && expenseForm.cashDestination === 'Reserve' ? expenseForm.reserveId : null,
    })
    Object.assign(expenseForm, {
      amount: 0,
      observation: '',
      debitCash: false,
      cashDestination: 'FreeBalance',
      reserveId: '',
    })
    await load()
    success.value = 'Gasto registrado.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao registrar gasto'
  }
}

async function removeExpense(expenseId: string) {
  if (!confirm('Excluir este gasto?')) return
  error.value = ''
  try {
    await api.delete(`/properties/${propertyId.value}/expenses/${expenseId}`)
    await load()
    success.value = 'Gasto removido.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao excluir gasto'
  }
}

async function addRentPayment() {
  error.value = ''
  success.value = ''
  try {
    await api.post(`/properties/${propertyId.value}/rent-payments`, {
      amount: Number(rentForm.amount),
      observation: rentForm.observation || null,
      paidAt: rentForm.paidAt ? new Date(`${rentForm.paidAt}T12:00:00`).toISOString() : null,
    })
    Object.assign(rentForm, {
      amount: property.value?.rentalAmount ?? 0,
      observation: '',
      paidAt: new Date().toISOString().slice(0, 10),
    })
    await load()
    success.value = 'Aluguel registrado no caixa.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao registrar aluguel'
  }
}

async function removeRentPayment(paymentId: string) {
  if (!confirm('Excluir este pagamento de aluguel e estornar o lançamento?')) return
  error.value = ''
  try {
    await api.delete(`/properties/${propertyId.value}/rent-payments/${paymentId}`)
    await load()
    success.value = 'Pagamento de aluguel removido.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao excluir pagamento de aluguel'
  }
}

onMounted(load)

watch(propertyId, () => {
  void load()
})

watch(
  () => amortForm.installmentsAmortized,
  () => {
    syncAmortAmountFromInstallments()
  },
)

watch(
  () => amortForm.amount,
  (value) => {
    if (!property.value) return
    const expected = Math.round(property.value.installmentAmount * Number(amortForm.installmentsAmortized || 0) * 100) / 100
    if (value !== expected) {
      amortForm.syncAmountFromInstallments = false
    }
  },
)
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Gerenciar</p>
        <h1>{{ property?.name || 'Imóvel' }}</h1>
        <p class="muted">Dados do imóvel, custos, aluguéis, financiamento e amortizações.</p>
      </div>
      <button class="btn secondary" type="button" @click="router.push({ name: 'properties' })">Voltar</button>
    </div>

    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>
    <p v-if="loading" class="muted">Carregando...</p>

    <template v-else-if="property">
      <div class="detail-layout">
        <form class="panel" @submit.prevent="save">
          <h2>Dados do imóvel</h2>
          <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
          <div class="field"><label>Endereço</label><input v-model="form.address" required /></div>
          <div class="field"><label>Valor avaliado</label><MoneyInput v-model="form.appraisedValue" /></div>
          <div class="field"><label>Valor do aluguel</label><MoneyInput v-model="form.rentalAmount" /></div>
          <div class="field">
            <label><input v-model="form.isRented" type="checkbox" /> Alugado</label>
            <span class="muted hint">Ao marcar como alugado, uma entrada do tipo Aluguel é criada/atualizada.</span>
          </div>
          <div class="field">
            <label>Foto</label>
            <input type="file" accept="image/*" @change="onPhotoSelected" />
          </div>
          <img
            v-if="photoPreview || photoSrc(property)"
            class="property-photo"
            :src="photoPreview || photoSrc(property)!"
            :alt="form.name"
          />

          <h3 class="section-title">Financiamento</h3>
          <div class="field"><label>Valor inicial do financiamento</label><MoneyInput v-model="form.initialFinancingAmount" /></div>
          <div class="field"><label>Valor das parcelas</label><MoneyInput v-model="form.installmentAmount" /></div>
          <div class="field"><label>Parcelas restantes</label><input v-model.number="form.remainingInstallments" type="number" min="0" /></div>
          <div class="field">
            <label>Valor restante a pagar</label>
            <MoneyInput v-model="form.remainingBalance" />
            <span class="muted hint">Atualizado automaticamente apenas ao registrar amortizações.</span>
          </div>

          <div class="actions">
            <button class="btn" type="submit">Atualizar imóvel</button>
          </div>
        </form>

        <div class="side-stack">
          <div class="panel">
            <h2>Totalizadores</h2>

            <div class="kpi-group">
              <h3 class="kpi-group-title">Resultado</h3>
              <div class="kpi-row">
                <div class="kpi">
                  <div class="label">Valor avaliado</div>
                  <div class="value" :class="appraisedTone">{{ formatMoney(property.appraisedValue) }}</div>
                </div>
                <div class="kpi">
                  <div class="label">Custo do imóvel</div>
                  <div class="value">{{ formatMoney(property.propertyCost) }}</div>
                </div>
                <div class="kpi">
                  <div class="label">Retorno</div>
                  <div class="value" :class="returnTone">{{ formatMoney(property.propertyReturn) }}</div>
                </div>
              </div>
            </div>

            <div class="kpi-group">
              <h3 class="kpi-group-title">Movimentações</h3>
              <div class="kpi-row">
                <div class="kpi">
                  <div class="label">Total de gastos</div>
                  <div class="value" :class="expensesTone">{{ formatMoney(property.totalExpenses) }}</div>
                </div>
                <div class="kpi">
                  <div class="label">Aluguéis pagos</div>
                  <div class="value" :class="rentTone">{{ formatMoney(property.totalRentPaid) }}</div>
                </div>
              </div>
            </div>

            <div class="kpi-group">
              <h3 class="kpi-group-title">Financiamento</h3>
              <div class="kpi-row">
                <div class="kpi">
                  <div class="label">Financiamento inicial</div>
                  <div class="value">{{ formatMoney(property.initialFinancingAmount) }}</div>
                </div>
                <div class="kpi">
                  <div class="label">Parcela</div>
                  <div class="value">{{ formatMoney(property.installmentAmount) }}</div>
                </div>
                <div class="kpi">
                  <div class="label">Restante</div>
                  <div class="value">{{ formatMoney(property.remainingBalance) }}</div>
                </div>
              </div>
            </div>
          </div>

          <div class="panel tabs-panel">
            <div class="tabs" role="tablist">
              <button
                type="button"
                class="tab"
                :class="{ active: activeTab === 'amortization' }"
                role="tab"
                @click="activeTab = 'amortization'"
              >
                Amortização
              </button>
              <button
                type="button"
                class="tab"
                :class="{ active: activeTab === 'rent' }"
                role="tab"
                @click="activeTab = 'rent'"
              >
                Aluguel
              </button>
              <button
                type="button"
                class="tab"
                :class="{ active: activeTab === 'costs' }"
                role="tab"
                @click="activeTab = 'costs'"
              >
                Custos
              </button>
            </div>

            <div v-if="activeTab === 'amortization'" class="tab-content split-launch" role="tabpanel">
              <form class="launch-panel" @submit.prevent="amortize">
                <h3>Registrar amortização</h3>
                <div class="field">
                  <label>Parcelas amortizadas</label>
                  <input
                    v-model.number="amortForm.installmentsAmortized"
                    type="number"
                    min="0"
                    :max="property.remainingInstallments"
                    required
                  />
                </div>
                <div class="field">
                  <label>Valor pago</label>
                  <MoneyInput v-model="amortForm.amount" required />
                  <span class="muted hint">Preenchido automaticamente com parcelas × valor da parcela.</span>
                </div>
                <div class="projection">
                  <div><span class="muted">Saldo restante atual:</span> {{ formatMoney(property.remainingBalance) }}</div>
                  <div><span class="muted">Novo saldo restante:</span> <strong>{{ formatMoney(projectedRemainingBalance) }}</strong></div>
                  <div><span class="muted">Novas parcelas restantes:</span> <strong>{{ projectedRemainingInstallments }}</strong></div>
                </div>
                <div class="field"><label>Observação</label><input v-model="amortForm.observation" /></div>
                <div class="field">
                  <label><input v-model="amortForm.debitCash" type="checkbox" /> Debitar do caixa</label>
                </div>
                <template v-if="amortForm.debitCash">
                  <div class="field">
                    <label>Origem do débito</label>
                    <select v-model="amortForm.cashDestination">
                      <option value="FreeBalance">Saldo livre</option>
                      <option value="Reserve">Reserva</option>
                    </select>
                  </div>
                  <div v-if="amortForm.cashDestination === 'Reserve'" class="field">
                    <label>Reserva</label>
                    <select v-model="amortForm.reserveId" required>
                      <option disabled value="">Selecione</option>
                      <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
                    </select>
                  </div>
                </template>
                <button class="btn" type="submit">Confirmar amortização</button>
              </form>

              <div class="history-panel">
                <h3>Histórico de amortizações</h3>
                <DataTable
                  :rows="property.amortizations"
                  :columns="amortizationColumns"
                  row-key="id"
                  :page-size="5"
                  initial-sort-key="paidAt"
                  empty-text="Nenhuma amortização registrada."
                >
                  <template #cell-paidAt="{ row }">{{ new Date(row.paidAt).toLocaleDateString('pt-BR') }}</template>
                  <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
                  <template #cell-observation="{ row }">{{ row.observation || '-' }}</template>
                  <template #cell-actions="{ row }">
                    <IconButton label="Excluir" icon="delete" variant="danger" @click="removeAmortization(row.id)" />
                  </template>
                </DataTable>
              </div>
            </div>

            <div v-else-if="activeTab === 'rent'" class="tab-content split-launch" role="tabpanel">
              <form class="launch-panel" @submit.prevent="addRentPayment">
                <h3>Registrar aluguel</h3>
                <div class="field"><label>Valor</label><MoneyInput v-model="rentForm.amount" required /></div>
                <div class="field"><label>Data</label><input v-model="rentForm.paidAt" type="date" required /></div>
                <div class="field"><label>Observação</label><input v-model="rentForm.observation" placeholder="Opcional" /></div>
                <button class="btn" type="submit">Registrar pagamento</button>
              </form>

              <div class="history-panel">
                <h3>Pagamentos de aluguel</h3>
                <DataTable
                  :rows="property.rentPayments"
                  :columns="rentColumns"
                  row-key="id"
                  :paginated="false"
                  initial-sort-key="paidAt"
                  empty-text="Nenhum aluguel registrado."
                >
                  <template #cell-paidAt="{ row }">{{ new Date(row.paidAt).toLocaleDateString('pt-BR') }}</template>
                  <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
                  <template #cell-observation="{ row }">{{ row.observation || '-' }}</template>
                  <template #cell-actions="{ row }">
                    <IconButton label="Excluir" icon="delete" variant="danger" @click="removeRentPayment(row.id)" />
                  </template>
                </DataTable>
              </div>
            </div>

            <div v-else class="tab-content split-launch" role="tabpanel">
              <form class="launch-panel" @submit.prevent="addExpense">
                <h3>Registrar gasto</h3>
                <div class="field"><label>Valor</label><MoneyInput v-model="expenseForm.amount" required /></div>
                <div class="field">
                  <label>Observação</label>
                  <input v-model="expenseForm.observation" required placeholder="Ex.: Contratado eletricista" />
                </div>
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
                <h3>Gastos do imóvel</h3>
                <DataTable
                  :rows="property.expenses"
                  :columns="expenseColumns"
                  row-key="id"
                  :paginated="false"
                  initial-sort-key="occurredAt"
                  empty-text="Nenhum gasto registrado."
                >
                  <template #cell-occurredAt="{ row }">{{ new Date(row.occurredAt).toLocaleDateString('pt-BR') }}</template>
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

.kpi-group {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.kpi-group + .kpi-group {
  margin-top: 1.1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}

.kpi-group-title {
  margin: 0;
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--muted);
}

.kpi-row {
  display: grid;
  grid-template-columns: repeat(auto-fill, 180px);
  justify-content: start;
  gap: 0.75rem;
}

.kpi-row :deep(.kpi),
.kpi {
  width: 180px;
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
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.kpi .value.tone-success { color: var(--success); }
.kpi .value.tone-danger { color: var(--danger); }
.kpi .value.tone-warning { color: var(--warning); }

.section-title {
  margin: 1rem 0 0.75rem;
  font-size: 1rem;
}

.hint {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.82rem;
}

.tabs-panel {
  padding-top: 0.85rem;
}

.tabs {
  display: flex;
  gap: 0.35rem;
  flex-wrap: wrap;
  margin-bottom: 1rem;
  border-bottom: 1px solid var(--border);
  padding-bottom: 0.65rem;
}

.tab {
  appearance: none;
  border: 1px solid transparent;
  background: transparent;
  color: var(--muted);
  border-radius: 999px;
  padding: 0.45rem 0.9rem;
  cursor: pointer;
  font-weight: 600;
}

.tab:hover {
  color: var(--text);
  background: var(--bg-soft);
}

.tab.active {
  color: var(--accent);
  background: var(--accent-soft);
  border-color: rgba(56, 189, 248, 0.28);
}

.tab-content {
  min-width: 0;
}

.split-launch {
  display: grid;
  grid-template-columns: minmax(260px, 340px) 1fr;
  gap: 1rem;
  align-items: start;
}

.launch-panel,
.history-panel {
  min-width: 0;
}

.launch-panel {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  min-height: 28rem;
  padding: 1rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: rgba(255, 255, 255, 0.02);
  overflow: visible;
}

.history-panel {
  padding: 1rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: rgba(255, 255, 255, 0.02);
}

.launch-panel h3,
.history-panel h3 {
  margin: 0 0 0.25rem;
  font-size: 1rem;
}

.projection {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  padding: 0.75rem 0.9rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: rgba(255, 255, 255, 0.02);
  font-size: 0.92rem;
}

.success {
  color: var(--success);
  background: rgba(74, 222, 128, 0.1);
  border: 1px solid rgba(74, 222, 128, 0.28);
  padding: 0.75rem 1rem;
  border-radius: var(--radius-sm);
}

@media (max-width: 1100px) {
  .split-launch {
    grid-template-columns: 1fr;
  }

  .launch-panel {
    min-height: auto;
  }
}

@media (max-width: 1000px) {
  .detail-layout {
    grid-template-columns: 1fr;
  }
}
</style>
