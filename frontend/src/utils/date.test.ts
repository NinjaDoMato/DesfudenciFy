import { describe, expect, it } from 'vitest'
import {
  compareDateStrings,
  daysUntilDue,
  displayToDateInputValue,
  dueDateUrgency,
  dueDateUrgencyLabel,
  formatDate,
  formatDateTime,
  maskDateInput,
  parseDateForSort,
  toDateInputValue,
} from '@/utils/date'

describe('formatDate', () => {
  it('should format ISO date strings as dd/mm/yyyy without timezone shift', () => {
    expect(formatDate('2027-03-01T00:00:00.000Z')).toBe('01/03/2027')
    expect(formatDate('2027-03-01')).toBe('01/03/2027')
  })

  it('should return empty string for missing values', () => {
    expect(formatDate(null)).toBe('')
    expect(formatDate('')).toBe('')
  })
})

describe('toDateInputValue', () => {
  it('should extract yyyy-mm-dd from API values', () => {
    expect(toDateInputValue('2027-03-01T00:00:00.000Z')).toBe('2027-03-01')
  })
})

describe('displayToDateInputValue', () => {
  it('should parse dd/mm/yyyy into yyyy-mm-dd', () => {
    expect(displayToDateInputValue('01/03/2027')).toBe('2027-03-01')
  })

  it('should reject invalid calendar dates', () => {
    expect(displayToDateInputValue('31/02/2027')).toBe('')
  })
})

describe('maskDateInput', () => {
  it('should mask digits as dd/mm/yyyy', () => {
    expect(maskDateInput('01032027')).toBe('01/03/2027')
    expect(maskDateInput('01/03/2027')).toBe('01/03/2027')
  })
})

describe('parseDateForSort', () => {
  it('should sort date-only values in calendar order', () => {
    const left = parseDateForSort('2027-03-01T00:00:00.000Z')
    const right = parseDateForSort('2027-02-28T00:00:00.000Z')
    expect(left && right && left.getTime() > right.getTime()).toBe(true)
  })
})

describe('compareDateStrings', () => {
  it('should compare date-only strings chronologically', () => {
    expect(compareDateStrings('2027-02-28', '2027-03-01')).toBeLessThan(0)
    expect(compareDateStrings('2027-03-01T00:00:00.000Z', '2027-02-28')).toBeGreaterThan(0)
  })
})

describe('formatDateTime', () => {
  it('should format datetime with dd/mm/yyyy date part', () => {
    const formatted = formatDateTime('2027-03-01T15:30:00.000Z')
    expect(formatted.startsWith('01/03/2027')).toBe(true)
  })
})

describe('dueDateUrgency', () => {
  const today = '2026-09-10'

  it('should mark past dates as overdue', () => {
    expect(dueDateUrgency('2026-09-09', today)).toBe('overdue')
    expect(dueDateUrgencyLabel('2026-09-01T00:00:00.000Z', today)).toBe('Atrasado')
  })

  it('should mark due within 0–5 days as soon', () => {
    expect(dueDateUrgency('2026-09-10', today)).toBe('soon')
    expect(dueDateUrgency('2026-09-15', today)).toBe('soon')
    expect(dueDateUrgencyLabel('2026-09-10', today)).toBe('Vence hoje')
    expect(dueDateUrgencyLabel('2026-09-11', today)).toBe('Em 1 dia')
    expect(dueDateUrgencyLabel('2026-09-15', today)).toBe('Em 5 dias')
  })

  it('should mark due within 6–10 days as upcoming', () => {
    expect(dueDateUrgency('2026-09-16', today)).toBe('upcoming')
    expect(dueDateUrgency('2026-09-20', today)).toBe('upcoming')
    expect(dueDateUrgencyLabel('2026-09-16', today)).toBe('Vence em 6 dias')
    expect(dueDateUrgencyLabel('2026-09-20', today)).toBe('Vence em 10 dias')
  })

  it('should return null beyond 10 days or for invalid dates', () => {
    expect(dueDateUrgency('2026-09-21', today)).toBeNull()
    expect(dueDateUrgencyLabel('2026-09-21', today)).toBeNull()
    expect(dueDateUrgency(null, today)).toBeNull()
    expect(daysUntilDue('2026-09-18', today)).toBe(8)
  })
})
