import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
})

api.interceptors.request.use((config) => {
  const raw = localStorage.getItem('desfudencify.auth')
  if (raw) {
    const auth = JSON.parse(raw) as { token?: string }
    if (auth.token) {
      config.headers.Authorization = `Bearer ${auth.token}`
    }
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const message = error.response?.data?.message || error.message || 'Request failed'
    return Promise.reject(new Error(message))
  },
)

export default api
