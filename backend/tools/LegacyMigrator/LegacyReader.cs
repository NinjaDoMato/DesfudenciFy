using MySqlConnector;

namespace LegacyMigrator;

internal sealed class LegacyReader
{
    private readonly string _connectionString;

    public LegacyReader(string connectionString) => _connectionString = connectionString;

    public async Task<LegacySnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var snapshot = new LegacySnapshot();

        if (await TableExistsAsync(connection, "Users", cancellationToken))
        {
            snapshot.Users.AddRange(await ReadUsersAsync(connection, cancellationToken));
        }
        else
        {
            snapshot.Notes.Add("Tabela Users ausente no MySQL — nenhum usuário será migrado.");
        }

        snapshot.Reserves.AddRange(await ReadReservesAsync(connection, cancellationToken));
        snapshot.Entries.AddRange(await ReadEntriesAsync(connection, cancellationToken));
        snapshot.Investments.AddRange(await ReadInvestmentsAsync(connection, cancellationToken));

        var mapTable = await ResolveReserveMapTableAsync(connection, cancellationToken);
        if (mapTable is null)
        {
            snapshot.Notes.Add("Tabela de vínculo reserva↔investimento não encontrada (ReserveInvestmentsMaps / ReserveInvestmentMaps).");
        }
        else
        {
            snapshot.ReserveInvestments.AddRange(await ReadReserveInvestmentsAsync(connection, mapTable, cancellationToken));
            snapshot.Notes.Add($"Vínculos lidos de `{mapTable}`.");
        }

        if (await TableExistsAsync(connection, "Costs", cancellationToken))
        {
            snapshot.Costs.AddRange(await ReadCostsAsync(connection, cancellationToken));
        }

        if (await TableExistsAsync(connection, "CostPayments", cancellationToken))
        {
            snapshot.Payments.AddRange(await ReadPaymentsAsync(connection, cancellationToken));
        }

        if (await TableExistsAsync(connection, "IncomeSources", cancellationToken))
        {
            snapshot.IncomeSources.AddRange(await ReadIncomeSourcesAsync(connection, cancellationToken));
        }

        if (await TableExistsAsync(connection, "Purchases", cancellationToken))
        {
            snapshot.Purchases.AddRange(await ReadPurchasesAsync(connection, cancellationToken));
        }

        if (await TableExistsAsync(connection, "PurchaseInstallments", cancellationToken))
        {
            snapshot.Installments.AddRange(await ReadInstallmentsAsync(connection, cancellationToken));
        }

        if (await TableExistsAsync(connection, "InvestmentHistory", cancellationToken))
        {
            snapshot.Notes.Add("InvestmentHistory existe no legado, mas não há equivalente em DesfudenciFy_2 — ignorado.");
        }

        return snapshot;
    }

    private static async Task<bool> TableExistsAsync(
        MySqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = @name;
            """;
        cmd.Parameters.AddWithValue("@name", tableName);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = @table
              AND column_name = @column;
            """;
        cmd.Parameters.AddWithValue("@table", tableName);
        cmd.Parameters.AddWithValue("@column", columnName);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<string?> ResolveReserveMapTableAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(connection, "ReserveInvestmentsMaps", cancellationToken))
        {
            return "ReserveInvestmentsMaps";
        }

        if (await TableExistsAsync(connection, "ReserveInvestmentMaps", cancellationToken))
        {
            return "ReserveInvestmentMaps";
        }

        return null;
    }

    private static async Task<List<LegacyUserRow>> ReadUsersAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Email, PasswordHash, LastLoginAt, DateCreated, LastUpdate
            FROM Users;
            """;

        var rows = new List<LegacyUserRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyUserRow(
                reader.GetGuid("Id"),
                reader.GetString("Email"),
                reader.GetString("PasswordHash"),
                reader.IsDBNull(reader.GetOrdinal("LastLoginAt")) ? null : ToUtc(reader.GetDateTime("LastLoginAt")),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private async Task<List<LegacyReserveRow>> ReadReservesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        var hasDisplayColor = await ColumnExistsAsync(connection, "Reserves", "DisplayColor", cancellationToken);
        var hasMonthlyGoal = await ColumnExistsAsync(connection, "Reserves", "MonthlyGoal", cancellationToken);

        var displayColorSelect = hasDisplayColor ? "DisplayColor" : "NULL AS DisplayColor";
        var monthlyGoalSelect = hasMonthlyGoal ? "MonthlyGoal" : "NULL AS MonthlyGoal";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT Id, Name, Description, Owner, Goal, {displayColorSelect}, {monthlyGoalSelect}, DateCreated, LastUpdate
            FROM Reserves;
            """;

        var rows = new List<LegacyReserveRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyReserveRow(
                reader.GetGuid("Id"),
                reader.GetString("Name"),
                reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString("Description"),
                reader.GetInt32("Owner"),
                reader.GetDecimal("Goal"),
                reader.IsDBNull(reader.GetOrdinal("DisplayColor")) ? null : reader.GetString("DisplayColor"),
                reader.IsDBNull(reader.GetOrdinal("MonthlyGoal")) ? null : reader.GetDecimal("MonthlyGoal"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyEntryRow>> ReadEntriesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Amount, Observation, ReserveId, DateCreated, LastUpdate
            FROM Entries;
            """;

        var rows = new List<LegacyEntryRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyEntryRow(
                reader.GetGuid("Id"),
                reader.GetDecimal("Amount"),
                reader.IsDBNull(reader.GetOrdinal("Observation")) ? string.Empty : reader.GetString("Observation"),
                reader.GetGuid("ReserveId"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyInvestmentRow>> ReadInvestmentsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Name, StartAmount, CurrentAmount, Rentability, Type, EndDate, Account, DateCreated, LastUpdate
            FROM Investments;
            """;

        var rows = new List<LegacyInvestmentRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyInvestmentRow(
                reader.GetGuid("Id"),
                reader.GetString("Name"),
                reader.GetDecimal("StartAmount"),
                reader.GetDecimal("CurrentAmount"),
                reader.GetDecimal("Rentability"),
                reader.GetInt32("Type"),
                ToUtc(reader.GetDateTime("EndDate")),
                reader.GetInt32("Account"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyReserveInvestmentRow>> ReadReserveInvestmentsAsync(
        MySqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT Id, ReserveId, InvestmentId, Amount, DateCreated, LastUpdate
            FROM `{tableName}`;
            """;

        var rows = new List<LegacyReserveInvestmentRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyReserveInvestmentRow(
                reader.IsDBNull(reader.GetOrdinal("Id")) ? Guid.Empty : reader.GetGuid("Id"),
                reader.GetGuid("ReserveId"),
                reader.GetGuid("InvestmentId"),
                reader.GetDecimal("Amount"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private async Task<List<LegacyCostRow>> ReadCostsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        var hasReserveId = await ColumnExistsAsync(connection, "Costs", "ReserveId", cancellationToken);
        var reserveSelect = hasReserveId ? "ReserveId" : "NULL AS ReserveId";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT Id, Amount, Type, Name, Description, DanielPercentage, CassiaPercentage, {reserveSelect}, DateCreated, LastUpdate
            FROM Costs;
            """;

        var rows = new List<LegacyCostRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyCostRow(
                reader.GetGuid("Id"),
                reader.GetDecimal("Amount"),
                reader.GetInt32("Type"),
                reader.GetString("Name"),
                reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString("Description"),
                reader.GetDecimal("DanielPercentage"),
                reader.GetDecimal("CassiaPercentage"),
                reader.IsDBNull(reader.GetOrdinal("ReserveId")) ? null : reader.GetGuid("ReserveId"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyPaymentRow>> ReadPaymentsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, PaidAmount, DatePaid, CostId, DateCreated, LastUpdate
            FROM CostPayments;
            """;

        var rows = new List<LegacyPaymentRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyPaymentRow(
                reader.GetGuid("Id"),
                reader.GetDecimal("PaidAmount"),
                ToUtc(reader.GetDateTime("DatePaid")),
                reader.GetGuid("CostId"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyIncomeSourceRow>> ReadIncomeSourcesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Name, Amount, Owner, Description, IsActive, DateCreated, LastUpdate
            FROM IncomeSources;
            """;

        var rows = new List<LegacyIncomeSourceRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyIncomeSourceRow(
                reader.GetGuid("Id"),
                reader.GetString("Name"),
                reader.GetDecimal("Amount"),
                reader.GetInt32("Owner"),
                reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString("Description"),
                reader.GetBoolean("IsActive"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyPurchaseRow>> ReadPurchasesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Name, ProductUrl, Owner, DateCreated, LastUpdate
            FROM Purchases;
            """;

        var rows = new List<LegacyPurchaseRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyPurchaseRow(
                reader.GetGuid("Id"),
                reader.GetString("Name"),
                reader.IsDBNull(reader.GetOrdinal("ProductUrl")) ? string.Empty : reader.GetString("ProductUrl"),
                reader.GetInt32("Owner"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static async Task<List<LegacyInstallmentRow>> ReadInstallmentsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, PurchaseId, Amount, InstallmentNumber, Paid, DueDate, PaidDate, PaymentUrl, DateCreated, LastUpdate
            FROM PurchaseInstallments;
            """;

        var rows = new List<LegacyInstallmentRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyInstallmentRow(
                reader.GetGuid("Id"),
                reader.GetGuid("PurchaseId"),
                reader.GetDecimal("Amount"),
                reader.GetInt32("InstallmentNumber"),
                reader.GetBoolean("Paid"),
                ToUtc(reader.GetDateTime("DueDate")),
                reader.IsDBNull(reader.GetOrdinal("PaidDate")) ? null : ToUtc(reader.GetDateTime("PaidDate")),
                reader.IsDBNull(reader.GetOrdinal("PaymentUrl")) ? string.Empty : reader.GetString("PaymentUrl"),
                ToUtc(reader.GetDateTime("DateCreated")),
                reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : ToUtc(reader.GetDateTime("LastUpdate"))));
        }

        return rows;
    }

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
