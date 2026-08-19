<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import NavIcon from '@/components/NavIcon.vue'

const auth = useAuthStore()
const theme = useThemeStore()
const router = useRouter()
const route = useRoute()

const SIDEBAR_COLLAPSED_KEY = 'sidebarCollapsed'
const sidebarCollapsed = ref<boolean>(false)

if (typeof window !== 'undefined') {
  try {
    const raw = window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY)
    if (raw === 'true') sidebarCollapsed.value = true
    if (raw === 'false') sidebarCollapsed.value = false
  } catch {
    // Ignore persistence errors (private mode, disabled storage, etc.)
  }
}

const openGroups = ref<Record<string, boolean>>({
  capital: true,
  budget: true,
  admin: true,
  settings: true,
})

const isAdminRoute = computed(() => route.path.startsWith('/admin'))
const isSettingsRoute = computed(() =>
  route.path.startsWith('/admin/bank-accounts') ||
  route.path.startsWith('/admin/investment-types') ||
  route.path.startsWith('/admin/income-types') ||
  route.path.startsWith('/admin/cost-types'),
)

watch(
  () => route.path,
  () => {
    if (isAdminRoute.value) openGroups.value.admin = true
    if (isSettingsRoute.value) openGroups.value.settings = true
  },
  { immediate: true },
)

function toggleGroup(key: string) {
  openGroups.value[key] = !openGroups.value[key]
}

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

async function onLogout() {
  await auth.logout()
  await router.push({ name: 'login' })
}

watch(sidebarCollapsed, (val) => {
  try {
    if (typeof window === 'undefined') return
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, val ? 'true' : 'false')
  } catch {
    // Ignore persistence errors
  }
})
</script>

<template>
  <div class="shell" :class="{ collapsed: sidebarCollapsed }">
    <aside class="sidebar" :class="{ collapsed: sidebarCollapsed }">
      <div class="brand-row">
        <div class="brand">
          <span class="brand-mark" aria-hidden="true">DF</span>
          <div class="brand-text">
            <strong>DesfudenciFy</strong>
            <p class="muted">Controle financeiro</p>
          </div>
        </div>

        <button
          class="sidebar-toggle-btn"
          type="button"
          :aria-label="sidebarCollapsed ? 'Expandir menu' : 'Recolher menu'"
          :aria-expanded="!sidebarCollapsed"
          aria-controls="sidebar-nav"
          @click="toggleSidebar"
        >
          <svg
            class="sidebar-toggle-icon"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.8"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
          >
            <path d="M9 18l6-6-6-6" />
          </svg>
        </button>
      </div>

      <nav id="sidebar-nav">
        <RouterLink class="nav-link" to="/" :title="sidebarCollapsed ? 'Dashboard' : undefined">
          <NavIcon name="dashboard" />
          <span class="nav-text">Dashboard</span>
        </RouterLink>

        <div class="nav-group">
          <button
            class="nav-group-toggle"
            type="button"
            :aria-expanded="openGroups.capital"
            :title="sidebarCollapsed ? 'Capital' : undefined"
            @click="toggleGroup('capital')"
          >
            <span class="nav-label">
              <NavIcon name="capital" />
              <span class="nav-text">Capital</span>
            </span>
            <span class="chevron" :class="{ open: openGroups.capital }">▾</span>
          </button>
          <div v-show="openGroups.capital" class="nav-group-items">
            <RouterLink class="nav-link" to="/reserves" :title="sidebarCollapsed ? 'Reservas' : undefined">
              <NavIcon name="reserves" />
              <span class="nav-text">Reservas</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/investments" :title="sidebarCollapsed ? 'Investimentos' : undefined">
              <NavIcon name="investments" />
              <span class="nav-text">Investimentos</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/properties" :title="sidebarCollapsed ? 'Imóveis' : undefined">
              <NavIcon name="properties" />
              <span class="nav-text">Imóveis</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/entries" :title="sidebarCollapsed ? 'Extrato' : undefined">
              <NavIcon name="entries" />
              <span class="nav-text">Extrato</span>
            </RouterLink>
          </div>
        </div>

        <div class="nav-group">
          <button
            class="nav-group-toggle"
            type="button"
            :aria-expanded="openGroups.budget"
            :title="sidebarCollapsed ? 'Orçamento' : undefined"
            @click="toggleGroup('budget')"
          >
            <span class="nav-label">
              <NavIcon name="budget" />
              <span class="nav-text">Orçamento</span>
            </span>
            <span class="chevron" :class="{ open: openGroups.budget }">▾</span>
          </button>
          <div v-show="openGroups.budget" class="nav-group-items">
            <RouterLink class="nav-link" to="/income" :title="sidebarCollapsed ? 'Entradas' : undefined">
              <NavIcon name="income" />
              <span class="nav-text">Entradas</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/fixed-costs" :title="sidebarCollapsed ? 'Contas fixas' : undefined">
              <NavIcon name="fixed-costs" />
              <span class="nav-text">Contas fixas</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/purchases" :title="sidebarCollapsed ? 'Parcelamentos' : undefined">
              <NavIcon name="purchases" />
              <span class="nav-text">Parcelamentos</span>
            </RouterLink>
          </div>
        </div>

        <div v-if="auth.isAdmin" class="nav-group">
          <button
            class="nav-group-toggle"
            type="button"
            :aria-expanded="openGroups.admin"
            :title="sidebarCollapsed ? 'Admin' : undefined"
            @click="toggleGroup('admin')"
          >
            <span class="nav-label">
              <NavIcon name="admin" />
              <span class="nav-text">Admin</span>
            </span>
            <span class="chevron" :class="{ open: openGroups.admin }">▾</span>
          </button>
          <div v-show="openGroups.admin" class="nav-group-items">
            <RouterLink class="nav-link" to="/admin/users" :title="sidebarCollapsed ? 'Usuários' : undefined">
              <NavIcon name="users" />
              <span class="nav-text">Usuários</span>
            </RouterLink>

            <div class="nav-subgroup">
              <button
                class="nav-subgroup-toggle"
                type="button"
                :aria-expanded="openGroups.settings"
                :title="sidebarCollapsed ? 'Configurações' : undefined"
                @click="toggleGroup('settings')"
              >
                <span class="nav-label">
                  <NavIcon name="settings" />
                  <span class="nav-text">Configurações</span>
                </span>
                <span class="chevron" :class="{ open: openGroups.settings }">▾</span>
              </button>
              <div v-show="openGroups.settings" class="nav-subgroup-items">
                <RouterLink
                  class="nav-link nested"
                  to="/admin/investment-types"
                  :title="sidebarCollapsed ? 'Tipos de Investimentos' : undefined"
                >
                  <NavIcon name="investment-types" />
                  <span class="nav-text">Tipos de Investimentos</span>
                </RouterLink>
                <RouterLink
                  class="nav-link nested"
                  to="/admin/income-types"
                  :title="sidebarCollapsed ? 'Tipos de Entrada' : undefined"
                >
                  <NavIcon name="income" />
                  <span class="nav-text">Tipos de Entrada</span>
                </RouterLink>
                <RouterLink
                  class="nav-link nested"
                  to="/admin/cost-types"
                  :title="sidebarCollapsed ? 'Tipos de Custo' : undefined"
                >
                  <NavIcon name="cost-types" />
                  <span class="nav-text">Tipos de Custo</span>
                </RouterLink>
                <RouterLink
                  class="nav-link nested"
                  to="/admin/bank-accounts"
                  :title="sidebarCollapsed ? 'Contas Bancárias' : undefined"
                >
                  <NavIcon name="bank-accounts" />
                  <span class="nav-text">Contas Bancárias</span>
                </RouterLink>
              </div>
            </div>
          </div>
        </div>
      </nav>

      <div class="sidebar-footer">
        <div class="footer-user-text">
          <strong>{{ auth.fullName }}</strong>
          <p class="muted">{{ auth.role }}</p>
        </div>
        <button
          class="btn secondary logout-btn"
          type="button"
          :title="sidebarCollapsed ? theme.label : undefined"
          @click="theme.toggle()"
        >
          <svg class="theme-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <template v-if="theme.isDark">
              <circle cx="12" cy="12" r="4" />
              <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
            </template>
            <template v-else>
              <path d="M21 14.5A8.5 8.5 0 0 1 9.5 3 7 7 0 1 0 21 14.5Z" />
            </template>
          </svg>
          <span class="nav-text">{{ theme.label }}</span>
        </button>
        <button
          class="btn secondary logout-btn"
          type="button"
          :title="sidebarCollapsed ? 'Sair' : undefined"
          @click="onLogout"
        >
          <NavIcon name="logout" />
          <span class="nav-text">Sair</span>
        </button>
      </div>
    </aside>
    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.shell {
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr);
  height: 100vh;
  overflow: hidden;
}

.shell.collapsed {
  grid-template-columns: 86px minmax(0, 1fr);
}

.shell.collapsed .sidebar {
  padding: 1.25rem 0.6rem;
  gap: 1.2rem;
}

.shell.collapsed .brand-row {
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.sidebar {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 1.5rem 1rem;
  border-right: 1px solid var(--border);
  background: var(--sidebar-bg);
  height: 100vh;
  position: sticky;
  top: 0;
  overflow: hidden;
}

.shell.collapsed .sidebar-toggle-icon {
  transform: rotate(180deg);
}

.brand-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.brand-text {
  min-width: 0;
}

.shell.collapsed .brand-text {
  display: none;
}

.sidebar-toggle-btn {
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.03);
  color: var(--muted);
  border-radius: 12px;
  width: 38px;
  height: 38px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}

.sidebar-toggle-btn:hover {
  color: var(--text);
  background: rgba(255, 255, 255, 0.06);
}

.sidebar-toggle-icon {
  width: 1.1rem;
  height: 1.1rem;
  flex-shrink: 0;
  transition: transform 0.2s ease;
}

.brand {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.shell.collapsed .brand {
  width: 100%;
  justify-content: center;
}

.brand p {
  margin: 0;
  font-size: 0.8rem;
}

.brand-mark {
  width: 42px;
  height: 42px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: var(--accent);
  color: var(--btn-on-accent);
  font-weight: 800;
}

.nav-text {
  display: inline;
}

.shell.collapsed .nav-text {
  display: none;
}

.footer-user-text {
  white-space: nowrap;
}

.shell.collapsed .footer-user-text {
  display: none;
}

.shell.collapsed .sidebar-footer {
  align-items: center;
}

nav {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  flex: 1;
  overflow: auto;
}

.nav-group,
.nav-subgroup {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.nav-label {
  display: inline-flex;
  align-items: center;
  gap: 0.65rem;
  min-width: 0;
  white-space: nowrap;
}

.nav-group-toggle,
.nav-subgroup-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  border: none;
  background: transparent;
  color: var(--muted);
  padding: 0.7rem 0.9rem;
  border-radius: 12px;
  cursor: pointer;
  font: inherit;
  font-weight: 600;
  text-align: left;
  transition: background 0.15s ease, color 0.15s ease;
  white-space: nowrap;
}

.nav-group-toggle:hover,
.nav-subgroup-toggle:hover {
  color: var(--text);
  background: rgba(255, 255, 255, 0.03);
}

.nav-subgroup-toggle {
  font-weight: 500;
  color: var(--muted);
}

.chevron {
  display: inline-flex;
  transition: transform 0.2s ease;
  opacity: 0.7;
  font-size: 0.85rem;
}

.chevron.open {
  transform: rotate(180deg);
}

.nav-group-items,
.nav-subgroup-items {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  margin-left: 0.9rem;
  padding: 0.15rem 0 0.15rem 0.45rem;
  border-left: 2px solid var(--border);
}

.nav-subgroup-items {
  margin-left: 0.65rem;
  padding-left: 0.5rem;
  border-left-color: color-mix(in srgb, var(--accent) 45%, var(--border));
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.65rem 0.9rem;
  border-radius: 12px;
  color: var(--muted);
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
  white-space: nowrap;
}

/* Nested links: clearer hierarchy vs muted group toggles */
.nav-group-items .nav-link,
.nav-subgroup-items .nav-link {
  color: var(--text);
  font-weight: 500;
  padding: 0.55rem 0.75rem;
  border-radius: 0 10px 10px 0;
  border-left: 3px solid transparent;
  margin-left: -2px;
}

.nav-link.nested {
  padding-left: 0.7rem;
  font-size: 0.93rem;
}

.nav-link:hover {
  color: var(--text);
  background: var(--bg-soft);
}

.nav-link.router-link-active {
  background: var(--accent-soft);
  color: var(--accent-strong);
  font-weight: 600;
}

.nav-group-items .nav-link.router-link-active,
.nav-subgroup-items .nav-link.router-link-active {
  border-left-color: var(--accent);
  color: var(--accent-strong);
}

.shell.collapsed .nav-label {
  gap: 0;
  justify-content: center;
}

.shell.collapsed .nav-link,
.shell.collapsed .nav-group-toggle,
.shell.collapsed .nav-subgroup-toggle {
  padding-left: 0.55rem;
  padding-right: 0.55rem;
}

.shell.collapsed .nav-link {
  gap: 0;
  justify-content: center;
}

.shell.collapsed .nav-link.nested {
  padding-left: 0.55rem;
}

.shell.collapsed .nav-group-items,
.shell.collapsed .nav-subgroup-items {
  margin-left: 0.35rem;
  padding-left: 0.25rem;
}

.shell.collapsed .nav-subgroup-items {
  margin-left: 0.25rem;
}

.shell.collapsed .nav-group-items .nav-link,
.shell.collapsed .nav-subgroup-items .nav-link {
  margin-left: 0;
  padding-left: 0.55rem;
}

.sidebar-footer {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}

.sidebar-footer p {
  margin: 0;
  font-size: 0.8rem;
}

.logout-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.theme-icon {
  width: 1.05rem;
  height: 1.05rem;
  flex-shrink: 0;
}

.content {
  padding: 1.5rem;
  overflow-y: auto;
  min-width: 0;
  min-height: 0;
  height: 100vh;
}

@media (max-width: 900px) {
  .shell {
    grid-template-columns: 1fr;
    height: auto;
    min-height: 100vh;
    overflow: visible;
  }

  .shell.collapsed {
    grid-template-columns: 1fr;
  }

  .sidebar {
    position: static;
    height: auto;
    overflow: visible;
    border-right: none;
    border-bottom: 1px solid var(--border);
  }

  .shell.collapsed .sidebar {
    padding: 1.5rem 1rem;
    gap: 1.5rem;
  }

  .content {
    height: auto;
    overflow: visible;
    padding: 1rem;
  }
}
</style>
