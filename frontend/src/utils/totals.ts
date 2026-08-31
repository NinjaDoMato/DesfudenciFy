export interface ReserveValueInput {
  currentValue: number
  availableValue: number
  investedValue: number
}

export interface InvestmentSourceInput {
  reserveId?: string | null
  amount: number
}

export interface InvestmentValueInput {
  startAmount: number
  currentAmount: number
  sourceReserves?: readonly InvestmentSourceInput[]
}

export interface ReserveScreenTotals {
  saldoLivre: number
  totalMontinhos: number
  totalDisponivelReservas: number
  disponivelParaInvestimento: number
  totalAcumulado: number
  investedFromFree: number
  investedFromReserves: number
  totalInvestido: number
}

export interface InvestmentScreenTotals {
  count: number
  totalInvestido: number
  lucroRetido: number
}

export interface PatrimonioTotals {
  patrimonio: number
  financialCapital: number
  propertyAppraised: number
  vehicleFipe: number
  saldoLivre: number
  somatorioReservas: number
}

export interface InvestidoTotals {
  totalInvestido: number
  investedFromFree: number
  investedFromReserves: number
  lucroRetido: number
}

export function roundMoney(value: number): number {
  return Math.round(value * 100) / 100
}

function sumBy(values: readonly number[]): number {
  return roundMoney(values.reduce((sum, value) => sum + value, 0))
}

export function sumReservedAvailable(
  reserves: readonly Pick<ReserveValueInput, 'availableValue'>[],
): number {
  return sumBy(reserves.map((reserve) => reserve.availableValue))
}

export function sumReservedCurrent(
  reserves: readonly Pick<ReserveValueInput, 'currentValue'>[],
): number {
  return sumBy(reserves.map((reserve) => reserve.currentValue))
}

/**
 * Dashboard "Patrimônio acumulado":
 * capital financeiro (saldo livre atual + reservas atuais)
 * + soma dos valores avaliados dos imóveis
 * + soma dos valores FIPE dos veículos.
 * Breakdown uses Extrato/Reservas "Saldo livre" (available). Somatório das reservas is the
 * remainder of financial capital so the parts still add to the total
 * (saldo livre + somatório das reservas + imóveis + automóveis).
 */
export function computePatrimonioTotals(
  financialCapital: number,
  propertyAppraised: number,
  saldoLivre: number,
  vehicleFipe = 0,
): PatrimonioTotals {
  const financial = roundMoney(financialCapital)
  const properties = roundMoney(propertyAppraised)
  const vehicles = roundMoney(vehicleFipe)
  const free = roundMoney(saldoLivre)
  return {
    financialCapital: financial,
    propertyAppraised: properties,
    vehicleFipe: vehicles,
    patrimonio: roundMoney(financial + properties + vehicles),
    saldoLivre: free,
    somatorioReservas: roundMoney(financial - free),
  }
}

/**
 * Dashboard "Investido":
 * total = principal investido + lucro retido (= valor atual dos investimentos ativos).
 * Saldo livre / Reservas match computeReserveTotals investedFromFree / investedFromReserves.
 * Lucro retido = computeInvestmentTotals on active investments (current − start).
 */
export function computeInvestidoTotals(
  totalInvested: number,
  investedFromFree: number,
  investedFromReserves: number,
  lucroRetido: number,
): InvestidoTotals {
  const lucro = roundMoney(lucroRetido)
  return {
    totalInvestido: roundMoney(totalInvested + lucro),
    investedFromFree: roundMoney(investedFromFree),
    investedFromReserves: roundMoney(investedFromReserves),
    lucroRetido: lucro,
  }
}

/**
 * Reserves screen totals.
 * Saldo livre = available free balance (entries − invested with reserveId null).
 * Total nos montinhos = sum of reserve.currentValue.
 * Total disponível nas reservas = sum of reserve.availableValue.
 * Disponível para investimento = saldo livre + total disponível nas reservas.
 * Total acumulado = saldo livre + total disponível nas reservas (unlocked capital).
 * Total investido = invested from free + invested from reserves (ReserveInvestment.Amount).
 */
export function computeReserveTotals(
  freeAvailable: number,
  totalInvested: number,
  reserves: readonly ReserveValueInput[],
): ReserveScreenTotals {
  const saldoLivre = roundMoney(freeAvailable)
  const totalMontinhos = sumReservedCurrent(reserves)
  const totalDisponivelReservas = sumReservedAvailable(reserves)
  const investedFromReserves = sumBy(reserves.map((reserve) => reserve.investedValue))
  const investedFromFree = roundMoney(totalInvested - investedFromReserves)

  return {
    saldoLivre,
    totalMontinhos,
    totalDisponivelReservas,
    disponivelParaInvestimento: roundMoney(saldoLivre + totalDisponivelReservas),
    totalAcumulado: roundMoney(saldoLivre + totalDisponivelReservas),
    investedFromFree,
    investedFromReserves,
    totalInvestido: roundMoney(investedFromFree + investedFromReserves),
  }
}

/** Sum of source allocations without a reserve (saldo livre). */
export function investedAmountFromFree(
  investments: readonly InvestmentValueInput[],
): number {
  return sumBy(
    investments.flatMap((investment) =>
      (investment.sourceReserves ?? [])
        .filter((source) => !source.reserveId)
        .map((source) => source.amount),
    ),
  )
}

/**
 * Investments screen totals (active investments).
 * Total investido = sum of startAmount (principal allocated).
 * Lucro retido = sum(currentAmount) − sum(startAmount).
 */
export function computeInvestmentTotals(
  investments: readonly InvestmentValueInput[],
): InvestmentScreenTotals {
  const totalInvestido = sumBy(investments.map((item) => item.startAmount))
  const totalCurrent = sumBy(investments.map((item) => item.currentAmount))

  return {
    count: investments.length,
    totalInvestido,
    lucroRetido: roundMoney(totalCurrent - totalInvestido),
  }
}
