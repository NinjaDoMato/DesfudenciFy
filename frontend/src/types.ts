export type UserRole = 'Admin' | 'User'
export type EntryDestination = 'FreeBalance' | 'Reserve'

export interface LoginResponse {
  token: string
  userId: string
  email: string
  fullName: string
  role: UserRole
}

export interface Reserve {
  id: string
  name: string
  description: string
  goal: number
  displayColor?: string | null
  monthlyGoal?: number | null
  currentValue: number
  investedValue: number
  availableValue: number
}

export interface Entry {
  id: string
  amount: number
  observation: string
  occurredAt: string
  destination: EntryDestination
  reserveId?: string | null
  reserveName?: string | null
}

export interface Investment {
  id: string
  name: string
  rentability: string
  startAmount: number
  currentAmount: number
  startDate: string
  endDate?: string | null
  bankAccountId: string
  bankAccountName: string
  investmentTypeId: string
  investmentTypeName: string
  status: string
  sourceReserves: { reserveId?: string | null; amount: number }[]
}

export interface Property {
  id: string
  name: string
  address: string
  photoUrl?: string | null
  isRented: boolean
  appraisedValue: number
  rentalAmount: number
  initialFinancingAmount: number
  installmentAmount: number
  remainingInstallments: number
  remainingBalance: number
  totalExpenses: number
  totalRentPaid: number
  propertyCost: number
  propertyReturn: number
  amortizations: {
    id: string
    amount: number
    installmentsAmortized: number
    paidAt: string
    observation?: string | null
    entryId?: string | null
  }[]
  expenses: {
    id: string
    amount: number
    expenseTypeId: string
    expenseTypeName: string
    observation: string
    occurredAt: string
    entryId?: string | null
  }[]
  rentPayments: {
    id: string
    amount: number
    observation?: string | null
    paidAt: string
    entryId: string
  }[]
}

export interface BankAccount {
  id: string
  name: string
  description?: string | null
  isActive: boolean
}

export interface InvestmentType {
  id: string
  name: string
  description?: string | null
  isActive: boolean
}

export interface IncomeType {
  id: string
  name: string
  description?: string | null
  isActive: boolean
}

export interface PropertyExpenseType {
  id: string
  name: string
  description?: string | null
  isActive: boolean
}

export interface UserDto {
  id: string
  email: string
  fullName: string
  isActive: boolean
  role: string
  lastLoginAt?: string | null
}

export interface CostPayment {
  id: string
  paidAmount: number
  datePaid: string
  entryId?: string | null
}

export interface FixedCost {
  id: string
  name: string
  description: string
  amount: number
  recurrence: string
  dueDate?: string | null
  reserveId?: string | null
  reserveName?: string | null
  isActive: boolean
  propertyId?: string | null
  payments: CostPayment[]
}

export interface IncomeSource {
  id: string
  name: string
  amount: number
  description: string
  isActive: boolean
  incomeTypeId: string
  incomeTypeName: string
  propertyId?: string | null
}

export interface Purchase {
  id: string
  name: string
  productUrl?: string | null
  installments: {
    id: string
    amount: number
    installmentNumber: number
    paid: boolean
    dueDate: string
    paidDate?: string | null
    paymentUrl?: string | null
  }[]
}

export interface DashboardTotals {
  totalAccumulated: number
  totalFreeBalance: number
  totalInvested: number
  totalIncome: number
  totalFixedCosts: number
  monthlyInvestmentGoal: number
  monthlyBalance: number
  totalPropertyRemainingBalance: number
}

export { formatMoney, parseMoneyInput } from '@/utils/money'

