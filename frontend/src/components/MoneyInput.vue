<script setup lang="ts">
import { ref, watch } from 'vue'
import { formatMoney, parseMoneyInput } from '@/utils/money'

const props = withDefaults(
  defineProps<{
    modelValue: number
    required?: boolean
    allowNegative?: boolean
    disabled?: boolean
    id?: string
  }>(),
  {
    required: false,
    allowNegative: false,
    disabled: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: number]
}>()

const display = ref(formatMoney(props.modelValue || 0))
const focused = ref(false)

watch(
  () => props.modelValue,
  (value) => {
    if (!focused.value) {
      display.value = formatMoney(value || 0)
    }
  },
)

function onFocus(event: FocusEvent) {
  focused.value = true
  const target = event.target
  if (target instanceof HTMLInputElement) {
    target.select()
  }
}

function onInput(event: Event) {
  const target = event.target
  if (!(target instanceof HTMLInputElement)) return

  const nextValue = parseMoneyInput(target.value, props.allowNegative)
  display.value = formatMoney(nextValue)
  emit('update:modelValue', nextValue)

  // Keep caret at end after remask
  requestAnimationFrame(() => {
    const len = target.value.length
    target.setSelectionRange(len, len)
  })
}

function onKeydown(event: KeyboardEvent) {
  if (!props.allowNegative) return
  if (event.key !== '-' && event.key !== '−') return

  event.preventDefault()
  const flipped = -1 * (props.modelValue || 0)
  emit('update:modelValue', flipped)
  display.value = formatMoney(flipped)
}

function onBlur() {
  focused.value = false
  display.value = formatMoney(props.modelValue || 0)
}
</script>

<template>
  <input
    :id="id"
    class="money-input"
    type="text"
    inputmode="decimal"
    autocomplete="off"
    :value="display"
    :required="required"
    :disabled="disabled"
    @focus="onFocus"
    @input="onInput"
    @keydown="onKeydown"
    @blur="onBlur"
  />
</template>

<style scoped>
.money-input {
  width: 100%;
  background: var(--bg-soft);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  color: var(--text);
  padding: 0.7rem 0.85rem;
  outline: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.money-input:focus {
  border-color: rgba(56, 189, 248, 0.55);
  box-shadow: 0 0 0 3px rgba(56, 189, 248, 0.15);
}

.money-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
