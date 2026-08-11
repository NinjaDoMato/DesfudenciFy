using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.Infrastructure.Auth;
using DesfudenciFy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegacyMigrator;

internal sealed record SeedAdminOptions(
    string Email,
    string Password,
    string FullName);

internal sealed class MigrationRunner
{
    private readonly string _legacyConnection;
    private readonly string _targetConnection;
    private readonly bool _wipeTarget;
    private readonly bool _dryRun;
    private readonly bool _skipConfirm;
    private readonly SeedAdminOptions _seedAdmin;

    public MigrationRunner(
        string legacyConnection,
        string targetConnection,
        bool wipeTarget,
        bool dryRun,
        bool skipConfirm,
        SeedAdminOptions seedAdmin)
    {
        _legacyConnection = legacyConnection;
        _targetConnection = targetConnection;
        _wipeTarget = wipeTarget;
        _dryRun = dryRun;
        _skipConfirm = skipConfirm;
        _seedAdmin = seedAdmin;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("=== DesfudenciFy Legacy Migrator ===");
        Console.WriteLine($"Modo: {(_dryRun ? "dry-run (somente leitura/contagem)" : "escrita")}");
        Console.WriteLine($"Wipe do PostgreSQL: {_wipeTarget}");
        Console.WriteLine($"Admin seed: {_seedAdmin.Email}");
        Console.WriteLine();

        Console.WriteLine("Lendo MySQL legado...");
        var reader = new LegacyReader(_legacyConnection);
        var snapshot = await reader.LoadAsync(cancellationToken);
        PrintSourceSummary(snapshot);

        foreach (var note in snapshot.Notes)
        {
            Console.WriteLine($"  • {note}");
        }

        Console.WriteLine();

        var appOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_targetConnection)
            .Options;
        await using (var migrateDb = new AppDbContext(appOptions))
        {
            await migrateDb.Database.MigrateAsync(cancellationToken);
        }

        var options = new DbContextOptionsBuilder<TargetDbContext>()
            .UseNpgsql(_targetConnection)
            .Options;
        await using var db = new TargetDbContext(options);

        if (!_wipeTarget)
        {
            var hasData = await TargetHasBusinessDataAsync(db, cancellationToken);
            if (hasData)
            {
                Console.Error.WriteLine(
                    "O PostgreSQL de destino já contém dados. Use --wipe-target (confirmação MIGRATE) ou limpe o banco manualmente.");
                return 2;
            }
        }
        else if (!_dryRun)
        {
            if (!ConfirmWipe())
            {
                Console.WriteLine("Operação cancelada.");
                return 3;
            }

            await WipeTargetAsync(db, cancellationToken);
            Console.WriteLine("PostgreSQL de destino limpo.");
        }

        var plan = BuildPlan(snapshot, _seedAdmin);
        PrintPlan(plan);

        if (_dryRun)
        {
            Console.WriteLine("Dry-run concluído — nenhum dado foi gravado.");
            return 0;
        }

        await WriteAsync(db, plan, cancellationToken);
        Console.WriteLine();
        Console.WriteLine("Migração concluída com sucesso.");
        return 0;
    }

    private bool ConfirmWipe()
    {
        if (_skipConfirm)
        {
            var envConfirm = Environment.GetEnvironmentVariable("LEGACY_MIGRATE_CONFIRM");
            if (string.Equals(envConfirm, "MIGRATE", StringComparison.Ordinal))
            {
                return true;
            }

            Console.Error.WriteLine(
                "--yes/--force exige LEGACY_MIGRATE_CONFIRM=MIGRATE no ambiente.");
            return false;
        }

        Console.WriteLine("ATENÇÃO: --wipe-target apaga TODOS os dados do PostgreSQL de destino.");
        Console.Write("Digite MIGRATE para confirmar: ");
        var input = Console.ReadLine();
        return string.Equals(input?.Trim(), "MIGRATE", StringComparison.Ordinal);
    }

    private static async Task<bool> TargetHasBusinessDataAsync(
        TargetDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Users.AnyAsync(cancellationToken)
               || await db.BankAccounts.AnyAsync(cancellationToken)
               || await db.InvestmentTypes.AnyAsync(cancellationToken)
               || await db.Reserves.AnyAsync(cancellationToken)
               || await db.Investments.AnyAsync(cancellationToken)
               || await db.Entries.AnyAsync(cancellationToken)
               || await db.FixedCosts.AnyAsync(cancellationToken)
               || await db.Purchases.AnyAsync(cancellationToken)
               || await db.IncomeSources.AnyAsync(cancellationToken)
               || await db.Properties.AnyAsync(cancellationToken);
    }

    private static async Task WipeTargetAsync(TargetDbContext db, CancellationToken cancellationToken)
    {
        // Ordem segura via TRUNCATE CASCADE (PostgreSQL).
        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "PropertyAmortizations",
                "CostPayments",
                "PurchaseInstallments",
                "ReserveInvestments",
                "Entries",
                "Investments",
                "FixedCosts",
                "Purchases",
                "IncomeSources",
                "Reserves",
                "Properties",
                "BankAccounts",
                "InvestmentTypes",
                "Users"
            RESTART IDENTITY CASCADE;
            """,
            cancellationToken);
    }

    private static MigrationPlan BuildPlan(LegacySnapshot snapshot, SeedAdminOptions seedAdmin)
    {
        var plan = new MigrationPlan();
        var droppedOwners = 0;
        var droppedSplits = 0;

        var accountIdsUsed = snapshot.Investments.Select(i => i.Account).Distinct().OrderBy(x => x).ToList();
        foreach (var account in accountIdsUsed)
        {
            var name = LegacyMappings.BankAccountName(account);
            plan.BankAccounts.Add(new BankAccount
            {
                Id = LegacyMappings.StableGuid("BankAccount:" + name),
                Name = name,
                Description = $"Migrado do enum AccountType legado ({account}).",
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                LastUpdate = null
            });
        }

        var typeIdsUsed = snapshot.Investments.Select(i => i.Type).Distinct().OrderBy(x => x).ToList();
        foreach (var type in typeIdsUsed)
        {
            var name = LegacyMappings.InvestmentTypeName(type);
            plan.InvestmentTypes.Add(new InvestmentType
            {
                Id = LegacyMappings.StableGuid("InvestmentType:" + name),
                Name = name,
                Description = $"Migrado do enum InvestmentType legado ({type}).",
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                LastUpdate = null
            });
        }

        // Tipos seed padrão (mesmo se não houver investimentos) — útil para a UI.
        foreach (var seedName in new[] { "LCI", "LCA", "CDB", "Tesouro SELIC", "FII" })
        {
            if (plan.InvestmentTypes.Any(t => t.Name == seedName))
            {
                continue;
            }

            plan.InvestmentTypes.Add(new InvestmentType
            {
                Id = LegacyMappings.StableGuid("InvestmentType:" + seedName),
                Name = seedName,
                Description = "Tipo padrão (seed do migrator).",
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                LastUpdate = null
            });
        }

        var bankIdByLegacyAccount = accountIdsUsed.ToDictionary(
            account => account,
            account => LegacyMappings.StableGuid("BankAccount:" + LegacyMappings.BankAccountName(account)));

        var typeIdByLegacyType = Enumerable.Range(0, 6).ToDictionary(
            type => type,
            type => LegacyMappings.StableGuid("InvestmentType:" + LegacyMappings.InvestmentTypeName(type)));

        var orderedUsers = snapshot.Users.OrderBy(u => u.DateCreated).ThenBy(u => u.Email).ToList();
        for (var i = 0; i < orderedUsers.Count; i++)
        {
            var user = orderedUsers[i];
            plan.Users.Add(new User
            {
                Id = user.Id,
                Email = LegacyMappings.Truncate(user.Email.Trim().ToLowerInvariant(), 256),
                PasswordHash = user.PasswordHash,
                FullName = LegacyMappings.Truncate(LegacyMappings.FullNameFromEmail(user.Email), 200),
                IsActive = true,
                Role = i == 0 ? UserRole.Admin : UserRole.User,
                LastLoginAt = user.LastLoginAt,
                DateCreated = user.DateCreated,
                LastUpdate = user.LastUpdate
            });
        }

        EnsureSeedAdmin(plan, seedAdmin);

        foreach (var reserve in snapshot.Reserves)
        {
            droppedOwners++;
            plan.Reserves.Add(new Reserve
            {
                Id = reserve.Id,
                Name = LegacyMappings.Truncate(reserve.Name, 100),
                Description = LegacyMappings.Truncate(reserve.Description, 500),
                Goal = reserve.Goal,
                DisplayColor = string.IsNullOrWhiteSpace(reserve.DisplayColor)
                    ? null
                    : LegacyMappings.Truncate(reserve.DisplayColor, 20),
                MonthlyGoal = reserve.MonthlyGoal,
                DateCreated = reserve.DateCreated,
                LastUpdate = reserve.LastUpdate
            });
            plan.DroppedOwnerNotes.Add(
                $"Reserve {reserve.Id}: Owner={LegacyMappings.OwnerLabel(reserve.Owner)} descartado.");
        }

        var reserveIds = plan.Reserves.Select(r => r.Id).ToHashSet();

        foreach (var entry in snapshot.Entries)
        {
            if (!reserveIds.Contains(entry.ReserveId))
            {
                plan.Skipped.Add($"Entry {entry.Id}: ReserveId {entry.ReserveId} inexistente — ignorada.");
                continue;
            }

            plan.Entries.Add(new Entry
            {
                Id = entry.Id,
                Amount = entry.Amount,
                Observation = LegacyMappings.Truncate(entry.Observation, 500),
                OccurredAt = entry.DateCreated,
                Destination = EntryDestination.Reserve,
                ReserveId = entry.ReserveId,
                DateCreated = entry.DateCreated,
                LastUpdate = entry.LastUpdate
            });
        }

        var investmentIds = new HashSet<Guid>();
        foreach (var investment in snapshot.Investments)
        {
            if (!bankIdByLegacyAccount.TryGetValue(investment.Account, out var bankAccountId))
            {
                bankAccountId = LegacyMappings.StableGuid(
                    "BankAccount:" + LegacyMappings.BankAccountName(investment.Account));
                if (plan.BankAccounts.All(b => b.Id != bankAccountId))
                {
                    plan.BankAccounts.Add(new BankAccount
                    {
                        Id = bankAccountId,
                        Name = LegacyMappings.BankAccountName(investment.Account),
                        Description = $"Migrado do enum AccountType legado ({investment.Account}).",
                        IsActive = true,
                        DateCreated = DateTime.UtcNow
                    });
                }
            }

            if (!typeIdByLegacyType.TryGetValue(investment.Type, out var investmentTypeId))
            {
                investmentTypeId = LegacyMappings.StableGuid(
                    "InvestmentType:" + LegacyMappings.InvestmentTypeName(investment.Type));
            }

            // Garante tipo no plano (ex.: valor de enum desconhecido).
            if (plan.InvestmentTypes.All(t => t.Id != investmentTypeId))
            {
                var name = LegacyMappings.InvestmentTypeName(investment.Type);
                plan.InvestmentTypes.Add(new InvestmentType
                {
                    Id = investmentTypeId,
                    Name = name,
                    Description = $"Migrado do enum InvestmentType legado ({investment.Type}).",
                    IsActive = true,
                    DateCreated = DateTime.UtcNow
                });
            }

            investmentIds.Add(investment.Id);
            plan.Investments.Add(new Investment
            {
                Id = investment.Id,
                Name = LegacyMappings.Truncate(investment.Name, 200),
                Rentability = LegacyMappings.Truncate(LegacyMappings.FormatRentability(investment.Rentability), 100),
                StartAmount = investment.StartAmount,
                CurrentAmount = investment.CurrentAmount,
                StartDate = investment.DateCreated,
                EndDate = investment.EndDate,
                BankAccountId = bankAccountId,
                InvestmentTypeId = investmentTypeId,
                Status = InvestmentStatus.Active,
                DateCreated = investment.DateCreated,
                LastUpdate = investment.LastUpdate
            });
        }

        var linkKeys = new HashSet<(Guid? ReserveId, Guid InvestmentId)>();
        foreach (var link in snapshot.ReserveInvestments)
        {
            if (!investmentIds.Contains(link.InvestmentId))
            {
                plan.Skipped.Add(
                    $"ReserveInvestment {link.ReserveId}/{link.InvestmentId}: investimento inexistente — ignorado.");
                continue;
            }

            if (!reserveIds.Contains(link.ReserveId))
            {
                plan.Skipped.Add(
                    $"ReserveInvestment {link.ReserveId}/{link.InvestmentId}: reserva inexistente — ignorado.");
                continue;
            }

            var key = ((Guid?)link.ReserveId, link.InvestmentId);
            if (!linkKeys.Add(key))
            {
                plan.Skipped.Add(
                    $"ReserveInvestment duplicado {link.ReserveId}/{link.InvestmentId} — mantido o primeiro.");
                continue;
            }

            plan.ReserveInvestments.Add(new ReserveInvestment
            {
                Id = LegacyMappings.EnsureLinkId(link.Id, link.ReserveId, link.InvestmentId),
                ReserveId = link.ReserveId,
                InvestmentId = link.InvestmentId,
                Amount = link.Amount,
                DateCreated = link.DateCreated,
                LastUpdate = link.LastUpdate
            });
        }

        var costIds = new HashSet<Guid>();
        foreach (var cost in snapshot.Costs)
        {
            droppedSplits++;
            Guid? reserveId = cost.ReserveId;
            if (reserveId is not null && !reserveIds.Contains(reserveId.Value))
            {
                plan.Skipped.Add($"Cost {cost.Id}: ReserveId {reserveId} inexistente — ReserveId anulado.");
                reserveId = null;
            }

            costIds.Add(cost.Id);
            plan.FixedCosts.Add(new FixedCost
            {
                Id = cost.Id,
                Name = LegacyMappings.Truncate(cost.Name, 200),
                Description = LegacyMappings.Truncate(
                    AppendSplitNote(cost.Description, cost.DanielPercentage, cost.CassiaPercentage),
                    500),
                Amount = cost.Amount,
                Recurrence = LegacyMappings.ToRecurrence(cost.Type),
                DueDate = null,
                ReserveId = reserveId,
                DateCreated = cost.DateCreated,
                LastUpdate = cost.LastUpdate
            });
        }

        foreach (var payment in snapshot.Payments)
        {
            if (!costIds.Contains(payment.CostId))
            {
                plan.Skipped.Add($"Payment {payment.Id}: CostId {payment.CostId} inexistente — ignorado.");
                continue;
            }

            plan.CostPayments.Add(new CostPayment
            {
                Id = payment.Id,
                PaidAmount = payment.PaidAmount,
                DatePaid = payment.DatePaid,
                FixedCostId = payment.CostId,
                EntryId = null,
                DateCreated = payment.DateCreated,
                LastUpdate = payment.LastUpdate
            });
        }

        foreach (var income in snapshot.IncomeSources)
        {
            droppedOwners++;
            plan.IncomeSources.Add(new IncomeSource
            {
                Id = income.Id,
                Name = LegacyMappings.Truncate(income.Name, 100),
                Amount = income.Amount,
                Description = LegacyMappings.Truncate(income.Description, 500),
                IsActive = income.IsActive,
                DateCreated = income.DateCreated,
                LastUpdate = income.LastUpdate
            });
            plan.DroppedOwnerNotes.Add(
                $"IncomeSource {income.Id}: Owner={LegacyMappings.OwnerLabel(income.Owner)} descartado.");
        }

        var purchaseIds = new HashSet<Guid>();
        foreach (var purchase in snapshot.Purchases)
        {
            droppedOwners++;
            purchaseIds.Add(purchase.Id);
            plan.Purchases.Add(new Purchase
            {
                Id = purchase.Id,
                Name = LegacyMappings.Truncate(purchase.Name, 200),
                ProductUrl = string.IsNullOrWhiteSpace(purchase.ProductUrl)
                    ? null
                    : LegacyMappings.Truncate(purchase.ProductUrl, 1000),
                DateCreated = purchase.DateCreated,
                LastUpdate = purchase.LastUpdate
            });
            plan.DroppedOwnerNotes.Add(
                $"Purchase {purchase.Id}: Owner={LegacyMappings.OwnerLabel(purchase.Owner)} descartado.");
        }

        foreach (var installment in snapshot.Installments)
        {
            if (!purchaseIds.Contains(installment.PurchaseId))
            {
                plan.Skipped.Add(
                    $"Installment {installment.Id}: PurchaseId {installment.PurchaseId} inexistente — ignorada.");
                continue;
            }

            plan.Installments.Add(new Installment
            {
                Id = installment.Id,
                PurchaseId = installment.PurchaseId,
                Amount = installment.Amount,
                InstallmentNumber = installment.InstallmentNumber,
                Paid = installment.Paid,
                DueDate = installment.DueDate,
                PaidDate = installment.PaidDate,
                PaymentUrl = string.IsNullOrWhiteSpace(installment.PaymentUrl)
                    ? null
                    : LegacyMappings.Truncate(installment.PaymentUrl, 1000),
                DateCreated = installment.DateCreated,
                LastUpdate = installment.LastUpdate
            });
        }

        plan.DroppedOwnerCount = droppedOwners;
        plan.DroppedSplitCount = droppedSplits;
        return plan;
    }

    private static string AppendSplitNote(string description, decimal daniel, decimal cassia)
    {
        var note = $"[legado split Daniel={daniel}% / Cassia={cassia}%]";
        if (string.IsNullOrWhiteSpace(description))
        {
            return note;
        }

        return $"{description.Trim()} {note}";
    }

    private static async Task WriteAsync(
        TargetDbContext db,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        db.Users.AddRange(plan.Users);
        db.BankAccounts.AddRange(plan.BankAccounts);
        db.InvestmentTypes.AddRange(plan.InvestmentTypes);
        db.Reserves.AddRange(plan.Reserves);
        await db.SaveChangesAsync(cancellationToken);

        db.Entries.AddRange(plan.Entries);
        db.Investments.AddRange(plan.Investments);
        db.FixedCosts.AddRange(plan.FixedCosts);
        db.IncomeSources.AddRange(plan.IncomeSources);
        db.Purchases.AddRange(plan.Purchases);
        await db.SaveChangesAsync(cancellationToken);

        db.ReserveInvestments.AddRange(plan.ReserveInvestments);
        db.CostPayments.AddRange(plan.CostPayments);
        db.Installments.AddRange(plan.Installments);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static void EnsureSeedAdmin(MigrationPlan plan, SeedAdminOptions seedAdmin)
    {
        var email = seedAdmin.Email.Trim().ToLowerInvariant();
        var hasher = new BcryptPasswordHasher();
        var passwordHash = hasher.Hash(seedAdmin.Password);
        var existing = plan.Users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Role = UserRole.Admin;
            existing.FullName = LegacyMappings.Truncate(seedAdmin.FullName, 200);
            existing.PasswordHash = passwordHash;
            existing.IsActive = true;
            plan.SeedAdminIncluded = true;
            plan.SeedAdminNote = $"Admin seed aplicado ao usuário legado existente ({email}).";
            return;
        }

        plan.Users.Add(new User
        {
            Id = LegacyMappings.StableGuid("SeedAdmin:" + email),
            Email = LegacyMappings.Truncate(email, 256),
            PasswordHash = passwordHash,
            FullName = LegacyMappings.Truncate(seedAdmin.FullName, 200),
            IsActive = true,
            Role = UserRole.Admin,
            LastLoginAt = null,
            DateCreated = DateTime.UtcNow,
            LastUpdate = null
        });
        plan.SeedAdminIncluded = true;
        plan.SeedAdminNote = $"Admin seed criado ({email}).";
    }

    private static void PrintSourceSummary(LegacySnapshot snapshot)
    {
        Console.WriteLine("Dados lidos do legado:");
        Console.WriteLine($"  Users              : {snapshot.Users.Count}");
        Console.WriteLine($"  Reserves           : {snapshot.Reserves.Count}");
        Console.WriteLine($"  Entries            : {snapshot.Entries.Count}");
        Console.WriteLine($"  Investments        : {snapshot.Investments.Count}");
        Console.WriteLine($"  ReserveInvestments : {snapshot.ReserveInvestments.Count}");
        Console.WriteLine($"  Costs              : {snapshot.Costs.Count}");
        Console.WriteLine($"  CostPayments       : {snapshot.Payments.Count}");
        Console.WriteLine($"  IncomeSources      : {snapshot.IncomeSources.Count}");
        Console.WriteLine($"  Purchases          : {snapshot.Purchases.Count}");
        Console.WriteLine($"  Installments       : {snapshot.Installments.Count}");
    }

    private static void PrintPlan(MigrationPlan plan)
    {
        Console.WriteLine("Plano de escrita (PostgreSQL):");
        Console.WriteLine($"  Users              : {plan.Users.Count}");
        if (plan.SeedAdminIncluded && !string.IsNullOrWhiteSpace(plan.SeedAdminNote))
        {
            Console.WriteLine($"    • {plan.SeedAdminNote}");
        }

        Console.WriteLine($"  BankAccounts       : {plan.BankAccounts.Count}");
        Console.WriteLine($"  InvestmentTypes    : {plan.InvestmentTypes.Count}");
        Console.WriteLine($"  Reserves           : {plan.Reserves.Count}");
        Console.WriteLine($"  Entries            : {plan.Entries.Count}");
        Console.WriteLine($"  Investments        : {plan.Investments.Count}");
        Console.WriteLine($"  ReserveInvestments : {plan.ReserveInvestments.Count}");
        Console.WriteLine($"  FixedCosts         : {plan.FixedCosts.Count}");
        Console.WriteLine($"  CostPayments       : {plan.CostPayments.Count}");
        Console.WriteLine($"  IncomeSources      : {plan.IncomeSources.Count}");
        Console.WriteLine($"  Purchases          : {plan.Purchases.Count}");
        Console.WriteLine($"  Installments       : {plan.Installments.Count}");
        Console.WriteLine($"  Properties         : 0 (módulo novo — sem legado)");
        Console.WriteLine($"  Owners descartados : {plan.DroppedOwnerCount}");
        Console.WriteLine($"  Splits descartados : {plan.DroppedSplitCount} (resumo anexado na Description do FixedCost)");

        if (plan.Skipped.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Itens ignorados ({plan.Skipped.Count}):");
            foreach (var item in plan.Skipped.Take(30))
            {
                Console.WriteLine($"  - {item}");
            }

            if (plan.Skipped.Count > 30)
            {
                Console.WriteLine($"  ... e mais {plan.Skipped.Count - 30}");
            }
        }
    }

    private sealed class MigrationPlan
    {
        public List<User> Users { get; } = [];
        public List<BankAccount> BankAccounts { get; } = [];
        public List<InvestmentType> InvestmentTypes { get; } = [];
        public List<Reserve> Reserves { get; } = [];
        public List<Entry> Entries { get; } = [];
        public List<Investment> Investments { get; } = [];
        public List<ReserveInvestment> ReserveInvestments { get; } = [];
        public List<FixedCost> FixedCosts { get; } = [];
        public List<CostPayment> CostPayments { get; } = [];
        public List<IncomeSource> IncomeSources { get; } = [];
        public List<Purchase> Purchases { get; } = [];
        public List<Installment> Installments { get; } = [];
        public List<string> Skipped { get; } = [];
        public List<string> DroppedOwnerNotes { get; } = [];
        public int DroppedOwnerCount { get; set; }
        public int DroppedSplitCount { get; set; }
        public bool SeedAdminIncluded { get; set; }
        public string? SeedAdminNote { get; set; }
    }
}
