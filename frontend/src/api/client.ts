import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
})

const STORAGE_KEY = 'desfudencify.auth'

interface StoredAuth {
  token?: string
  refreshToken?: string
}

function readStoredAuth(): StoredAuth {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return {}
  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    return {}
  }
}

api.interceptors.request.use((config) => {
  const auth = readStoredAuth()
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

interface RefreshResponse {
  token: string
  refreshToken: string
}

let refreshPromise: Promise<void> | null = null

async function doRefresh(): Promise<void> {
  const auth = readStoredAuth()
  if (!auth.refreshToken) {
    throw new Error('Sem refresh token')
  }

  const response = await axios.post<RefreshResponse>(
    `${api.defaults.baseURL}/auth/refresh`,
    { refreshToken: auth.refreshToken },
  )

  // Update stored auth without going through the store to avoid circular import
  const raw = localStorage.getItem(STORAGE_KEY)
  const stored = raw ? (JSON.parse(raw) as Record<string, unknown>) : {}
  stored.token = response.data.token
  stored.refreshToken = response.data.refreshToken
  localStorage.setItem(STORAGE_KEY, JSON.stringify(stored))

  // Sync Pinia store if it's already initialised (dynamic import to avoid circular dep)
  try {
    const { useAuthStore } = await import('@/stores/auth')
    const { getActivePinia } = await import('pinia')
    if (getActivePinia()) {
      const store = useAuthStore()
      store.applyTokens(response.data.token, response.data.refreshToken)
    }
  } catch {
    // store not ready — localStorage update above is sufficient
  }
}

function redirectToLogin(): void {
  // Dynamic import to avoid circular dependency with the router
  void import('@/router').then(({ default: router }) => {
    void router.push('/login')
  })
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      if (!refreshPromise) {
        refreshPromise = doRefresh().finally(() => {
          refreshPromise = null
        })
      }

      try {
        await refreshPromise
        // Retry original request with updated token
        const auth = readStoredAuth()
        if (auth.token) {
          originalRequest.headers.Authorization = `Bearer ${auth.token}`
        }
        return await api(originalRequest)
      } catch {
        // Dynamic import to avoid circular dep
        try {
          const { useAuthStore } = await import('@/stores/auth')
          const { getActivePinia } = await import('pinia')
          if (getActivePinia()) {
            useAuthStore().clearAuth()
          } else {
            localStorage.removeItem(STORAGE_KEY)
          }
        } catch {
          localStorage.removeItem(STORAGE_KEY)
        }
        redirectToLogin()
        return Promise.reject(new Error('Sessão expirada. Faça login novamente.'))
      }
    }

    const message = (error.response?.data as { message?: string } | undefined)?.message
      ?? error.message
      ?? 'Requisição falhou'
    return Promise.reject(new Error(message))
  },
)

export default api
