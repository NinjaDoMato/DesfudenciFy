/** Formats a number as Brazilian Real currency, e.g. "R$ 100.000,00". */
export function formatMoney(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

/**
 * Parses a BRL-masked string into a number.
 * Digits are treated as cents (typing 10000000 => 100000.00).
 */
export function parseMoneyInput(value: string, allowNegative = false): number {
  const negative = allowNegative && /-|−/.test(value)
  const digits = value.replace(/\D/g, '')
  if (!digits) {
    return 0
  }

  const amount = Number(digits) / 100
  return negative ? -amount : amount
}
