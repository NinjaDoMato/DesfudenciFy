import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import api from '@/api/client'
import type { LoginResponse, UserRole } from '@/types'

const STORAGE_KEY = 'desfudencify.auth'

interface StoredAuth {
  token: string
  refreshToken: string
  userId: string
  email: string
  fullName: string
  role: UserRole
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const email = ref<string | null>(null)
  const fullName = ref<string | null>(null)
  const role = ref<UserRole | null>(null)
  const userId = ref<string | null>(null)

  function load() {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return
    const data = JSON.parse(raw) as StoredAuth
    token.value = data.token
    refreshToken.value = data.refreshToken ?? null
    email.value = data.email
    fullName.value = data.fullName
    role.value = data.role
    userId.value = data.userId
  }

  function persist() {
    if (
      !token.value ||
      !email.value ||
      !fullName.value ||
      !role.value ||
      !userId.value
    ) {
      localStorage.removeItem(STORAGE_KEY)
      return
    }
    const stored: StoredAuth = {
      token: token.value,
      refreshToken: refreshToken.value ?? '',
      email: email.value,
      fullName: fullName.value,
      role: role.value,
      userId: userId.value,
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored))
  }

  async function login(loginEmail: string, password: string) {
    const { data } = await api.post<LoginResponse>('/auth/login', { email: loginEmail, password })
    token.value = data.token
    refreshToken.value = data.refreshToken
    email.value = data.email
    fullName.value = data.fullName
    role.value = data.role
    userId.value = data.userId
    persist()
  }

  function applyTokens(newToken: string, newRefreshToken: string) {
    token.value = newToken
    refreshToken.value = newRefreshToken
    persist()
  }

  function clearAuth() {
    token.value = null
    refreshToken.value = null
    email.value = null
    fullName.value = null
    role.value = null
    userId.value = null
    persist()
  }

  async function logout() {
    try {
      await api.post('/auth/logout')
    } catch {
      // ignore
    }
    clearAuth()
  }

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => role.value === 'Admin')

  load()

  return {
    token,
    refreshToken,
    email,
    fullName,
    role,
    userId,
    isAuthenticated,
    isAdmin,
    login,
    logout,
    load,
    applyTokens,
    clearAuth,
  }
})
