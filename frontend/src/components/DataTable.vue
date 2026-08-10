<script setup lang="ts" generic="T">
import { toRef, watch } from 'vue'
import { useDataTable, type DataTableColumn } from '@/composables/useDataTable'

const props = withDefaults(
  defineProps<{
    rows: T[]
    columns: DataTableColumn<T>[]
    rowKey: string
    pageSize?: number
    initialSortKey?: string
    emptyText?: string
    paginated?: boolean
    clickableRows?: boolean
  }>(),
  {
    pageSize: 10,
    emptyText: 'Nenhum registro encontrado.',
    paginated: true,
    clickableRows: false,
  },
)

const emit = defineEmits<{
  rowClick: [row: T]
}>()

const rowsRef = toRef(props, 'rows')
const table = useDataTable(rowsRef, props.columns, {
  pageSize: props.paginated ? props.pageSize : Number.MAX_SAFE_INTEGER,
  initialSortKey: props.initialSortKey,
})

watch(
  () => props.pageSize,
  (value) => {
    if (!props.paginated) return
    table.pageSize.value = value
    table.page.value = 1
  },
)

watch(
  () => props.paginated,
  (paginated) => {
    table.pageSize.value = paginated ? props.pageSize : Number.MAX_SAFE_INTEGER
    table.page.value = 1
  },
)

function onRowClick(row: T) {
  if (!props.clickableRows) return
  emit('rowClick', row)
}

defineExpose(table)
</script>

<template>
  <div class="data-table">
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th
              v-for="column in columns"
              :key="column.key"
              :class="{ sortable: column.sortable !== false }"
              @click="table.toggleSort(column.key)"
            >
              <span class="th-content">
                {{ column.label }}
                <span v-if="column.sortable !== false" class="sort-indicator">
                  <template v-if="table.sortKey.value === column.key">
                    {{ table.sortDir.value === 'asc' ? '▲' : '▼' }}
                  </template>
                  <template v-else>↕</template>
                </span>
              </span>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="row in table.pagedRows.value"
            :key="String((row as Record<string, unknown>)[rowKey])"
            :class="{ clickable: clickableRows }"
            @click="onRowClick(row)"
          >
            <td v-for="column in columns" :key="column.key">
              <slot :name="`cell-${column.key}`" :row="row" :value="(row as Record<string, unknown>)[column.key]">
                {{ (row as Record<string, unknown>)[column.key] }}
              </slot>
            </td>
          </tr>
          <tr v-if="!table.pagedRows.value.length">
            <td :colspan="columns.length" class="empty">{{ emptyText }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="paginated" class="table-footer">
      <div class="muted">
        {{ table.totalItems.value }} registro(s)
        <span v-if="table.totalItems.value">
          · página {{ table.page.value }} de {{ table.totalPages.value }}
        </span>
      </div>
      <div class="pager">
        <label class="page-size">
          Por página
          <select v-model.number="table.pageSize.value" @change="table.page.value = 1">
            <option :value="5">5</option>
            <option :value="10">10</option>
            <option :value="20">20</option>
            <option :value="50">50</option>
          </select>
        </label>
        <button class="btn secondary" type="button" :disabled="table.page.value <= 1" @click="table.goToPage(table.page.value - 1)">
          Anterior
        </button>
        <button class="btn secondary" type="button" :disabled="table.page.value >= table.totalPages.value" @click="table.goToPage(table.page.value + 1)">
          Próxima
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.data-table {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.table-wrap {
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  text-align: left;
  padding: 0.85rem 0.5rem;
  border-bottom: 1px solid var(--border);
  font-size: 0.92rem;
  vertical-align: middle;
}

th {
  color: var(--muted);
  font-weight: 500;
  user-select: none;
}

th.sortable {
  cursor: pointer;
}

th.sortable:hover {
  color: var(--text);
}

.th-content {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.sort-indicator {
  font-size: 0.7rem;
  opacity: 0.7;
}

tbody tr.clickable {
  cursor: pointer;
  transition: background 0.15s ease;
}

tbody tr.clickable:hover {
  background: var(--bg-soft);
}

.empty {
  color: var(--muted);
  text-align: center !important;
  padding: 1.25rem 0.5rem !important;
}

.table-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.pager {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.page-size {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  color: var(--muted);
  font-size: 0.85rem;
}

.page-size select {
  background: var(--bg-soft);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text);
  padding: 0.4rem 0.55rem;
}
</style>
