<script setup lang="ts">
import { computed } from 'vue'
import type { PatrimonioTotals } from '@/utils/totals'
import KpiCard from './KpiCard.vue'

const props = defineProps<{
  data: PatrimonioTotals
  nowrap?: boolean
}>()

const breakdown = computed(() => [
  { label: 'Montinhos', value: props.data.somatorioReservas },
  { label: 'Saldo livre', value: props.data.saldoLivre },
  {
    label: 'Imóveis',
    value: props.data.propertyAppraised,
    show: props.data.propertyAppraised !== 0,
  },
])
</script>

<template>
  <KpiCard
    label="Patrimônio acumulado"
    :value="data.patrimonio"
    :breakdown="breakdown"
    :nowrap="nowrap"
  />
</template>
