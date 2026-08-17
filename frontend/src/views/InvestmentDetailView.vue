<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import { formatMoney, type Investment, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import LiquidationModal from '@/components/LiquidationModal.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const FREE_SOURCE = '__free__'

const route = useRoute()
const router = useRouter()
const toast = useToastStore()
const investmentId = computed(() => String(route.params.id))

const investment = ref<Investment | null>(null)
const reserves = ref<Reserve[]>([])
const loading = ref(true)
const currentAmount = ref(0)
const showLiquidation = ref(false)

type SourceRow = { sourceKey: string; reserveId: string | null; amount: number }

const sourceColumns: DataTableColumn<SourceRow>[] = [
  { key: 'sourceKey', label: 'Origem', sortValue: (row) => sourceLabel(row.reserveId) },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
]

const sourceRows = computed<SourceRow[]>(() =>
  (investment.value?.sourceReserves ?? []).map((s) => ({
    sourceKey: s.reserveId ?? FREE_SOURCE,
    reserveId: s.reserveId ?? null,
    amount: s.amount,
  })),
)

const isActive = computed(() => investment.value?.status === 'Active')

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

function sourceLabel(reserveId: string | null) {
  if (!reserveId) return 'Saldo livre'
  return reserves.value.find((r) => r.id === reserveId)?.name || 'Reserva'
}

async function load() {
  loading.value = true
  try {
    const [invRes, reservesRes] = await Promise.all([
      api.get<Investment>(`/investments/${investmentId.value}`),
      api.get<Reserve[]>('/reserves'),
    ])
    investment.value = invRes.data
    reserves.value = reservesRes.data
    currentAmount.value = invRes.data.currentAmount
  } catch (e) {
    toastError(e, 'Erro ao carregar investimento')
  } finally {
    loading.value = false
  }
}

async function updateAmount() {
  if (!investment.value) return
  try {
    await api.put(`/investments/${investment.value.id}/current-amount`, {
      currentAmount: Number(currentAmount.value),
    })
    await load()
    toast.success('Valor atual atualizado.')
  } catch (e) {
    toastError(e, 'Erro ao atualizar valor')
  }
}

function openLiquidation() {
  showLiquidation.value = true
}

function onLiquidated() {
  showLiquidation.value = false
  toast.success('Investimento liquidado.')
  void load()
}

onMounted(load)
watch(investmentId, () => {
  void load()
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Gerenciar</p>
        <h1>{{ investment?.name || 'Investimento' }}</h1>
        <p class="muted">Detalhes, valor atual e liquidação.</p>
      </div>
      <button class="btn secondary" type="button" @click="router.push({ name: 'investments' })">Voltar</button>
    </div>

    <p v-if="loading" class="muted">Carregando...</p>

    <template v-else-if="investment">
      <div class="detail-layout">
        <div class="panel">
          <h2>Dados</h2>
          <div class="info-grid">
            <div>
              <div class="muted label">Tipo</div>
              <div>{{ investment.investmentTypeName }}</div>
            </div>
            <div>
              <div class="muted label">Conta</div>
              <div>{{ investment.bankAccountName }}</div>
            </div>
            <div>
              <div class="muted label">Rentabilidade</div>
              <div>{{ investment.rentability || '-' }}</div>
            </div>
            <div>
              <div class="muted label">Status</div>
              <div><span class="badge">{{ investment.status }}</span></div>
            </div>
            <div>
              <div class="muted label">Início</div>
              <div>{{ new Date(investment.startDate).toLocaleDateString('pt-BR') }}</div>
            </div>
            <div>
              <div class="muted label">Fim</div>
              <div>
                {{ investment.endDate ? new Date(investment.endDate).toLocaleDateString('pt-BR') : '-' }}
              </div>
            </div>
          </div>
          <div class="kpi-row">
            <div class="kpi">
              <div class="label">Valor inicial</div>
              <div class="value">{{ formatMoney(investment.startAmount) }}</div>
            </div>
            <div class="kpi">
              <div class="label">Valor atual</div>
              <div class="value">{{ formatMoney(investment.currentAmount) }}</div>
            </div>
          </div>
        </div>

        <div class="side-stack">
          <div class="panel">
            <h2>Origens do capital</h2>
            <DataTable
              :rows="sourceRows"
              :columns="sourceColumns"
              row-key="sourceKey"
              :paginated="false"
              empty-text="Nenhuma origem."
            >
              <template #cell-sourceKey="{ row }">{{ sourceLabel(row.reserveId) }}</template>
              <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
            </DataTable>
          </div>

          <div v-if="isActive" class="panel">
            <h2>Atualizar valor atual</h2>
            <form class="amount-form" @submit.prevent="updateAmount">
              <div class="field">
                <label>Valor atual</label>
                <MoneyInput v-model="currentAmount" required />
              </div>
              <button class="btn" type="submit">Salvar valor</button>
            </form>
          </div>

          <div v-if="isActive" class="panel">
            <h2>Liquidação</h2>
            <p class="muted">
              O lucro (valor atual − inicial) é rateado proporcionalmente entre as origens.
            </p>
            <button class="btn" type="button" @click="openLiquidation">
              Liquidar investimento
            </button>
          </div>
        </div>
      </div>
    </template>

    <LiquidationModal
      v-if="showLiquidation && investment"
      :investment="investment"
      :reserves="reserves"
      @close="showLiquidation = false"
      @liquidated="onLiquidated"
    />
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

.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.85rem 1rem;
  margin-bottom: 1rem;
}

.info-grid .label {
  font-size: 0.8rem;
  margin-bottom: 0.2rem;
}

.kpi-row {
  display: grid;
  gap: 0.75rem;
}

.amount-form {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.75rem;
  align-items: end;
}

.amount-form .field {
  margin-bottom: 0;
}

@media (max-width: 1000px) {
  .detail-layout {
    grid-template-columns: 1fr;
  }

  .amount-form {
    grid-template-columns: 1fr;
  }
}
</style>
