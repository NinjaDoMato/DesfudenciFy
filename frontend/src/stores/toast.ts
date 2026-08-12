import { ref } from 'vue'
import { defineStore } from 'pinia'

export type ToastTone = 'success' | 'error' | 'info'

export interface ToastItem {
  id: number
  message: string
  tone: ToastTone
}

const DEFAULT_DURATION_MS = 4200

export const useToastStore = defineStore('toast', () => {
  const items = ref<ToastItem[]>([])
  let nextId = 1
  const timers = new Map<number, number>()

  function dismiss(id: number) {
    const timer = timers.get(id)
    if (timer !== undefined) {
      window.clearTimeout(timer)
      timers.delete(id)
    }
    items.value = items.value.filter((item) => item.id !== id)
  }

  function push(message: string, tone: ToastTone = 'info', durationMs = DEFAULT_DURATION_MS) {
    const trimmed = message.trim()
    if (!trimmed) return

    const id = nextId++
    items.value.push({ id, message: trimmed, tone })

    if (durationMs > 0) {
      timers.set(
        id,
        window.setTimeout(() => {
          dismiss(id)
        }, durationMs),
      )
    }
  }

  function success(message: string, durationMs = DEFAULT_DURATION_MS) {
    push(message, 'success', durationMs)
  }

  function error(message: string, durationMs = DEFAULT_DURATION_MS) {
    push(message, 'error', durationMs)
  }

  function info(message: string, durationMs = DEFAULT_DURATION_MS) {
    push(message, 'info', durationMs)
  }

  return { items, push, success, error, info, dismiss }
})
