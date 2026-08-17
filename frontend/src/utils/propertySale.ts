import { roundMoney } from '@/utils/totals'

export interface PropertySalePreviewInput {
  saleAmount: number
  propertyCost: number
  totalRentPaid: number
  remainingBalance: number
  isRented: boolean
}

export interface PropertySalePreview {
  saleAmount: number
  profit: number
  remainingBalanceAfter: number
  isRented: boolean
}

/** Same return formula as the property "Retorno" KPI, with sale amount as realization price. */
export function computePropertySalePreview(input: PropertySalePreviewInput): PropertySalePreview {
  const saleAmount = roundMoney(input.saleAmount)
  const profit = roundMoney(saleAmount - input.propertyCost + input.totalRentPaid)
  return {
    saleAmount,
    profit,
    remainingBalanceAfter: 0,
    isRented: input.isRented,
  }
}
