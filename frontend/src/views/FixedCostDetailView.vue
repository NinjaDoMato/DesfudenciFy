<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type CostPayment, type FixedCost, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'

const route = useRoute()
const router = useRouter()
const costId = computed(() => String(route.params.id))

const cost = ref<FixedCost | null>(null)
const reserves = ref<Reserve[]>([])
const error = ref('')
const success = ref('')
const loading = ref(true)
const paying = ref(false)

const form = reactive({
  name: '',
  description: '',
  amount: 0,
  recurrence: 'Month',
  dueDate: '',
  reserveId: '',
})

const payForm = reactive({
  paidAmount: 0,
})

const recurrenceLabel: Record<string, string> = {
  Day: 'Diária',
  Week: 'Semanal',
  Month: 'Mensal',
  Year: 'Anual',
}

const paymentColumns: DataTableColumn<CostPayment>[] = [
  { key: 'datePaid', label: 'Data', sortValue: (row) => new Date(row.datePaid) },
  { key: 'paidAmount', label: 'Valor', sortValue: (row) => row.paidAmount },
  { key: 'actions', label: '', sortable: false },
]

const payments = computed(() => cost.value?.payments ?? [])
const totalPaid = computed(() => payments.value.reduce((sum, p) => sum + p.paidAmount, 0))

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [costRes, reservesRes] = await Promise.all([
      api.get<FixedCost>(`/fixed-costs/${costId.value}`),
      api.get<Reserve[]>('/reserves'),
    ])
    cost.value = costRes.data
    reserves.value = reservesRes.data
    Object.assign(form, {
      name: costRes.data.name,
      description: costRes.data.description,
      amount: costRes.data.amount,
      recurrence: costRes.data.recurrence,
      dueDate: costRes.data.dueDate ? costRes.data.dueDate.slice(0, 10) : '',
      reserveId: costRes.data.reserveId || '',
    })
    payForm.paidAmount = costRes.data.amount
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao carregar conta'
  } finally {
    loading.value = false
  }
}

async function saveCost() {
  error.value = ''
  success.value = ''
  try {
    const { data } = await api.put<FixedCost>(`/fixed-costs/${costId.value}`, {
      name: form.name,
      description: form.description,
      amount: Number(form.amount),
      recurrence: form.recurrence,
      dueDate: form.dueDate || null,
      reserveId: form.reserveId || null,
    })
    cost.value = data
    success.value = 'Conta atualizada.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao atualizar'
  }
}

async function pay() {
  error.value = ''
  success.value = ''
  paying.value = true
  try {
    await api.post(`/fixed-costs/${costId.value}/payments`, {
      paidAmount: Number(payForm.paidAmount),
    })
    await load()
    success.value = 'Pagamento registrado.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao pagar'
  } finally {
    paying.value = false
  }
}

async function removePayment(paymentId: string) {
  if (!confirm('Remover este pagamento?')) return
  error.value = ''
  success.value = ''
  try {
    await api.delete(`/fixed-costs/${costId.value}/payments/${paymentId}`)
    await load()
    success.value = 'Pagamento removido.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Erro ao remover pagamento'
  }
}

onMounted(load)
watch(costId, () => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Gerenciar</p>
        <h1>{{ cost?.name || 'Conta fixa' }}</h1>
        <p class="muted">Dados da conta, histórico e registro de pagamentos.</p>
      </div>
      <button class="btn secondary" type="button" @click="router.push({ name: 'fixed-costs' })">Voltar</button>
    </div>

    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>
    <p v-if="loading" class="muted">Carregando...</p>

    <template v-else-if="cost">
      <div class="detail-layout">
        <form class="panel" @submit.prevent="saveCost">
          <h2>Dados</h2>
          <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
          <div class="field"><label>Descrição</label><textarea v-model="form.description" rows="3" /></div>
          <div class="field"><label>Valor</label><MoneyInput v-model="form.amount" required /></div>
          <div class="field">
            <label>Recorrência</label>
            <select v-model="form.recurrence">
              <option value="Day">Diária</option>
              <option value="Week">Semanal</option>
              <option value="Month">Mensal</option>
              <option value="Year">Anual</option>
            </select>
          </div>
          <div class="field">
            <label>Data de vencimento</label>
            <input v-model="form.dueDate" type="date" required />
            <span class="muted hint">Avança automaticamente ao pagar.</span>
          </div>
          <div class="field">
            <label>Reserva (opcional)</label>
            <select v-model="form.reserveId">
              <option value="">Nenhuma</option>
              <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
            </select>
          </div>
          <div class="kpi-row">
            <div class="kpi">
              <div class="label">Próximo vencimento</div>
              <div class="value value-sm">
                {{ cost.dueDate ? new Date(cost.dueDate).toLocaleDateString('pt-BR') : '-' }}
              </div>
            </div>
            <div class="kpi">
              <div class="label">Recorrência</div>
              <div class="value value-sm">{{ recurrenceLabel[cost.recurrence] || cost.recurrence }}</div>
            </div>
            <div class="kpi">
              <div class="label">Total pago</div>
              <div class="value value-sm">{{ formatMoney(totalPaid) }}</div>
            </div>
          </div>
          <div class="actions">
            <button class="btn" type="submit">Atualizar conta</button>
          </div>
        </form>

        <div class="side-stack">
          <div class="panel">
            <h2>Registrar pagamento</h2>
            <p v-if="cost.reserveName" class="muted pay-hint">
              Será debitado da reserva <strong>{{ cost.reserveName }}</strong>.
            </p>
            <p v-else class="muted pay-hint">
              Sem reserva vinculada — o pagamento será só registrado no histórico.
            </p>
            <form class="pay-form" @submit.prevent="pay">
              <div class="field">
                <label>Valor pago</label>
                <MoneyInput v-model="payForm.paidAmount" required />
              </div>
              <button class="btn" type="submit" :disabled="paying">
                {{ paying ? 'Pagando...' : 'Pagar' }}
              </button>
            </form>
          </div>

          <div class="panel">
            <h2>Histórico de pagamentos</h2>
            <DataTable
              :rows="payments"
              :columns="paymentColumns"
              row-key="id"
              :paginated="false"
              initial-sort-key="datePaid"
              empty-text="Nenhum pagamento registrado."
            >
              <template #cell-datePaid="{ row }">
                {{ new Date(row.datePaid).toLocaleString('pt-BR') }}
              </template>
              <template #cell-paidAmount="{ row }">{{ formatMoney(row.paidAmount) }}</template>
              <template #cell-actions="{ row }">
                <IconButton label="Excluir" icon="delete" variant="danger" @click.stop="removePayment(row.id)" />
              </template>
            </DataTable>
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
  grid-template-columns: minmax(280px, 360px) 1fr;
  gap: 1rem;
  align-items: start;
}

.side-stack {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.kpi-row {
  display: grid;
  gap: 0.75rem;
  margin: 0.5rem 0 1rem;
}

.value-sm {
  font-size: 1.1rem !important;
}

.hint {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.82rem;
}

.pay-hint {
  margin: 0 0 1rem;
}

.pay-form {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.75rem;
  align-items: end;
}

.pay-form .field {
  margin-bottom: 0;
}

.success {
  color: var(--success);
  background: rgba(74, 222, 128, 0.1);
  border: 1px solid rgba(74, 222, 128, 0.28);
  padding: 0.75rem 1rem;
  border-radius: var(--radius-sm);
}

@media (max-width: 1000px) {
  .detail-layout {
    grid-template-columns: 1fr;
  }

  .pay-form {
    grid-template-columns: 1fr;
  }
}
</style>
