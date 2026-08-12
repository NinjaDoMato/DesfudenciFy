<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import api from '@/api/client'
import { formatMoney, type Entry, type EntryDestination, type Reserve } from '@/types'
import MoneyInput from '@/components/MoneyInput.vue'
import DataTable from '@/components/DataTable.vue'
import IconButton from '@/components/IconButton.vue'
import type { DataTableColumn } from '@/composables/useDataTable'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
const items = ref<Entry[]>([])
const reserves = ref<Reserve[]>([])
const freeBalance = ref(0)
const showEntry = ref(false)
const showTransfer = ref(false)

const entryForm = reactive({
  amount: 0,
  observation: '',
  destination: 'FreeBalance' as EntryDestination,
  reserveId: '',
})

const transferForm = reactive({
  amount: 0,
  observation: '',
  sourceDestination: 'FreeBalance' as EntryDestination,
  sourceReserveId: '',
  targetDestination: 'Reserve' as EntryDestination,
  targetReserveId: '',
})

const columns: DataTableColumn<Entry>[] = [
  { key: 'occurredAt', label: 'Data', sortValue: (row) => new Date(row.occurredAt) },
  {
    key: 'destination',
    label: 'Destino',
    sortValue: (row) => (row.destination === 'FreeBalance' ? 'Saldo livre' : row.reserveName),
  },
  { key: 'observation', label: 'Obs.', sortValue: (row) => row.observation },
  { key: 'amount', label: 'Valor', sortValue: (row) => row.amount },
  { key: 'actions', label: '', sortable: false },
]

function toastError(e: unknown, fallback: string) {
  toast.error(e instanceof Error ? e.message : fallback)
}

async function load() {
  const [entries, reservesRes, free] = await Promise.all([
    api.get<Entry[]>('/entries'),
    api.get<Reserve[]>('/reserves'),
    api.get<{ amount: number }>('/entries/free-balance'),
  ])
  items.value = entries.data
  reserves.value = reservesRes.data
  freeBalance.value = free.data.amount
}

async function createEntry() {
  try {
    await api.post('/entries', {
      amount: Number(entryForm.amount),
      observation: entryForm.observation,
      destination: entryForm.destination,
      reserveId: entryForm.destination === 'Reserve' ? entryForm.reserveId : null,
    })
    showEntry.value = false
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function transfer() {
  try {
    await api.post('/entries/transfer', {
      amount: Number(transferForm.amount),
      observation: transferForm.observation,
      sourceDestination: transferForm.sourceDestination,
      sourceReserveId: transferForm.sourceDestination === 'Reserve' ? transferForm.sourceReserveId : null,
      targetDestination: transferForm.targetDestination,
      targetReserveId: transferForm.targetDestination === 'Reserve' ? transferForm.targetReserveId : null,
    })
    showTransfer.value = false
    await load()
  } catch (e) {
    toastError(e, 'Erro')
  }
}

async function remove(id: string) {
  if (!confirm('Excluir lançamento?')) return
  await api.delete(`/entries/${id}`)
  await load()
}

onMounted(async () => {
  try { await load() } catch (e) { toastError(e, 'Erro') }
})
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h1>Extrato</h1>
        <p class="muted">Saldo livre: <strong>{{ formatMoney(freeBalance) }}</strong></p>
      </div>
      <div class="actions">
        <button class="btn" type="button" @click="showEntry = true">Novo lançamento</button>
        <button class="btn secondary" type="button" @click="showTransfer = true">Transferir</button>
      </div>
    </div>
    <div class="panel">
      <DataTable :rows="items" :columns="columns" row-key="id" initial-sort-key="occurredAt">
        <template #cell-occurredAt="{ row }">{{ new Date(row.occurredAt).toLocaleString('pt-BR') }}</template>
        <template #cell-destination="{ row }">
          {{ row.destination === 'FreeBalance' ? 'Saldo livre' : row.reserveName }}
        </template>
        <template #cell-amount="{ row }">
          <span :style="{ color: row.amount >= 0 ? 'var(--success)' : 'var(--danger)' }">{{ formatMoney(row.amount) }}</span>
        </template>
        <template #cell-actions="{ row }">
          <IconButton label="Excluir" icon="delete" variant="danger" @click="remove(row.id)" />
        </template>
      </DataTable>
    </div>

    <div v-if="showEntry" class="modal-backdrop" @click.self="showEntry = false">
      <form class="modal" @submit.prevent="createEntry">
        <h2>Novo lançamento</h2>
        <div class="field"><label>Valor</label><MoneyInput v-model="entryForm.amount" allow-negative required /></div>
        <div class="field"><label>Observação</label><input v-model="entryForm.observation" /></div>
        <div class="field">
          <label>Destino</label>
          <select v-model="entryForm.destination">
            <option value="FreeBalance">Saldo livre</option>
            <option value="Reserve">Reserva</option>
          </select>
        </div>
        <div v-if="entryForm.destination === 'Reserve'" class="field">
          <label>Reserva</label>
          <select v-model="entryForm.reserveId" required>
            <option disabled value="">Selecione</option>
            <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>
        <div class="actions">
          <button class="btn" type="submit">Salvar</button>
          <button class="btn secondary" type="button" @click="showEntry = false">Cancelar</button>
        </div>
      </form>
    </div>

    <div v-if="showTransfer" class="modal-backdrop" @click.self="showTransfer = false">
      <form class="modal" @submit.prevent="transfer">
        <h2>Transferência</h2>
        <div class="field"><label>Valor</label><MoneyInput v-model="transferForm.amount" required /></div>
        <div class="field"><label>Observação</label><input v-model="transferForm.observation" /></div>
        <div class="field">
          <label>Origem</label>
          <select v-model="transferForm.sourceDestination">
            <option value="FreeBalance">Saldo livre</option>
            <option value="Reserve">Reserva</option>
          </select>
        </div>
        <div v-if="transferForm.sourceDestination === 'Reserve'" class="field">
          <label>Reserva origem</label>
          <select v-model="transferForm.sourceReserveId" required>
            <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>
        <div class="field">
          <label>Destino</label>
          <select v-model="transferForm.targetDestination">
            <option value="FreeBalance">Saldo livre</option>
            <option value="Reserve">Reserva</option>
          </select>
        </div>
        <div v-if="transferForm.targetDestination === 'Reserve'" class="field">
          <label>Reserva destino</label>
          <select v-model="transferForm.targetReserveId" required>
            <option v-for="r in reserves" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </div>
        <div class="actions">
          <button class="btn" type="submit">Transferir</button>
          <button class="btn secondary" type="button" @click="showTransfer = false">Cancelar</button>
        </div>
      </form>
    </div>
  </div>
</template>
