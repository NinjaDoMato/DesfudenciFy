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
import { formatMoney, moneyPolarity, type DashboardTotals } from '@/types'
import { computeInvestidoTotals, computePatrimonioTotals } from '@/utils/totals'
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

interface CommitmentItem {
  name: string
  value: number
  color: string
}

const totals = ref<DashboardTotals | null>(null)
const monthly = ref<{ month: string; freeCapital: number; investedCapital: number; reservedCapital: number; propertyValue: number }[]>([])
const distribution = ref<DistributionItem[]>([])
const typeDistribution = ref<DistributionItem[]>([])
const upcomingInvestments = ref<UpcomingInvestment[]>([])
const upcomingBills = ref<UpcomingBill[]>([])

const patrimonio = computed(() => {
  if (!totals.value) return null
  return computePatrimonioTotals(
    totals.value.totalFinancialCapital,
    totals.value.totalPropertyAppraisedValue,
    totals.value.totalFreeBalance,
  )
})

const investido = computed(() => {
  if (!totals.value) return null
  return computeInvestidoTotals(
    totals.value.totalInvested,
    totals.value.totalInvestedFromFree,
    totals.value.totalInvestedFromReserves,
    totals.value.retainedProfit,
  )
})

const commitmentItems = computed<CommitmentItem[]>(() => {
  if (!totals.value) return []
  const t = totals.value
  const metas = t.totalInvestmentGoals
  const contasFixas = t.totalFixedCosts
  const parcelamentos = t.totalOperationalCosts - t.totalFixedCosts
  const committed = metas + contasFixas + parcelamentos
  const disponivel = Math.max(0, t.totalFreeBalance - committed)
  return [
    { name: 'Metas de montinhos', value: metas, color: '#3b82f6' },
    { name: 'Contas fixas', value: contasFixas, color: '#f97316' },
    { name: 'Parcelamentos', value: Math.max(0, parcelamentos), color: '#fb7185' },
    { name: 'Disponível', value: disponivel, color: '#4ade80' },
  ].filter((item) => item.value > 0)
})

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
      label: 'Capital Investido',
      data: monthly.value.map((x) => x.investedCapital),
      backgroundColor: cssVar('--success', '#4ade80'),
      stack: 'capital',
      borderSkipped: false,
      borderRadius: { topLeft: 0, topRight: 0, bottomLeft: 6, bottomRight: 6 },
      barPercentage: 0.45,
      categoryPercentage: 0.6,
    },
    {
      label: 'Capital Reservado',
      data: monthly.value.map((x) => x.reservedCapital),
      backgroundColor: cssVar('--chart-blue', '#3b82f6'),
      stack: 'capital',
      borderSkipped: false,
      borderRadius: { topLeft: 0, topRight: 0, bottomLeft: 0, bottomRight: 0 },
      barPercentage: 0.45,
      categoryPercentage: 0.6,
    },
    {
      label: 'Capital Livre',
      data: monthly.value.map((x) => x.freeCapital),
      backgroundColor: '#f59e0b',
      stack: 'capital',
      borderSkipped: false,
      borderRadius: { topLeft: 0, topRight: 0, bottomLeft: 0, bottomRight: 0 },
      barPercentage: 0.45,
      categoryPercentage: 0.6,
    },
    {
      label: 'Valor em Imóveis',
      data: monthly.value.map((x) => x.propertyValue),
      backgroundColor: '#14b8a6',
      stack: 'capital',
      borderSkipped: false,
      borderRadius: { topLeft: 6, topRight: 6, bottomLeft: 0, bottomRight: 0 },
      barPercentage: 0.45,
      categoryPercentage: 0.6,
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
    legend: { display: false },
    tooltip: {
      callbacks: {
        label(context: { label?: string; parsed: number; dataset: { data: number[] } }) {
          const total = context.dataset.data.reduce((a, b) => a + b, 0)
          const pct = total > 0 ? ((context.parsed / total) * 100).toFixed(1) : '0.0'
          return ` ${context.label ?? ''}: ${formatMoney(context.parsed)} (${pct}%)`
        },
      },
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
    <div v-if="totals && patrimonio && investido" class="grid grid-totals-lead dashboard-totals">
      <div class="kpi">
        <div class="label">Patrimônio acumulado</div>
        <div class="kpi-body">
          <div class="value">{{ formatMoney(patrimonio.patrimonio) }}</div>
          <div class="kpi-break">
            <div class="kpi-break-row">
              <span>Valor Reservado</span>
              <strong>{{ formatMoney(patrimonio.somatorioReservas) }}</strong>
            </div>
            <div class="kpi-break-row">
              <span>Saldo livre</span>
              <strong>{{ formatMoney(patrimonio.saldoLivre) }}</strong>
            </div>
            <div v-if="patrimonio.propertyAppraised !== 0" class="kpi-break-row">
              <span>Imóveis</span>
              <strong>{{ formatMoney(patrimonio.propertyAppraised) }}</strong>
            </div>
          </div>
        </div>
      </div>
      <div class="kpi">
        <div class="label">Investido</div>
        <div class="kpi-body">
          <div class="value">{{ formatMoney(investido.totalInvestido) }}</div>
          <div class="kpi-break">
            <div class="kpi-break-row">
              <span>Saldo livre</span>
              <strong>{{ formatMoney(investido.investedFromFree) }}</strong>
            </div>
            <div class="kpi-break-row">
              <span>Montinhos</span>
              <strong>{{ formatMoney(investido.investedFromReserves) }}</strong>
            </div>
            <div class="kpi-break-row">
              <span>Lucro retido</span>
              <strong :class="moneyPolarity(investido.lucroRetido)">{{ formatMoney(investido.lucroRetido) }}</strong>
            </div>
          </div>
        </div>
      </div>
      <div class="kpi">
        <div class="label">Saldo mensal</div>
        <div class="kpi-body">
          <div class="value" :class="moneyPolarity(totals.monthlyBalance)">{{ formatMoney(totals.monthlyBalance) }}</div>
          <div class="kpi-break">
            <div class="kpi-break-row">
              <span>Total de Entradas</span>
              <strong :class="moneyPolarity(totals.totalIncome)">{{ formatMoney(totals.totalIncome) }}</strong>
            </div>
            <div class="kpi-break-row">
              <span>Total de Custos</span>
              <strong :class="moneyPolarity(-totals.totalOperationalCosts)">{{ formatMoney(totals.totalOperationalCosts) }}</strong>
            </div>
            <div class="kpi-break-row">
              <span>Metas de Investimento</span>
              <strong class="accent">{{ formatMoney(totals.totalInvestmentGoals) }}</strong>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="charts-stack">
      <div class="panel chart-main">
        <h2>Evolução do patrimônio</h2>
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
          <div v-if="distribution.length" class="donut-layout">
            <div class="donut-canvas-wrap">
              <Doughnut :data="doughnutData(distribution)" :options="doughnutOptions" />
            </div>
            <ul class="donut-legend">
              <li
                v-for="(item, i) in distribution"
                :key="item.name"
                class="donut-legend-item"
              >
                <span class="donut-legend-swatch" :style="{ background: item.color || DONUT_COLORS[i % DONUT_COLORS.length] }" />
                <span class="donut-legend-label" :title="item.name">{{ item.name }}</span>
                <span class="donut-legend-value">{{ formatMoney(item.value) }}</span>
              </li>
            </ul>
          </div>
          <p v-else class="muted">Nenhuma reserva cadastrada.</p>
        </div>
        <div class="panel chart-side">
          <h2>Tipo de investimento</h2>
          <div v-if="typeDistribution.length" class="donut-layout">
            <div class="donut-canvas-wrap">
              <Doughnut :data="doughnutData(typeDistribution)" :options="doughnutOptions" />
            </div>
            <ul class="donut-legend">
              <li
                v-for="(item, i) in typeDistribution"
                :key="item.name"
                class="donut-legend-item"
              >
                <span class="donut-legend-swatch" :style="{ background: item.color || DONUT_COLORS[i % DONUT_COLORS.length] }" />
                <span class="donut-legend-label" :title="item.name">{{ item.name }}</span>
                <span class="donut-legend-value">{{ formatMoney(item.value) }}</span>
              </li>
            </ul>
          </div>
          <p v-else class="muted">Nenhum investimento ativo.</p>
        </div>
        <div class="panel chart-side">
          <h2>Comprometimento do saldo livre</h2>
          <div v-if="commitmentItems.length" class="donut-layout">
            <div class="donut-canvas-wrap">
              <Doughnut :data="doughnutData(commitmentItems)" :options="doughnutOptions" />
            </div>
            <ul class="donut-legend">
              <li
                v-for="item in commitmentItems"
                :key="item.name"
                class="donut-legend-item"
              >
                <span class="donut-legend-swatch" :style="{ background: item.color }" />
                <span class="donut-legend-label" :title="item.name">{{ item.name }}</span>
                <span class="donut-legend-value">{{ formatMoney(item.value) }}</span>
              </li>
            </ul>
          </div>
          <p v-else class="muted">Nenhum dado disponível.</p>
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
.dashboard-totals .kpi-body {
  flex-wrap: nowrap;
}

.charts-stack {
  display: grid;
  gap: 1rem;
  min-width: 0;
}

.chart-main .chart-frame {
  height: 280px;
}

.donuts-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
  gap: 1rem;
  min-width: 0;
}

.chart-side {
  display: flex;
  flex-direction: column;
}

.chart-frame {
  position: relative;
  width: 100%;
}

.donut-layout {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  min-height: 0;
}

.donut-canvas-wrap {
  flex: 0 0 160px;
  height: 160px;
  position: relative;
}

.donut-legend {
  flex: 1 1 0;
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: 160px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: var(--border) transparent;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.donut-legend-item {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  font-size: 0.8rem;
  min-width: 0;
}

.donut-legend-swatch {
  flex: 0 0 10px;
  width: 10px;
  height: 10px;
  border-radius: 3px;
}

.donut-legend-label {
  flex: 1 1 0;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--muted);
}

.donut-legend-value {
  flex: 0 0 auto;
  font-weight: 600;
  white-space: nowrap;
  font-size: 0.78rem;
}

@media (max-width: 480px) {
  .donut-layout {
    flex-direction: column;
    align-items: center;
  }

  .donut-canvas-wrap {
    flex: 0 0 auto;
    width: 160px;
  }

  .donut-legend {
    max-height: 120px;
    width: 100%;
  }
}

.accent {
  color: var(--accent);
}
</style>
