import { computed, ref, watch, type Ref } from 'vue'

export type SortDirection = 'asc' | 'desc'

export interface DataTableColumn<T> {
  key: string
  label: string
  sortable?: boolean
  sortValue?: (row: T) => string | number | Date | boolean | null | undefined
}

function compareValues(a: unknown, b: unknown): number {
  if (a == null && b == null) return 0
  if (a == null) return -1
  if (b == null) return 1

  if (typeof a === 'number' && typeof b === 'number') return a - b
  if (typeof a === 'boolean' && typeof b === 'boolean') return Number(a) - Number(b)
  if (a instanceof Date && b instanceof Date) return a.getTime() - b.getTime()

  return String(a).localeCompare(String(b), 'pt-BR', { sensitivity: 'base', numeric: true })
}

export function useDataTable<T>(
  rows: Ref<T[]>,
  columns: DataTableColumn<T>[],
  options?: { pageSize?: number; initialSortKey?: string; initialSortDir?: SortDirection },
) {
  const pageSize = ref(options?.pageSize ?? 10)
  const page = ref(1)
  const sortKey = ref<string | null>(options?.initialSortKey ?? null)
  const sortDir = ref<SortDirection>(options?.initialSortDir ?? 'asc')

  watch(rows, () => {
    page.value = 1
  })

  function toggleSort(key: string) {
    const column = columns.find((c) => c.key === key)
    if (!column || column.sortable === false) return

    if (sortKey.value === key) {
      sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortKey.value = key
      sortDir.value = 'asc'
    }
    page.value = 1
  }

  const sortedRows = computed(() => {
    const data = [...rows.value]
    if (!sortKey.value) return data

    const column = columns.find((c) => c.key === sortKey.value)
    if (!column) return data

    data.sort((left, right) => {
      const leftValue = column.sortValue
        ? column.sortValue(left)
        : (left as unknown as Record<string, unknown>)[column.key]
      const rightValue = column.sortValue
        ? column.sortValue(right)
        : (right as unknown as Record<string, unknown>)[column.key]
      const result = compareValues(leftValue, rightValue)
      return sortDir.value === 'asc' ? result : -result
    })

    return data
  })

  const totalItems = computed(() => sortedRows.value.length)
  const totalPages = computed(() => Math.max(1, Math.ceil(totalItems.value / pageSize.value)))

  watch([totalPages, page], () => {
    if (page.value > totalPages.value) page.value = totalPages.value
  })

  const pagedRows = computed(() => {
    const start = (page.value - 1) * pageSize.value
    return sortedRows.value.slice(start, start + pageSize.value)
  })

  function goToPage(next: number) {
    page.value = Math.min(Math.max(1, next), totalPages.value)
  }

  return {
    page,
    pageSize,
    sortKey,
    sortDir,
    sortedRows,
    pagedRows,
    totalItems,
    totalPages,
    toggleSort,
    goToPage,
  }
}
