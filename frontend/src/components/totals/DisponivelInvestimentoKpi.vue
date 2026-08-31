<script setup lang="ts">
import { computed } from 'vue'
import KpiCard from './KpiCard.vue'

const props = withDefaults(
  defineProps<{
    disponivelParaInvestimento: number
    totalDisponivelReservas: number
    saldoLivre: number
    nowrap?: boolean
  }>(),
  {
    nowrap: false,
  },
)

const breakdown = computed(() => [
  { label: 'Montinhos', value: props.totalDisponivelReservas },
  { label: 'Saldo livre', value: props.saldoLivre },
])
</script>

<template>
  <KpiCard
    label="Disponível para investimento"
    :value="disponivelParaInvestimento"
    :breakdown="breakdown"
    :nowrap="nowrap"
  />
</template>
