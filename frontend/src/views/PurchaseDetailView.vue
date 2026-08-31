<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type Purchase, type PurchaseInstallment, type Reserve } from '@/types'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const FREE_SOURCE = '__free__'

const route = useRoute()
const router = useRouter()
const toast = useToastStore()
const purchaseId = computed(() => String(route.params.id))

const purchase = ref<Purchase | null>(null)
const reserves = ref<Reserve[]>([])
const loading = ref(true)
const payingId = ref<string | null>(null)

const form = reactive({
  name: '',
  productUrl: '',
  sourceId: '',
})

const installmentColumns: DataTableColumn<PurchaseInstallment>[] = [
  { key: 'installmentNumber', label: '#', sortValue: (row) => row.installmentNumber },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'dueDate', label: 'Vencimento', sortValue: (row) => new Date(row.dueDate) },
  { key: 'status', label: 'Status', sortValue: (row) => (row.paid ? 1 : 0) },
  { key: 'paidDate', label: 'Pago em', sortValue: (row) => (row.paidDate ? new Date(row.paidDate) : null) },
  { key: 'actions', label: '', sortable: false },
]

const installments = computed(() => purchase.value?.installments ?? [])
const totalAmount = computed(() => installments.value.reduce((sum, item) => sum + item.amount, 0))
const remaining = computed(() => installments.value.filter((item) => !item.paid))
const remainingAmount = computed(() => remaining.value.reduce((sum, item) => sum + item.amount, 0))
const sourceLabel = computed(() => {
  if (purchase.value?.debitSource === 'FreeBalance') return 'Saldo livre'
  return purchase.value?.reserveName || ''
})
const nextDue = computed(() => {
  const pending = remaining.value
    .slice()
    .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
  return pending[0] ?? null
})

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

function sourceIdFromPurchase(item: Purchase) {
  if (item.debitSource === 'FreeBalance') return FREE_SOURCE
  return item.reserveId || ''
}

function debitPayload(sourceId: string) {
  if (sourceId === FREE_SOURCE) {
    return { debitSource: 'FreeBalance', reserveId: null }
  }
  if (sourceId) {
    return { debitSource: 'Reserve', reserveId: sourceId }
  }
  return { debitSource: 'None', reserveId: null }
}

async function load() {
  loading.value = true
  try {
    const [purchaseRes, reservesRes] = await Promise.all([
      api.get<Purchase>(`/purchases/${purchaseId.value}`),
      api.get<Reserve[]>('/reserves'),
    ])
    purchase.value = purchaseRes.data
    reserves.value = reservesRes.data
    Object.assign(form, {
      name: purchaseRes.data.name,
      productUrl: purchaseRes.data.productUrl || '',
      sourceId: sourceIdFromPurchase(purchaseRes.data),
    })
  } catch (e) {
    toastError(e, 'Erro ao carregar parcelamento')
  } finally {
    loading.value = false
  }
}

async function savePurchase() {
  try {
    const { data } = await api.put<Purchase>(`/purchases/${purchaseId.value}`, {
      name: form.name,
      productUrl: form.productUrl || null,
      ...debitPayload(form.sourceId),
    })
    purchase.value = data
    toast.success('Parcelamento atualizado.')
  } catch (e) {
    toastError(e, 'Erro ao atualizar')
  }
}

async function pay(installmentId: string) {
  payingId.value = installmentId
  try {
    await api.post(`/purchases/${purchaseId.value}/installments/${installmentId}/pay`)
    await load()
    toast.success('Parcela paga.')
  } catch (e) {
    toastError(e, 'Erro ao pagar')
  } finally {
    payingId.value = null
  }
}

async function unpay(installmentId: string) {
  if (!confirm(sourceLabel.value
    ? `Reverter este pagamento? O lançamento em ${sourceLabel.value} será removido.`
    : 'Reverter este pagamento?')) return
  payingId.value = installmentId
  try {
    await api.post(`/purchases/${purchaseId.value}/installments/${installmentId}/unpay`)
    await load()
    toast.success('Pagamento revertido.')
  } catch (e) {
    toastError(e, 'Erro ao reverter')
  } finally {
    payingId.value = null
  }
}

onMounted(load)
watch(purchaseId, () => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Gerenciar</p>
        <h1>{{ purchase?.name || 'Parcelamento' }}</h1>
        <p class="muted">Dados da compra e controle das parcelas.</p>
      </div>
      <button class="btn secondary" type="button" @click="router.push({ name: 'purchases' })">Voltar</button>
    </div>

    <p v-if="loading" class="muted">Carregando...</p>

    <template v-else-if="purchase">
      <div class="detail-layout">
        <form class="panel" @submit.prevent="savePurchase">
          <h2>Dados</h2>
          <div class="field"><label>Nome</label><input v-model="form.name" required /></div>
          <div class="field"><label>URL do produto</label><input v-model="form.productUrl" /></div>
          <div class="field">
            <label>Origem do pagamento (opcional)</label>
            <select v-model="form.sourceId">
              <option value="">Nenhuma</option>
              <option :value="FREE_SOURCE">Saldo livre</option>
              <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
            </select>
            <span class="muted hint">Parcelas pagas debitam automaticamente esta origem.</span>
          </div>
          <a v-if="purchase.productUrl" class="muted product-link" :href="purchase.productUrl" target="_blank" rel="noreferrer">
            Abrir link do produto
          </a>
          <div class="kpi-row">
            <div class="kpi">
              <div class="label">Valor total</div>
              <div class="value value-sm">{{ formatMoney(totalAmount) }}</div>
            </div>
            <div class="kpi">
              <div class="label">Restante</div>
              <div class="value value-sm">{{ formatMoney(remainingAmount) }}</div>
            </div>
            <div class="kpi">
              <div class="label">Próximo vencimento</div>
              <div class="value value-sm">
                {{ nextDue ? new Date(nextDue.dueDate).toLocaleDateString('pt-BR') : '-' }}
              </div>
            </div>
            <div class="kpi">
              <div class="label">Parcelas</div>
              <div class="value value-sm">{{ remaining.length }} / {{ installments.length }}</div>
            </div>
          </div>
          <div class="actions">
            <button class="btn" type="submit">Atualizar parcelamento</button>
          </div>
        </form>

        <div class="panel">
          <h2>Parcelas</h2>
          <p v-if="sourceLabel" class="muted pay-hint">
            Será debitado de <strong>{{ sourceLabel }}</strong>.
          </p>
          <p v-else class="muted pay-hint">
            Sem origem vinculada — o pagamento será só registrado como pago.
          </p>
          <DataTable
            :rows="installments"
            :columns="installmentColumns"
            row-key="id"
            initial-sort-key="dueDate"
            initial-sort-dir="desc"
            empty-text="Nenhuma parcela encontrada."
          >
            <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
            <template #cell-dueDate="{ row }">{{ new Date(row.dueDate).toLocaleDateString('pt-BR') }}</template>
            <template #cell-status="{ row }">
              <span class="badge" :class="row.paid ? 'success' : ''">{{ row.paid ? 'Pago' : 'Pendente' }}</span>
            </template>
            <template #cell-paidDate="{ row }">
              {{ row.paidDate ? new Date(row.paidDate).toLocaleDateString('pt-BR') : '-' }}
            </template>
            <template #cell-actions="{ row }">
              <div class="actions">
                <IconButton
                  v-if="!row.paid"
                  label="Pagar"
                  icon="pay"
                  variant="primary"
                  :disabled="payingId === row.id"
                  @click.stop="pay(row.id)"
                />
                <IconButton
                  v-else
                  label="Reverter"
                  icon="delete"
                  variant="danger"
                  :disabled="payingId === row.id"
                  @click.stop="unpay(row.id)"
                />
              </div>
            </template>
          </DataTable>
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

.product-link {
  display: inline-block;
  margin: -0.35rem 0 1rem;
}

.hint {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.82rem;
}

.pay-hint {
  margin: 0 0 1rem;
}

.kpi-row {
  display: grid;
  gap: 0.75rem;
  margin: 0.5rem 0 1rem;
}

.value-sm {
  font-size: 1.1rem !important;
}

@media (max-width: 1000px) {
  .detail-layout {
    grid-template-columns: 1fr;
  }
}
</style>
