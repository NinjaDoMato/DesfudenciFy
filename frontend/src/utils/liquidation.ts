import { roundMoney } from '@/utils/totals'

export const FREE_INVESTMENT_SOURCE = '__free__'

export interface LiquidationSourceInput {
  reserveId?: string | null
  amount: number
}

export interface LiquidationInvestmentInput {
  startAmount: number
  currentAmount: number
  sourceReserves: readonly LiquidationSourceInput[]
}

export interface LiquidationDistribution {
  sourceKey: string
  reserveId: string | null
  investedAmount: number
  proportion: number
  profitShare: number
}

export interface LiquidationSummary {
  invested: number
  finalValue: number
  profit: number
  distributions: LiquidationDistribution[]
}

/** Same preview the list modal uses before POST /investments/{id}/liquidate. */
export function buildLiquidationSummary(item: LiquidationInvestmentInput): LiquidationSummary {
  const invested = item.startAmount
  const finalValue = item.currentAmount
  const profit = roundMoney(finalValue - invested)
  const distributions = item.sourceReserves.map((source) => {
    const reserveId = source.reserveId ?? null
    const proportion = invested > 0 ? source.amount / invested : 0
    const profitShare = profit > 0 ? roundMoney(proportion * profit) : 0
    return {
      sourceKey: reserveId ?? FREE_INVESTMENT_SOURCE,
      reserveId,
      investedAmount: source.amount,
      proportion,
      profitShare,
    }
  })

  return { invested, finalValue, profit, distributions }
}
