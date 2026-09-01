export interface DateParts {
  year: number
  month: number
  day: number
}

/**
 * Extracts calendar date parts from an ISO string without timezone conversion.
 */
export function extractDateParts(value: string): DateParts | null {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (!match) return null

  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  if (!isValidDate(year, month, day)) return null

  return { year, month, day }
}

function isValidDate(year: number, month: number, day: number): boolean {
  const date = new Date(year, month - 1, day)
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
}

/**
 * Formats a date-only value as dd/mm/yyyy.
 */
export function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const parts = extractDateParts(value)
  if (!parts) return ''

  const day = String(parts.day).padStart(2, '0')
  const month = String(parts.month).padStart(2, '0')
  return `${day}/${month}/${parts.year}`
}

/**
 * Formats a datetime value with date in dd/mm/yyyy and local time as HH:mm.
 */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${day}/${month}/${year} ${hours}:${minutes}`
}

/**
 * Converts API/date values to yyyy-mm-dd for form state.
 */
export function toDateInputValue(value: string | null | undefined): string {
  if (!value) return ''
  const parts = extractDateParts(value)
  if (!parts) return ''

  const month = String(parts.month).padStart(2, '0')
  const day = String(parts.day).padStart(2, '0')
  return `${parts.year}-${month}-${day}`
}

/**
 * Returns today's local date as yyyy-mm-dd.
 */
export function todayDateInputValue(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/**
 * Parses a date-only value for sorting without timezone shift.
 */
export function parseDateForSort(value: string | null | undefined): Date | null {
  const parts = extractDateParts(value ?? '')
  if (!parts) return null
  return new Date(parts.year, parts.month - 1, parts.day)
}

/**
 * Compares two date-only strings chronologically.
 */
export function compareDateStrings(a: string, b: string): number {
  const left = parseDateForSort(a)
  const right = parseDateForSort(b)
  if (!left && !right) return 0
  if (!left) return -1
  if (!right) return 1
  return left.getTime() - right.getTime()
}

/**
 * Converts dd/mm/yyyy display text to yyyy-mm-dd.
 */
export function displayToDateInputValue(display: string): string {
  const match = display.match(/^(\d{2})\/(\d{2})\/(\d{4})$/)
  if (!match) return ''

  const day = Number(match[1])
  const month = Number(match[2])
  const year = Number(match[3])
  if (!isValidDate(year, month, day)) return ''

  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

/**
 * Converts yyyy-mm-dd to dd/mm/yyyy for display in inputs.
 */
export function dateInputValueToDisplay(value: string): string {
  return formatDate(value)
}

/**
 * Masks partial user input as dd/mm/yyyy while typing.
 */
export function maskDateInput(raw: string): string {
  const digits = raw.replace(/\D/g, '').slice(0, 8)
  if (digits.length <= 2) return digits
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`
}

/**
 * Serializes a yyyy-mm-dd form value to ISO at local noon to avoid timezone drift.
 */
export function dateInputToIso(value: string): string | null {
  if (!toDateInputValue(value)) return null
  return new Date(`${toDateInputValue(value)}T12:00:00`).toISOString()
}
