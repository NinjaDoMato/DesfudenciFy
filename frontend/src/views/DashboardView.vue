<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Bar, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  BarElement,
  CategoryScale,
  LinearScale,
  ArcElement,
  Tooltip,
  Legend,
} from 'chart.js'
import api from '@/api/client'
import { formatMoney, type DashboardTotals } from '@/types'
import DataTable from '@/components/DataTable.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useThemeStore } from '@/stores/theme'
import { useToastStore } from '@/stores/toast'

ChartJS.register(BarElement, CategoryScale, LinearScale, ArcElement, Tooltip, Legend)

const router = useRouter()
const theme = useThemeStore()
const toast = useToastStore()

function cssVar(name: string, fallback: string) {
  void theme.mode
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value || fallback
}

interface UpcomingInvestment {
  id: string
  name: string
  endDate: string
  currentAmount: number
}

interface UpcomingBill {
  kind: string
  id: string
  name: string
  amount: number
  dueDate?: string | null
  targetId?: string | null
}

interface DistributionItem {
  name: string
  value: number
  color?: string | null
}

type UpcomingBillRow = UpcomingBill & { rowKey: string }

const DONUT_COLORS = ['#38bdf8', '#4ade80', '#3b82f6', '#a855f7', '#fb7185', '#f59e0b', '#14b8a6']

const totals = ref<DashboardTotals | null>(null)
const monthly = ref<{ month: string; freeCapital: number; investedCapital: number }[]>([])
const distribution = ref<DistributionItem[]>([])
const typeDistribution = ref<DistributionItem[]>([])
const upcomingInvestments = ref<UpcomingInvestment[]>([])
const upcomingBills = ref<UpcomingBill[]>([])
function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

const upcomingBillRows = computed<UpcomingBillRow[]>(() =>
  upcomingBills.value.map((item) => ({ ...item, rowKey: `${item.id}-${item.kind}` })),
)

const investmentColumns: DataTableColumn<UpcomingInvestment>[] = [
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'endDate', label: 'Vencimento', sortValue: (row) => new Date(row.endDate) },
  { key: 'currentAmount', label: 'Valor', sortValue: (row) => row.currentAmount },
]

const billColumns: DataTableColumn<UpcomingBillRow>[] = [
  { key: 'kind', label: 'Tipo', sortValue: (row) => row.kind },
  { key: 'name', label: 'Nome', sortValue: (row) => row.name },
  { key: 'dueDate', label: 'Vencimento', sortValue: (row) => (row.dueDate ? new Date(row.dueDate) : null) },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
]

const monthlyChartData = computed(() => ({
  labels: monthly.value.map((x) => x.month),
  datasets: [
    {
      label: 'Investido',
      data: monthly.value.map((x) => x.investedCapital),
      backgroundColor: cssVar('--chart-blue', '#3b82f6'),
      stack: 'capital',
      borderSkipped: false,
      borderRadius: { topLeft: 0, topRight: 0, bottomLeft: 6, bottomRight: 6 },
    },
    {
      label: 'Livre',
      data: monthly.value.map((x) => x.freeCapital),
      backgroundColor: cssVar('--success', '#4ade80'),
      stack: 'capital',
      borderSkipped: false,
      borderRadius: { topLeft: 6, topRight: 6, bottomLeft: 0, bottomRight: 0 },
    },
  ],
}))

const monthlyChartOptions = computed(() => {
  const muted = cssVar('--chart-muted', '#8a8aa0')
  const grid = cssVar('--chart-grid', 'rgba(255,255,255,0.06)')
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom' as const,
        labels: { color: muted, boxWidth: 12, padding: 16 },
      },
      tooltip: {
        mode: 'index' as const,
        intersect: false,
        callbacks: {
          label(context: { dataset: { label?: string }; parsed: { y: number | null } }) {
            const label = context.dataset.label || ''
            const value = context.parsed.y ?? 0
            return `${label}: ${formatMoney(value)}`
          },
          footer(items: { parsed: { y: number | null } }[]) {
            const total = items.reduce((sum, item) => sum + (item.parsed.y ?? 0), 0)
            return `Total: ${formatMoney(total)}`
          },
        },
      },
    },
    scales: {
      x: {
        stacked: true,
        ticks: { color: muted },
        grid: { color: grid },
      },
      y: {
        stacked: true,
        beginAtZero: true,
        ticks: {
          color: muted,
          callback(value: string | number) {
            return formatMoney(Number(value))
          },
        },
        grid: { color: grid },
      },
    },
  }
})

function doughnutData(items: DistributionItem[]) {
  return {
    labels: items.map((x) => x.name),
    datasets: [{
      data: items.map((x) => x.value),
      backgroundColor: items.map((x, i) => x.color || DONUT_COLORS[i % DONUT_COLORS.length]),
      borderWidth: 0,
    }],
  }
}

const doughnutOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '62%',
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: { color: cssVar('--chart-muted', '#8a8aa0'), boxWidth: 12, padding: 12 },
    },
  },
}))

function openInvestment(row: UpcomingInvestment) {
  void router.push({ name: 'investment-detail', params: { id: row.id } })
}

function openBill(row: UpcomingBillRow) {
  if (row.kind === 'FixedCost') {
    void router.push({ name: 'fixed-cost-detail', params: { id: row.targetId || row.id } })
    return
  }
  if (row.targetId) {
    void router.push({ name: 'purchase-detail', params: { id: row.targetId } })
    return
  }
  void router.push({ name: 'purchases' })
}

onMounted(async () => {
  try {
    const [t, m, d, td, i, b] = await Promise.all([
      api.get('/dashboard/totals'),
      api.get('/dashboard/monthly-capital'),
      api.get('/dashboard/reserve-distribution'),
      api.get('/dashboard/investment-type-distribution'),
      api.get('/dashboard/upcoming-investments'),
      api.get('/dashboard/upcoming-bills'),
    ])
    totals.value = t.data
    monthly.value = m.data
    distribution.value = d.data
    typeDistribution.value = td.data
    upcomingInvestments.value = i.data
    upcomingBills.value = b.data
  } catch (e) {
    toastError(e, 'Erro ao carregar dashboard')
  }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h1>Dashboard</h1>
        <p class="muted">Visão geral do capital livre, investido e compromissos.</p>
      </div>
    </div>
    <div v-if="totals" class="grid grid-4">
      <div class="kpi"><div class="label">Total acumulado</div><div class="value">{{ formatMoney(totals.totalAccumulated) }}</div></div>
      <div class="kpi"><div class="label">Saldo livre</div><div class="value">{{ formatMoney(totals.totalFreeBalance) }}</div></div>
      <div class="kpi"><div class="label">Investido</div><div class="value">{{ formatMoney(totals.totalInvested) }}</div></div>
      <div class="kpi"><div class="label">Saldo mensal</div><div class="value">{{ formatMoney(totals.monthlyBalance) }}</div></div>
    </div>
    <div class="charts-stack">
      <div class="panel chart-main">
        <h2>Capital livre x investido</h2>
        <div class="chart-frame">
          <Bar
            v-if="monthly.length"
            :data="monthlyChartData"
            :options="monthlyChartOptions"
          />
        </div>
      </div>
      <div class="donuts-row">
        <div class="panel chart-side">
          <h2>Distribuição por reserva</h2>
          <div v-if="distribution.length" class="chart-frame chart-frame-doughnut">
            <Doughnut :data="doughnutData(distribution)" :options="doughnutOptions" />
          </div>
          <p v-else class="muted">Nenhuma reserva cadastrada.</p>
        </div>
        <div class="panel chart-side">
          <h2>Tipo de investimento</h2>
          <div v-if="typeDistribution.length" class="chart-frame chart-frame-doughnut">
            <Doughnut :data="doughnutData(typeDistribution)" :options="doughnutOptions" />
          </div>
          <p v-else class="muted">Nenhum investimento ativo.</p>
        </div>
      </div>
    </div>
    <div class="grid grid-2">
      <div class="panel">
        <h2>Próximos investimentos</h2>
        <DataTable
          :rows="upcomingInvestments"
          :columns="investmentColumns"
          row-key="id"
          :paginated="false"
          clickable-rows
          initial-sort-key="endDate"
          empty-text="Nenhum vencimento próximo."
          @row-click="openInvestment"
        >
          <template #cell-endDate="{ row }">{{ new Date(row.endDate).toLocaleDateString('pt-BR') }}</template>
          <template #cell-currentAmount="{ row }">{{ formatMoney(row.currentAmount) }}</template>
        </DataTable>
      </div>
      <div class="panel">
        <h2>Próximas contas</h2>
        <DataTable
          :rows="upcomingBillRows"
          :columns="billColumns"
          row-key="rowKey"
          :paginated="false"
          clickable-rows
          initial-sort-key="name"
          empty-text="Nenhuma conta pendente."
          @row-click="openBill"
        >
          <template #cell-kind="{ row }">
            <span class="badge">{{ row.kind === 'FixedCost' ? 'Conta fixa' : 'Parcelamento' }}</span>
          </template>
          <template #cell-dueDate="{ row }">
            {{ row.dueDate ? new Date(row.dueDate).toLocaleDateString('pt-BR') : '-' }}
          </template>
          <template #cell-amount="{ row }">{{ formatMoney(row.amount) }}</template>
        </DataTable>
      </div>
    </div>
  </div>
</template>

<style scoped>
.charts-stack {
  display: grid;
  gap: 1rem;
}

.chart-main .chart-frame {
  height: 280px;
}

.donuts-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.chart-side {
  display: flex;
  flex-direction: column;
}

.chart-frame {
  position: relative;
  width: 100%;
}

.chart-frame-doughnut {
  height: 240px;
  max-width: 280px;
  width: 100%;
  margin: 0 auto;
}

@media (max-width: 640px) {
  .donuts-row {
    grid-template-columns: 1fr;
  }

  .chart-frame-doughnut {
    max-width: 240px;
  }
}
</style>
