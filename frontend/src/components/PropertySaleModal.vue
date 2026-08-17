<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import api from '@/api/client'
import { formatMoney, moneyPolarity, type EntryDestination, type Property, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import { useToastStore } from '@/stores/toast'
import { computePropertySalePreview } from '@/utils/propertySale'

const props = defineProps<{
  property: Property
  reserves: readonly Reserve[]
}>()

const emit = defineEmits<{
  close: []
  sold: []
}>()

const toast = useToastStore()
const submitting = ref(false)
const form = reactive({
  saleAmount: props.property.appraisedValue,
  destination: 'FreeBalance' as EntryDestination,
  reserveId: '',
})

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

const preview = computed(() =>
  computePropertySalePreview({
    saleAmount: Number(form.saleAmount) || 0,
    propertyCost: props.property.propertyCost,
    totalRentPaid: props.property.totalRentPaid,
    remainingBalance: props.property.remainingBalance,
    isRented: props.property.isRented,
  }),
)

const destinationLabel = computed(() => {
  if (form.destination !== 'Reserve') return 'Saldo livre'
  return props.reserves.find((reserve) => reserve.id === form.reserveId)?.name || 'Reserva'
})

const canConfirm = computed(() => {
  if (preview.value.saleAmount <= 0) return false
  if (form.destination === 'Reserve' && !form.reserveId) return false
  return true
})

function close() {
  if (submitting.value) return
  emit('close')
}

async function confirm() {
  if (!canConfirm.value) return
  submitting.value = true
  try {
    await api.post(`/properties/${props.property.id}/sell`, {
      saleAmount: preview.value.saleAmount,
      destination: form.destination,
      reserveId: form.destination === 'Reserve' ? form.reserveId : null,
    })
    emit('sold')
  } catch (error) {
    toastError(error, 'Erro ao vender imóvel')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="modal-backdrop" @click.self="close">
    <div class="modal">
      <h2>Vender imóvel</h2>
      <p class="muted">{{ property.name }}</p>

      <div class="field">
        <label>Valor da venda</label>
        <MoneyInput v-model="form.saleAmount" required />
      </div>
      <div class="field">
        <label>Destino do lucro</label>
        <select v-model="form.destination">
          <option value="FreeBalance">Saldo livre</option>
          <option value="Reserve">Reserva</option>
        </select>
      </div>
      <div v-if="form.destination === 'Reserve'" class="field">
        <label>Reserva</label>
        <select v-model="form.reserveId" required>
          <option disabled value="">Selecione</option>
          <option v-for="reserve in reserves" :key="reserve.id" :value="reserve.id">{{ reserve.name }}</option>
        </select>
      </div>

      <div v-if="preview.saleAmount > 0" class="summary-grid">
        <div class="kpi">
          <div class="label">Lucro estimado</div>
          <div class="value" :class="moneyPolarity(preview.profit)">{{ formatMoney(preview.profit) }}</div>
        </div>
        <div class="kpi">
          <div class="label">Valor a pagar após a venda</div>
          <div class="value">{{ formatMoney(preview.remainingBalanceAfter) }}</div>
        </div>
        <div class="kpi">
          <div class="label">Destino do lucro</div>
          <div class="value value-sm">{{ destinationLabel }}</div>
        </div>
      </div>
      <p v-if="preview.saleAmount > 0 && property.isRented" class="muted note">
        O imóvel está alugado: o aluguel será encerrado e a entrada de aluguel será desativada, como se fosse desmarcada manualmente.
      </p>

      <div class="actions footer">
        <button class="btn" type="button" :disabled="!canConfirm || submitting" @click="confirm">
          {{ submitting ? 'Vendendo...' : 'Confirmar venda' }}
        </button>
        <button class="btn secondary" type="button" :disabled="submitting" @click="close">Cancelar</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 0.75rem;
  margin: 1rem 0 0;
}

.value-sm {
  font-size: 1.05rem !important;
}

.note {
  margin-top: 0.85rem;
}

.footer {
  margin-top: 1rem;
}
</style>
