<script setup lang="ts">
import { computed } from 'vue'
import KpiCard from './KpiCard.vue'

const props = withDefaults(
  defineProps<{
    monthlyBalance: number
    totalIncome: number
    totalOperationalCosts: number
    totalInvestmentGoals: number
    nowrap?: boolean
  }>(),
  {
    nowrap: false,
  },
)

const breakdown = computed(() => [
  { label: 'Total de Entradas', value: props.totalIncome, polarity: true },
  {
    label: 'Total de Custos',
    value: props.totalOperationalCosts,
    polarity: true,
    polarityInvert: true,
  },
  { label: 'Metas de Investimento', value: props.totalInvestmentGoals, accent: true },
])
</script>

<template>
  <KpiCard
    label="Saldo mensal"
    :value="monthlyBalance"
    :breakdown="breakdown"
    value-polarity
    :nowrap="nowrap"
  />
</template>
