import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
    {
      path: '/',
      component: () => import('@/layouts/AppLayout.vue'),
      children: [
        { path: '', name: 'dashboard', component: () => import('@/views/DashboardView.vue') },
        { path: 'reserves', name: 'reserves', component: () => import('@/views/ReservesView.vue') },
        { path: 'reserves/:id', name: 'reserve-detail', component: () => import('@/views/ReserveDetailView.vue') },
        { path: 'investments', name: 'investments', component: () => import('@/views/InvestmentsView.vue') },
        { path: 'investments/:id', name: 'investment-detail', component: () => import('@/views/InvestmentDetailView.vue') },
        { path: 'properties', name: 'properties', component: () => import('@/views/PropertiesView.vue') },
        { path: 'properties/:id', name: 'property-detail', component: () => import('@/views/PropertyDetailView.vue') },
        { path: 'entries', name: 'entries', component: () => import('@/views/EntriesView.vue') },
        { path: 'income', name: 'income', component: () => import('@/views/IncomeView.vue') },
        { path: 'fixed-costs', name: 'fixed-costs', component: () => import('@/views/FixedCostsView.vue') },
        { path: 'fixed-costs/:id', name: 'fixed-cost-detail', component: () => import('@/views/FixedCostDetailView.vue') },
        { path: 'purchases', name: 'purchases', component: () => import('@/views/PurchasesView.vue') },
        { path: 'purchases/:id', name: 'purchase-detail', component: () => import('@/views/PurchaseDetailView.vue') },
        { path: 'admin/users', name: 'admin-users', component: () => import('@/views/admin/UsersView.vue'), meta: { admin: true } },
        { path: 'admin/bank-accounts', name: 'admin-bank-accounts', component: () => import('@/views/admin/BankAccountsView.vue'), meta: { admin: true } },
        { path: 'admin/investment-types', name: 'admin-investment-types', component: () => import('@/views/admin/InvestmentTypesView.vue'), meta: { admin: true } },
        { path: 'admin/income-types', name: 'admin-income-types', component: () => import('@/views/admin/IncomeTypesView.vue'), meta: { admin: true } },
        { path: 'admin/cost-types', name: 'admin-cost-types', component: () => import('@/views/admin/CostTypesView.vue'), meta: { admin: true } },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.meta.admin && !auth.isAdmin) {
    return { name: 'dashboard' }
  }
  if (to.name === 'login' && auth.isAuthenticated) {
    return { name: 'dashboard' }
  }
})

export default router
