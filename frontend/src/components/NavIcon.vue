<script setup lang="ts">
defineProps<{
  name:
    | 'dashboard'
    | 'capital'
    | 'patrimonio'
    | 'budget'
    | 'admin'
    | 'settings'
    | 'reserves'
    | 'investments'
    | 'properties'
    | 'vehicles'
    | 'entries'
    | 'income'
    | 'fixed-costs'
    | 'purchases'
    | 'users'
    | 'investment-types'
    | 'cost-types'
    | 'bank-accounts'
    | 'logout'
}>()
</script>

<template>
  <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <template v-if="name === 'dashboard'">
      <rect x="3" y="3" width="7" height="9" rx="1.5" />
      <rect x="14" y="3" width="7" height="5" rx="1.5" />
      <rect x="14" y="12" width="7" height="9" rx="1.5" />
      <rect x="3" y="16" width="7" height="5" rx="1.5" />
    </template>
    <template v-else-if="name === 'capital'">
      <path d="M12 3v18" />
      <path d="M7 7h7a3 3 0 0 1 0 6H9a3 3 0 0 0 0 6h8" />
    </template>
    <template v-else-if="name === 'patrimonio'">
      <path d="M4 19V8l8-5 8 5v11" />
      <path d="M9 19v-6h6v6" />
      <path d="M4 19h16" />
    </template>
    <template v-else-if="name === 'budget'">
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <path d="M3 10h18" />
      <path d="M8 15h3" />
    </template>
    <template v-else-if="name === 'admin'">
      <path d="M12 3 4.5 6.5v4.2c0 4.7 3.2 9 7.5 10.3 4.3-1.3 7.5-5.6 7.5-10.3V6.5L12 3Z" />
      <path d="M9.5 12.2 11.2 14l3.5-3.8" />
    </template>
    <template v-else-if="name === 'settings'">
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V20a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H4a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H10a1.7 1.7 0 0 0 1-1.5V4a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V10c.2.7.8 1.2 1.5 1.2H20a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1Z" />
    </template>
    <template v-else-if="name === 'reserves'">
      <path d="M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Z" />
      <path d="M12 12v9" />
      <path d="M12 12 4.2 7.6" />
      <path d="M12 12 19.8 7.6" />
    </template>
    <template v-else-if="name === 'investments'">
      <path d="M4 19V5" />
      <path d="M4 19h16" />
      <path d="M8 15v-4" />
      <path d="M12 15V8" />
      <path d="M16 15v-6" />
      <path d="M8 7l4-3 4 3" />
    </template>
    <template v-else-if="name === 'properties'">
      <path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6h-4v6H5a1 1 0 0 1-1-1v-9.5Z" />
    </template>
    <template v-else-if="name === 'vehicles'">
      <path d="M5 16h14v2a1 1 0 0 1-1 1h-1a1 1 0 0 1-1-1v-1H8v1a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1v-2Z" />
      <path d="M5 16 6.5 10.5A2 2 0 0 1 8.4 9h7.2a2 2 0 0 1 1.9 1.5L19 16" />
      <circle cx="7.5" cy="16" r="1.2" />
      <circle cx="16.5" cy="16" r="1.2" />
      <path d="M9 12h6" />
    </template>
    <template v-else-if="name === 'entries'">
      <path d="M8 6h11a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H8" />
      <path d="M8 6a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2" />
      <path d="M11 10h6" />
      <path d="M11 14h6" />
      <path d="M5 10h.01" />
      <path d="M5 14h.01" />
    </template>
    <template v-else-if="name === 'income'">
      <circle cx="12" cy="12" r="8" />
      <path d="M12 8v8" />
      <path d="M9.5 10.5c.6-1 1.6-1.5 2.5-1.5s2 .7 2 1.8c0 2.2-4 1.4-4 3.6 0 1 .9 1.6 2 1.6s1.8-.4 2.4-1.2" />
    </template>
    <template v-else-if="name === 'fixed-costs'">
      <path d="M9 6V4h6v2" />
      <rect x="5" y="6" width="14" height="14" rx="2" />
      <path d="M9 12h6" />
      <path d="M9 16h4" />
    </template>
    <template v-else-if="name === 'purchases'">
      <rect x="4" y="5" width="16" height="14" rx="2" />
      <path d="M8 5V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v1" />
      <path d="M8 11h8" />
      <path d="M8 15h5" />
    </template>
    <template v-else-if="name === 'users'">
      <circle cx="9" cy="8" r="3" />
      <path d="M3.5 19a5.5 5.5 0 0 1 11 0" />
      <circle cx="17" cy="9" r="2.5" />
      <path d="M15.5 19a4.5 4.5 0 0 1 5-4.2" />
    </template>
    <template v-else-if="name === 'investment-types'">
      <path d="M12 3 4 8v8l8 5 8-5V8l-8-5Z" />
      <path d="M12 12 4 8" />
      <path d="M12 12v9" />
      <path d="M12 12 20 8" />
    </template>
    <template v-else-if="name === 'cost-types'">
      <path d="M12 3 4.5 7v5.8c0 4.2 3 8.1 7.5 9.2 4.5-1.1 7.5-5 7.5-9.2V7L12 3Z" />
      <path d="M9.5 12h5" />
      <path d="M9.5 15h3.5" />
    </template>
    <template v-else-if="name === 'bank-accounts'">
      <path d="M3 10.5 12 4l9 6.5" />
      <path d="M5 10.5V19h14v-8.5" />
      <path d="M9 19v-4h6v4" />
      <path d="M3 19h18" />
    </template>
    <template v-else-if="name === 'logout'">
      <path d="M10 7V5a2 2 0 0 1 2-2h7a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-7a2 2 0 0 1-2-2v-2" />
      <path d="M15 12H3" />
      <path d="M6 9 3 12l3 3" />
    </template>
  </svg>
</template>

<style scoped>
.nav-icon {
  width: 1.1rem;
  height: 1.1rem;
  flex-shrink: 0;
}
</style>
