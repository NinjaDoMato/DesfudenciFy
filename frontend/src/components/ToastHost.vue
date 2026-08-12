<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
const { items } = storeToRefs(toast)
</script>

<template>
  <div class="toast-host" aria-live="polite" aria-relevant="additions">
    <TransitionGroup name="toast">
      <div
        v-for="item in items"
        :key="item.id"
        class="toast"
        :class="`toast-${item.tone}`"
        role="status"
      >
        <p class="toast-message">{{ item.message }}</p>
        <button
          class="toast-dismiss"
          type="button"
          aria-label="Fechar notificação"
          @click="toast.dismiss(item.id)"
        >
          ×
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-host {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 2000;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  width: min(22rem, calc(100vw - 2rem));
  pointer-events: none;
}

.toast {
  pointer-events: auto;
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border);
  background: var(--bg-elevated);
  box-shadow: var(--shadow);
  color: var(--text);
}

.toast-success {
  border-color: rgba(74, 222, 128, 0.35);
  background: color-mix(in srgb, var(--bg-elevated) 88%, var(--success));
}

.toast-error {
  border-color: rgba(251, 113, 133, 0.35);
  background: color-mix(in srgb, var(--bg-elevated) 88%, var(--danger));
}

.toast-info {
  border-color: color-mix(in srgb, var(--accent) 40%, var(--border));
  background: color-mix(in srgb, var(--bg-elevated) 88%, var(--accent));
}

.toast-message {
  margin: 0;
  flex: 1;
  font-size: 0.92rem;
  line-height: 1.4;
}

.toast-dismiss {
  border: 0;
  background: transparent;
  color: var(--muted);
  cursor: pointer;
  font-size: 1.15rem;
  line-height: 1;
  padding: 0;
  margin-top: -0.1rem;
}

.toast-dismiss:hover {
  color: var(--text);
}

.toast-enter-active,
.toast-leave-active {
  transition: opacity 0.22s ease, transform 0.22s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(-0.5rem);
}

.toast-move {
  transition: transform 0.22s ease;
}

@media (max-width: 640px) {
  .toast-host {
    top: auto;
    bottom: 1rem;
    right: 1rem;
    left: 1rem;
    width: auto;
  }

  .toast-enter-from,
  .toast-leave-to {
    transform: translateY(0.5rem);
  }
}
</style>
