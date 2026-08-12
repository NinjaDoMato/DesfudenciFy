<script setup lang="ts">
import { RouterView } from 'vue-router'
import EnvBanner from '@/components/EnvBanner.vue'
import ToastHost from '@/components/ToastHost.vue'
</script>

<template>
  <EnvBanner />
  <RouterView />
  <ToastHost />
</template>
