using DesfudenciFy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesfudenciFy.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
    }
}

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

public class InvestmentTypeConfiguration : IEntityTypeConfiguration<InvestmentType>
{
    public void Configure(EntityTypeBuilder<InvestmentType> builder)
    {
        builder.ToTable("InvestmentTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

public class IncomeTypeConfiguration : IEntityTypeConfiguration<IncomeType>
{
    public void Configure(EntityTypeBuilder<IncomeType> builder)
    {
        builder.ToTable("IncomeTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

public class PropertyExpenseTypeConfiguration : IEntityTypeConfiguration<PropertyExpenseType>
{
    public void Configure(EntityTypeBuilder<PropertyExpenseType> builder)
    {
        builder.ToTable("PropertyExpenseTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

public class ReserveConfiguration : IEntityTypeConfiguration<Reserve>
{
    public void Configure(EntityTypeBuilder<Reserve> builder)
    {
        builder.ToTable("Reserves");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Goal).HasPrecision(18, 2);
        builder.Property(x => x.MonthlyGoal).HasPrecision(18, 2);
        builder.Property(x => x.DisplayColor).HasMaxLength(20);
    }
}

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("Entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Observation).HasMaxLength(500);
        builder.HasOne(x => x.Reserve)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.ReserveId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("Investments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Rentability).HasMaxLength(100);
        builder.Property(x => x.StartAmount).HasPrecision(18, 2);
        builder.Property(x => x.CurrentAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.BankAccount)
            .WithMany(x => x.Investments)
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InvestmentType)
            .WithMany(x => x.Investments)
            .HasForeignKey(x => x.InvestmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReserveInvestmentConfiguration : IEntityTypeConfiguration<ReserveInvestment>
{
    public void Configure(EntityTypeBuilder<ReserveInvestment> builder)
    {
        builder.ToTable("ReserveInvestments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ReserveId, x.InvestmentId }).IsUnique();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.Reserve)
            .WithMany(x => x.LinkedInvestments)
            .HasForeignKey(x => x.ReserveId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
        builder.HasOne(x => x.Investment)
            .WithMany(x => x.SourceReserves)
            .HasForeignKey(x => x.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PhotoPath).HasMaxLength(1000);
        builder.Property(x => x.AppraisedValue).HasPrecision(18, 2);
        builder.Property(x => x.RentalAmount).HasPrecision(18, 2);
        builder.Property(x => x.InitialFinancingAmount).HasPrecision(18, 2);
        builder.Property(x => x.InstallmentAmount).HasPrecision(18, 2);
        builder.Property(x => x.RemainingBalance).HasPrecision(18, 2);
        builder.Property(x => x.SaleAmount).HasPrecision(18, 2);
    }
}

public class PropertyAmortizationConfiguration : IEntityTypeConfiguration<PropertyAmortization>
{
    public void Configure(EntityTypeBuilder<PropertyAmortization> builder)
    {
        builder.ToTable("PropertyAmortizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Observation).HasMaxLength(500);
        builder.HasOne(x => x.Property)
            .WithMany(x => x.Amortizations)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PropertyExpenseConfiguration : IEntityTypeConfiguration<PropertyExpense>
{
    public void Configure(EntityTypeBuilder<PropertyExpense> builder)
    {
        builder.ToTable("PropertyExpenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Observation).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Property)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ExpenseType)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PropertyRentPaymentConfiguration : IEntityTypeConfiguration<PropertyRentPayment>
{
    public void Configure(EntityTypeBuilder<PropertyRentPayment> builder)
    {
        builder.ToTable("PropertyRentPayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Observation).HasMaxLength(500);
        builder.HasOne(x => x.Property)
            .WithMany(x => x.RentPayments)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FixedCostConfiguration : IEntityTypeConfiguration<FixedCost>
{
    public void Configure(EntityTypeBuilder<FixedCost> builder)
    {
        builder.ToTable("FixedCosts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.Reserve)
            .WithMany(x => x.FixedCosts)
            .HasForeignKey(x => x.ReserveId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.PropertyId);
    }
}

public class CostPaymentConfiguration : IEntityTypeConfiguration<CostPayment>
{
    public void Configure(EntityTypeBuilder<CostPayment> builder)
    {
        builder.ToTable("CostPayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.FixedCost)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.FixedCostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class IncomeSourceConfiguration : IEntityTypeConfiguration<IncomeSource>
{
    public void Configure(EntityTypeBuilder<IncomeSource> builder)
    {
        builder.ToTable("IncomeSources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.IncomeType)
            .WithMany(x => x.IncomeSources)
            .HasForeignKey(x => x.IncomeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.PropertyId);
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProductUrl).HasMaxLength(1000);
        builder.HasOne(x => x.Reserve)
            .WithMany(x => x.Purchases)
            .HasForeignKey(x => x.ReserveId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("PurchaseInstallments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.PaymentUrl).HasMaxLength(1000);
        builder.HasOne(x => x.Purchase)
            .WithMany(x => x.Installments)
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
