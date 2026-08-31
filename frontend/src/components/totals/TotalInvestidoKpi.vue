<script setup lang="ts">
import { computed } from 'vue'
import type { InvestidoTotals } from '@/utils/totals'
import KpiCard from './KpiCard.vue'

const props = withDefaults(
  defineProps<{
    data: InvestidoTotals
    label?: string
    nowrap?: boolean
  }>(),
  {
    label: 'Total investido',
  },
)

const breakdown = computed(() => [
  { label: 'Saldo livre', value: props.data.investedFromFree },
  { label: 'Montinhos', value: props.data.investedFromReserves },
  { label: 'Lucro retido', value: props.data.lucroRetido, polarity: true },
])
</script>

<template>
  <KpiCard
    :label="label"
    :value="data.totalInvestido"
    :breakdown="breakdown"
    :nowrap="nowrap"
  />
</template>
