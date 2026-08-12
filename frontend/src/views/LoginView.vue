<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import { useToastStore } from '@/stores/toast'

const auth = useAuthStore()
const theme = useThemeStore()
const toast = useToastStore()
const router = useRouter()
const route = useRoute()
const email = ref('')
const password = ref('')
const loading = ref(false)

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function onSubmit() {
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (e) {
    toastError(e, 'Falha no login')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <button class="btn secondary theme-toggle" type="button" @click="theme.toggle()">
      {{ theme.label }}
    </button>
    <form class="panel login-card" @submit.prevent="onSubmit">
      <p class="eyebrow">DesfudenciFy</p>
      <h1>Entre na plataforma</h1>
      <p class="muted">Controle de gastos, reservas, investimentos e imóveis.</p>
      <div class="field">
        <label>Email</label>
        <input v-model="email" type="email" required autocomplete="username" />
      </div>
      <div class="field">
        <label>Senha</label>
        <input v-model="password" type="password" required autocomplete="current-password" />
      </div>
      <button class="btn" type="submit" :disabled="loading">
        {{ loading ? 'Entrando...' : 'Entrar' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  position: relative;
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1.5rem;
  background: var(--bg);
}

.theme-toggle {
  position: absolute;
  top: 1.25rem;
  right: 1.25rem;
}
.login-card {
  width: min(420px, 100%);
  animation: rise 0.5s ease;
}
.login-card .btn {
  width: 100%;
  margin-top: 0.35rem;
}
.eyebrow {
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  font-size: 0.75rem;
  color: var(--accent);
  font-weight: 700;
}
h1 { margin: 0.35rem 0 0.5rem; }
@keyframes rise {
  from { opacity: 0; transform: translateY(12px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
