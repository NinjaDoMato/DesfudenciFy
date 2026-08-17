<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    icon: 'details' | 'edit' | 'amount' | 'liquidate' | 'delete' | 'pay' | 'sell'
    variant?: 'primary' | 'secondary' | 'danger'
    disabled?: boolean
  }>(),
  {
    variant: 'secondary',
    disabled: false,
  },
)
</script>

<template>
  <button
    class="icon-btn"
    :class="variant"
    type="button"
    :title="label"
    :aria-label="label"
    :disabled="disabled"
  >
    <svg
      class="icon"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.8"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <template v-if="icon === 'details'">
        <path d="M2.5 12s3.5-6.5 9.5-6.5S21.5 12 21.5 12s-3.5 6.5-9.5 6.5S2.5 12 2.5 12Z" />
        <circle cx="12" cy="12" r="2.5" />
      </template>
      <template v-else-if="icon === 'edit'">
        <path d="M12 20h9" />
        <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5Z" />
      </template>
      <template v-else-if="icon === 'amount'">
        <path d="M3 12a9 9 0 0 1 15.5-6.4" />
        <path d="M21 3v6h-6" />
        <path d="M21 12a9 9 0 0 1-15.5 6.4" />
        <path d="M3 21v-6h6" />
        <path d="M12 8v8" />
        <path d="M9.8 10.2c.5-.9 1.4-1.4 2.2-1.4s1.8.6 1.8 1.6c0 2-3.6 1.3-3.6 3.2 0 .9.8 1.4 1.8 1.4s1.6-.4 2.1-1.1" />
      </template>
      <template v-else-if="icon === 'liquidate'">
        <circle cx="12" cy="12" r="9" />
        <path d="m8.5 12.5 2.3 2.3 4.7-5" />
      </template>
      <template v-else-if="icon === 'delete'">
        <path d="M4 7h16" />
        <path d="M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
        <path d="M7 7l1 12a2 2 0 0 0 2 2h4a2 2 0 0 0 2-2l1-12" />
        <path d="M10 11v6" />
        <path d="M14 11v6" />
      </template>
      <template v-else-if="icon === 'sell'">
        <path d="M3 7h13v10H3z" />
        <path d="M16 10h4l2 2-2 2h-4" />
        <path d="M7 12h5" />
      </template>
      <template v-else-if="icon === 'pay'">
        <rect x="3" y="6" width="18" height="12" rx="2" />
        <path d="M3 10h18" />
        <path d="M7 15h3" />
      </template>
    </svg>
  </button>
</template>

<style scoped>
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  padding: 0;
  border: none;
  border-radius: 0.65rem;
  cursor: pointer;
  background: var(--accent);
  color: var(--btn-on-accent);
  font-weight: 600;
  transition: background 0.15s ease;
  box-shadow: none;
  flex-shrink: 0;
}

.icon-btn:hover {
  background: var(--accent-strong);
}

.icon-btn.secondary {
  background: transparent;
  color: var(--text);
  border: 1px solid var(--border);
}

.icon-btn.secondary:hover {
  background: var(--bg-soft);
}

.icon-btn.danger {
  background: var(--danger);
  color: #ffffff;
}

.icon-btn.danger:hover {
  background: #f43f5e;
}

.icon-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.icon {
  width: 1.05rem;
  height: 1.05rem;
}
</style>
