import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

export type ThemeMode = 'light' | 'dark'

const STORAGE_KEY = 'desfudencify.theme'

function readStoredTheme(): ThemeMode {
  const stored = localStorage.getItem(STORAGE_KEY)
  return stored === 'light' || stored === 'dark' ? stored : 'dark'
}

function applyTheme(mode: ThemeMode) {
  document.documentElement.setAttribute('data-theme', mode)
}

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>(readStoredTheme())

  const isDark = computed(() => mode.value === 'dark')
  const isLight = computed(() => mode.value === 'light')
  const label = computed(() => (mode.value === 'dark' ? 'Tema claro' : 'Tema escuro'))

  function setMode(next: ThemeMode) {
    mode.value = next
    localStorage.setItem(STORAGE_KEY, next)
    applyTheme(next)
  }

  function toggle() {
    setMode(mode.value === 'dark' ? 'light' : 'dark')
  }

  function init() {
    applyTheme(mode.value)
  }

  return { mode, isDark, isLight, label, setMode, toggle, init }
})
