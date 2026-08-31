<script setup lang="ts">
import { computed } from 'vue'
import { formatMoney, moneyPolarity } from '@/types'

export interface KpiBreakdownRow {
  label: string
  value: number
  polarity?: boolean
  polarityInvert?: boolean
  accent?: boolean
  show?: boolean
}

const props = withDefaults(
  defineProps<{
    label: string
    value: number
    breakdown: KpiBreakdownRow[]
    valuePolarity?: boolean
    nowrap?: boolean
  }>(),
  {
    valuePolarity: false,
    nowrap: false,
  },
)

const visibleBreakdown = computed(() => props.breakdown.filter((row) => row.show !== false))

function rowPolarity(row: KpiBreakdownRow) {
  if (!row.polarity) return undefined
  const signed = row.polarityInvert ? -row.value : row.value
  return moneyPolarity(signed)
}
</script>

<template>
  <div class="kpi" :class="{ 'kpi-nowrap': nowrap }">
    <div class="label">{{ label }}</div>
    <div class="kpi-body">
      <div class="value" :class="valuePolarity ? moneyPolarity(value) : undefined">
        {{ formatMoney(value) }}
      </div>
      <div class="kpi-break">
        <div v-for="row in visibleBreakdown" :key="row.label" class="kpi-break-row">
          <span>{{ row.label }}</span>
          <strong :class="[row.accent ? 'accent' : rowPolarity(row)]">
            {{ formatMoney(row.value) }}
          </strong>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.kpi-nowrap .kpi-body {
  flex-wrap: nowrap;
}

.accent {
  color: var(--accent);
}
</style>
