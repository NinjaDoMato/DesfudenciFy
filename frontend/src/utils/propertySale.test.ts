import { describe, expect, it } from 'vitest'
import { computePropertySalePreview } from './propertySale'

describe('computePropertySalePreview', () => {
  it('should compute lucro as sale minus cost plus rents', () => {
    const preview = computePropertySalePreview({
      saleAmount: 280_000,
      propertyCost: 205_000,
      totalRentPaid: 2_400,
      remainingBalance: 80_000,
      isRented: true,
    })

    expect(preview.profit).toBe(77_400)
    expect(preview.remainingBalanceAfter).toBe(0)
    expect(preview.isRented).toBe(true)
  })

  it('should allow a negative lucro when sale is below cost', () => {
    const preview = computePropertySalePreview({
      saleAmount: 90_000,
      propertyCost: 120_000,
      totalRentPaid: 0,
      remainingBalance: 0,
      isRented: false,
    })

    expect(preview.profit).toBe(-30_000)
  })
})
