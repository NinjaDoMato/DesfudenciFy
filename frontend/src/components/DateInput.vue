<script setup lang="ts">
import { ref, watch } from 'vue'
import {
  dateInputValueToDisplay,
  displayToDateInputValue,
  maskDateInput,
  toDateInputValue,
} from '@/utils/date'

const props = withDefaults(
  defineProps<{
    modelValue: string
    required?: boolean
    disabled?: boolean
    id?: string
  }>(),
  {
    required: false,
    disabled: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const display = ref(dateInputValueToDisplay(toDateInputValue(props.modelValue)))
const focused = ref(false)

watch(
  () => props.modelValue,
  (value) => {
    if (!focused.value) {
      display.value = dateInputValueToDisplay(toDateInputValue(value))
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

  const masked = maskDateInput(target.value)
  display.value = masked
  target.value = masked

  const parsed = displayToDateInputValue(masked)
  if (parsed) {
    emit('update:modelValue', parsed)
  } else if (!masked) {
    emit('update:modelValue', '')
  }
}

function onBlur() {
  focused.value = false
  const parsed = displayToDateInputValue(display.value)
  if (parsed) {
    display.value = dateInputValueToDisplay(parsed)
    emit('update:modelValue', parsed)
    return
  }

  display.value = dateInputValueToDisplay(toDateInputValue(props.modelValue))
}
</script>

<template>
  <input
    :id="id"
    class="date-input"
    type="text"
    inputmode="numeric"
    autocomplete="off"
    placeholder="dd/mm/aaaa"
    :value="display"
    :required="required"
    :disabled="disabled"
    @focus="onFocus"
    @input="onInput"
    @blur="onBlur"
  />
</template>

<style scoped>
.date-input {
  width: 100%;
  background: var(--bg-soft);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  color: var(--text);
  padding: 0.7rem 0.85rem;
  outline: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.date-input:focus {
  border-color: rgba(56, 189, 248, 0.55);
  box-shadow: 0 0 0 3px rgba(56, 189, 248, 0.15);
}

.date-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
