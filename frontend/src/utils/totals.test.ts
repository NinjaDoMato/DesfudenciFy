import { describe, expect, it } from 'vitest'
import {
  computeInvestidoTotals,
  computeInvestmentTotals,
  computePatrimonioTotals,
  computeReserveTotals,
  investedAmountFromFree,
  sumReservedAvailable,
} from './totals'

describe('computeReserveTotals', () => {
  it('should add available free and reserved balances as total acumulado', () => {
    const totals = computeReserveTotals(1200, 800, [
      { availableValue: 300, investedValue: 200 },
      { availableValue: 150, investedValue: 50 },
    ])

    expect(totals.saldoLivre).toBe(1200)
    expect(totals.saldoReservado).toBe(450)
    expect(totals.totalAcumulado).toBe(1650)
  })

  it('should split total invested between free balance and reserves', () => {
    const totals = computeReserveTotals(500, 800, [
      { availableValue: 100, investedValue: 200 },
      { availableValue: 50, investedValue: 150 },
    ])

    expect(totals.investedFromReserves).toBe(350)
    expect(totals.investedFromFree).toBe(450)
    expect(totals.totalInvestido).toBe(800)
  })

  it('should return zeros when there are no reserves and nothing invested', () => {
    const totals = computeReserveTotals(0, 0, [])

    expect(totals).toEqual({
      saldoLivre: 0,
      saldoReservado: 0,
      totalAcumulado: 0,
      investedFromFree: 0,
      investedFromReserves: 0,
      totalInvestido: 0,
    })
  })
})

describe('investedAmountFromFree', () => {
  it('should sum source allocations without a reserve id', () => {
    const amount = investedAmountFromFree([
      {
        startAmount: 300,
        currentAmount: 310,
        sourceReserves: [
          { reserveId: null, amount: 100 },
          { reserveId: 'r1', amount: 200 },
        ],
      },
      {
        startAmount: 50,
        currentAmount: 50,
        sourceReserves: [{ reserveId: undefined, amount: 50 }],
      },
    ])

    expect(amount).toBe(150)
  })
})

describe('computeInvestmentTotals', () => {
  it('should count investments and sum start amounts as total investido', () => {
    const totals = computeInvestmentTotals([
      { startAmount: 1000, currentAmount: 1100 },
      { startAmount: 400, currentAmount: 380 },
    ])

    expect(totals.count).toBe(2)
    expect(totals.totalInvestido).toBe(1400)
  })

  it('should compute lucro retido as current total minus invested total', () => {
    const totals = computeInvestmentTotals([
      { startAmount: 1000, currentAmount: 1250.5 },
      { startAmount: 500, currentAmount: 500 },
      { startAmount: 200, currentAmount: 150 },
    ])

    expect(totals.lucroRetido).toBe(200.5)
  })

  it('should return negative lucro retido when current value is below invested', () => {
    const totals = computeInvestmentTotals([
      { startAmount: 1000, currentAmount: 900 },
    ])

    expect(totals.lucroRetido).toBe(-100)
  })

  it('should return zeros for an empty investment list', () => {
    expect(computeInvestmentTotals([])).toEqual({
      count: 0,
      totalInvestido: 0,
      lucroRetido: 0,
    })
  })
})

describe('computePatrimonioTotals', () => {
  it('should add financial capital and property appraised values', () => {
    const totals = computePatrimonioTotals(1400, 450_000, 1000)

    expect(totals.financialCapital).toBe(1400)
    expect(totals.propertyAppraised).toBe(450_000)
    expect(totals.patrimonio).toBe(451_400)
  })

  it('should keep patrimônio equal to financial capital when there are no properties', () => {
    expect(computePatrimonioTotals(1650.4, 0, 1650.4)).toEqual({
      financialCapital: 1650.4,
      propertyAppraised: 0,
      patrimonio: 1650.4,
      saldoLivre: 1650.4,
      somatorioReservas: 0,
    })
  })

  it('should split financial capital into saldo livre and somatório das reservas', () => {
    const totals = computePatrimonioTotals(1400, 450_000, 1000)

    expect(totals.saldoLivre).toBe(1000)
    expect(totals.somatorioReservas).toBe(400)
    expect(totals.saldoLivre + totals.somatorioReservas + totals.propertyAppraised).toBe(totals.patrimonio)
  })
})

describe('computeInvestidoTotals', () => {
  it('should keep invested split and lucro retido from the same sources as reservas and investimentos', () => {
    const totals = computeInvestidoTotals(1000, 300, 700, 100)

    expect(totals.totalInvestido).toBe(1100)
    expect(totals.investedFromFree).toBe(300)
    expect(totals.investedFromReserves).toBe(700)
    expect(totals.lucroRetido).toBe(100)
  })

  it('should keep negative lucro retido', () => {
    const totals = computeInvestidoTotals(500, 500, 0, -25.5)
    expect(totals.lucroRetido).toBe(-25.5)
    expect(totals.totalInvestido).toBe(474.5)
  })
})

describe('sumReservedAvailable', () => {
  it('should sum availableValue of each reserve', () => {
    expect(sumReservedAvailable([
      { availableValue: 300 },
      { availableValue: 150.25 },
    ])).toBe(450.25)
  })
})
