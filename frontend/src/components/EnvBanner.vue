<script setup lang="ts">
import { computed } from 'vue'

const isDevelopment = computed(
  () => import.meta.env.DEV || import.meta.env.VITE_APP_ENV === 'development',
)
</script>

<template>
  <div v-if="isDevelopment" class="env-banner" role="status">
    Ambiente de desenvolvimento
  </div>
</template>

<style scoped>
.env-banner {
  position: sticky;
  top: 0;
  z-index: 2000;
  width: 100%;
  padding: 0.4rem 1rem;
  text-align: center;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #1a1200;
  background: #fbbf24;
  border-bottom: 1px solid rgba(0, 0, 0, 0.12);
}
</style>
