import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import api from '@/api/client'
import type { LoginResponse, UserRole } from '@/types'

const STORAGE_KEY = 'desfudencify.auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const email = ref<string | null>(null)
  const fullName = ref<string | null>(null)
  const role = ref<UserRole | null>(null)
  const userId = ref<string | null>(null)

  function load() {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return
    const data = JSON.parse(raw) as LoginResponse
    token.value = data.token
    email.value = data.email
    fullName.value = data.fullName
    role.value = data.role
    userId.value = data.userId
  }

  function persist() {
    if (!token.value) {
      localStorage.removeItem(STORAGE_KEY)
      return
    }
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        token: token.value,
        email: email.value,
        fullName: fullName.value,
        role: role.value,
        userId: userId.value,
      }),
    )
  }

  async function login(loginEmail: string, password: string) {
    const { data } = await api.post<LoginResponse>('/auth/login', { email: loginEmail, password })
    token.value = data.token
    email.value = data.email
    fullName.value = data.fullName
    role.value = data.role
    userId.value = data.userId
    persist()
  }

  async function logout() {
    try {
      await api.post('/auth/logout')
    } catch {
      // ignore
    }
    token.value = null
    email.value = null
    fullName.value = null
    role.value = null
    userId.value = null
    persist()
  }

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => role.value === 'Admin')

  load()

  return { token, email, fullName, role, userId, isAuthenticated, isAdmin, login, logout, load }
})
