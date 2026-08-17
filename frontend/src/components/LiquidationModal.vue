<script setup lang="ts">
import { computed, ref } from 'vue'
import api from '@/api/client'
import { formatMoney, type Investment, type Reserve } from '@/types'
import DataTable from '@/components/DataTable.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'
import {
  buildLiquidationSummary,
  type LiquidationDistribution,
} from '@/utils/liquidation'

const props = defineProps<{
  investment: Investment
  reserves: readonly Reserve[]
}>()

const emit = defineEmits<{
  close: []
  liquidated: []
}>()

const toast = useToastStore()
const submitting = ref(false)

function sourceLabel(reserveId: string | null | undefined) {
  if (!reserveId) return 'Saldo livre'
  return props.reserves.find((reserve) => reserve.id === reserveId)?.name || reserveId
}

function toastError(error: unknown, fallback: string) {
  if (
    typeof error === 'object'
    && error !== null
    && 'message' in error
    && typeof error.message === 'string'
    && error.message.length > 0
  ) {
    toast.error(error.message)
    return
  }
  toast.error(fallback)
}

const summary = computed(() => buildLiquidationSummary(props.investment))

const columns: DataTableColumn<LiquidationDistribution>[] = [
  { key: 'reserveId', label: 'Origem', sortValue: (row) => sourceLabel(row.reserveId) },
  { key: 'investedAmount', label: 'Investido', sortValue: (row) => row.investedAmount },
  { key: 'proportion', label: 'Proporção', sortValue: (row) => row.proportion },
  { key: 'profitShare', label: 'Lucro a lançar', sortValue: (row) => row.profitShare },
]

function close() {
  if (submitting.value) return
  emit('close')
}

async function confirm() {
  submitting.value = true
  try {
    await api.post(`/investments/${props.investment.id}/liquidate`)
    emit('liquidated')
  } catch (error) {
    toastError(error, 'Erro ao liquidar')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="modal-backdrop" @click.self="close">
    <div class="modal wide">
      <h2>Resumo da liquidação</h2>
      <p class="muted">
        O principal volta ao disponível das origens; o lucro é lançado proporcionalmente (saldo livre ou reserva).
      </p>

      <div class="summary-grid">
        <div class="kpi">
          <div class="label">Investimento</div>
          <div class="value value-sm">{{ investment.name }}</div>
        </div>
        <div class="kpi">
          <div class="label">Valor investido original</div>
          <div class="value">{{ formatMoney(summary.invested) }}</div>
        </div>
        <div class="kpi">
          <div class="label">Valor final</div>
          <div class="value">{{ formatMoney(summary.finalValue) }}</div>
        </div>
        <div class="kpi">
          <div class="label">Lucro</div>
          <div
            class="value"
            :class="{ profit: summary.profit > 0, zero: summary.profit <= 0 }"
          >
            {{ formatMoney(summary.profit) }}
          </div>
        </div>
      </div>

      <h3 class="section-title">Lançamentos de lucro por origem</h3>
      <DataTable
        :rows="summary.distributions"
        :columns="columns"
        row-key="sourceKey"
        :page-size="5"
      >
        <template #cell-reserveId="{ row }">{{ sourceLabel(row.reserveId) }}</template>
        <template #cell-investedAmount="{ row }">{{ formatMoney(row.investedAmount) }}</template>
        <template #cell-proportion="{ row }">{{ (row.proportion * 100).toFixed(2) }}%</template>
        <template #cell-profitShare="{ row }">
          <span :style="{ color: row.profitShare > 0 ? 'var(--success)' : 'var(--muted)' }">
            {{ formatMoney(row.profitShare) }}
          </span>
        </template>
      </DataTable>

      <p v-if="summary.profit <= 0" class="muted note">
        Sem lucro positivo: nenhum lançamento de rendimento será criado. O principal investido volta ao saldo disponível ao remover os vínculos.
      </p>
      <p v-else class="muted note">
        Total de lucro a lançar: <strong>{{ formatMoney(summary.profit) }}</strong>
      </p>

      <div class="actions footer">
        <button class="btn" type="button" :disabled="submitting" @click="confirm">
          {{ submitting ? 'Liquidando...' : 'Confirmar liquidação' }}
        </button>
        <button class="btn secondary" type="button" :disabled="submitting" @click="close">
          Cancelar
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.75rem;
  margin: 1rem 0;
}

.value-sm {
  font-size: 1.05rem !important;
}

.profit {
  color: var(--success);
}

.zero {
  color: var(--muted);
}

.section-title {
  margin: 0.5rem 0 0.75rem;
  font-size: 1rem;
}

.note {
  margin-top: 0.85rem;
}

.footer {
  margin-top: 1rem;
}
</style>
