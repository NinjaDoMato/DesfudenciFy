import { describe, expect, it } from 'vitest'
import {
  compareDateStrings,
  displayToDateInputValue,
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
