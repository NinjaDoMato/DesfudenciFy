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

async function onLogout() {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="shell">
    <aside class="sidebar">
      <div class="brand">
        <span class="brand-mark">DF</span>
        <div>
          <strong>DesfudenciFy</strong>
          <p class="muted">Controle financeiro</p>
        </div>
      </div>

      <nav>
        <RouterLink class="nav-link" to="/">
          <NavIcon name="dashboard" />
          <span>Dashboard</span>
        </RouterLink>

        <div class="nav-group">
          <button
            class="nav-group-toggle"
            type="button"
            :aria-expanded="openGroups.capital"
            @click="toggleGroup('capital')"
          >
            <span class="nav-label">
              <NavIcon name="capital" />
              <span>Capital</span>
            </span>
            <span class="chevron" :class="{ open: openGroups.capital }">▾</span>
          </button>
          <div v-show="openGroups.capital" class="nav-group-items">
            <RouterLink class="nav-link" to="/reserves">
              <NavIcon name="reserves" />
              <span>Reservas</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/investments">
              <NavIcon name="investments" />
              <span>Investimentos</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/properties">
              <NavIcon name="properties" />
              <span>Imóveis</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/entries">
              <NavIcon name="entries" />
              <span>Extrato</span>
            </RouterLink>
          </div>
        </div>

        <div class="nav-group">
          <button
            class="nav-group-toggle"
            type="button"
            :aria-expanded="openGroups.budget"
            @click="toggleGroup('budget')"
          >
            <span class="nav-label">
              <NavIcon name="budget" />
              <span>Orçamento</span>
            </span>
            <span class="chevron" :class="{ open: openGroups.budget }">▾</span>
          </button>
          <div v-show="openGroups.budget" class="nav-group-items">
            <RouterLink class="nav-link" to="/income">
              <NavIcon name="income" />
              <span>Entradas</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/fixed-costs">
              <NavIcon name="fixed-costs" />
              <span>Contas fixas</span>
            </RouterLink>
            <RouterLink class="nav-link" to="/purchases">
              <NavIcon name="purchases" />
              <span>Parcelamentos</span>
            </RouterLink>
          </div>
        </div>

        <div v-if="auth.isAdmin" class="nav-group">
          <button
            class="nav-group-toggle"
            type="button"
            :aria-expanded="openGroups.admin"
            @click="toggleGroup('admin')"
          >
            <span class="nav-label">
              <NavIcon name="admin" />
              <span>Admin</span>
            </span>
            <span class="chevron" :class="{ open: openGroups.admin }">▾</span>
          </button>
          <div v-show="openGroups.admin" class="nav-group-items">
            <RouterLink class="nav-link" to="/admin/users">
              <NavIcon name="users" />
              <span>Usuários</span>
            </RouterLink>

            <div class="nav-subgroup">
              <button
                class="nav-subgroup-toggle"
                type="button"
                :aria-expanded="openGroups.settings"
                @click="toggleGroup('settings')"
              >
                <span class="nav-label">
                  <NavIcon name="settings" />
                  <span>Configurações</span>
                </span>
                <span class="chevron" :class="{ open: openGroups.settings }">▾</span>
              </button>
              <div v-show="openGroups.settings" class="nav-subgroup-items">
                <RouterLink class="nav-link nested" to="/admin/investment-types">
                  <NavIcon name="investment-types" />
                  <span>Tipos de Investimentos</span>
                </RouterLink>
                <RouterLink class="nav-link nested" to="/admin/income-types">
                  <NavIcon name="income" />
                  <span>Tipos de Entrada</span>
                </RouterLink>
                <RouterLink class="nav-link nested" to="/admin/cost-types">
                  <NavIcon name="cost-types" />
                  <span>Tipos de Custo</span>
                </RouterLink>
                <RouterLink class="nav-link nested" to="/admin/bank-accounts">
                  <NavIcon name="bank-accounts" />
                  <span>Contas Bancárias</span>
                </RouterLink>
              </div>
            </div>
          </div>
        </div>
      </nav>

      <div class="sidebar-footer">
        <div>
          <strong>{{ auth.fullName }}</strong>
          <p class="muted">{{ auth.role }}</p>
        </div>
        <button class="btn secondary logout-btn" type="button" @click="theme.toggle()">
          <svg class="theme-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <template v-if="theme.isDark">
              <circle cx="12" cy="12" r="4" />
              <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
            </template>
            <template v-else>
              <path d="M21 14.5A8.5 8.5 0 0 1 9.5 3 7 7 0 1 0 21 14.5Z" />
            </template>
          </svg>
          <span>{{ theme.label }}</span>
        </button>
        <button class="btn secondary logout-btn" type="button" @click="onLogout">
          <NavIcon name="logout" />
          <span>Sair</span>
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

.brand {
  display: flex;
  gap: 0.75rem;
  align-items: center;
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

  .sidebar {
    position: static;
    height: auto;
    overflow: visible;
    border-right: none;
    border-bottom: 1px solid var(--border);
  }

  .content {
    height: auto;
    overflow: visible;
    padding: 1rem;
  }
}
</style>
