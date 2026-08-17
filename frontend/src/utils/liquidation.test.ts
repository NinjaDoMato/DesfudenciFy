import { describe, expect, it } from 'vitest'
import { buildLiquidationSummary, FREE_INVESTMENT_SOURCE } from './liquidation'

describe('buildLiquidationSummary', () => {
  it('should share profit proportionally by invested amount', () => {
    const summary = buildLiquidationSummary({
      startAmount: 1000,
      currentAmount: 1200,
      sourceReserves: [
        { reserveId: null, amount: 400 },
        { reserveId: 'r1', amount: 600 },
      ],
    })

    expect(summary.invested).toBe(1000)
    expect(summary.finalValue).toBe(1200)
    expect(summary.profit).toBe(200)
    expect(summary.distributions[0]).toMatchObject({
      sourceKey: FREE_INVESTMENT_SOURCE,
      reserveId: null,
      investedAmount: 400,
      proportion: 0.4,
      profitShare: 80,
    })
    expect(summary.distributions[1]).toMatchObject({
      reserveId: 'r1',
      investedAmount: 600,
      proportion: 0.6,
      profitShare: 120,
    })
  })

  it('should not create profit shares when current value is not above invested', () => {
    const summary = buildLiquidationSummary({
      startAmount: 500,
      currentAmount: 480,
      sourceReserves: [{ reserveId: 'r1', amount: 500 }],
    })

    expect(summary.profit).toBe(-20)
    expect(summary.distributions[0]?.profitShare).toBe(0)
  })
})
