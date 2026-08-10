using LegacyMigrator;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(NormalizeArgs(args))
    .Build();

var wipeTarget = HasFlag(args, "--wipe-target");
var dryRun = HasFlag(args, "--dry-run");
var skipConfirm = HasFlag(args, "--yes") || HasFlag(args, "--force");
var showHelp = HasFlag(args, "--help") || HasFlag(args, "-h");

if (showHelp)
{
    PrintHelp();
    return 0;
}

var legacy = FirstNonEmpty(
    configuration["LegacyMySql"],
    configuration["ConnectionStrings:LegacyMySql"],
    Environment.GetEnvironmentVariable("LEGACY_MYSQL_CONNECTION"));

var target = FirstNonEmpty(
    configuration["TargetPostgres"],
    configuration["ConnectionStrings:TargetPostgres"],
    configuration["ConnectionStrings:DefaultConnection"],
    Environment.GetEnvironmentVariable("TARGET_POSTGRES_CONNECTION"));

if (string.IsNullOrWhiteSpace(legacy) || string.IsNullOrWhiteSpace(target))
{
    Console.Error.WriteLine("Connection strings obrigatórias ausentes.");
    Console.Error.WriteLine("Defina LegacyMySql / TargetPostgres em appsettings.json, ou:");
    Console.Error.WriteLine("  LEGACY_MYSQL_CONNECTION");
    Console.Error.WriteLine("  TARGET_POSTGRES_CONNECTION");
    Console.Error.WriteLine();
    PrintHelp();
    return 1;
}

try
{
    var runner = new MigrationRunner(legacy, target, wipeTarget, dryRun, skipConfirm);
    return await runner.RunAsync(CancellationToken.None);
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Falha na migração:");
    Console.Error.WriteLine(ex.ToString());
    return 10;
}

static bool HasFlag(string[] rawArgs, string flag) =>
    rawArgs.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

static string? FirstNonEmpty(params string?[] values) =>
    values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

/// <summary>
/// Configuration.CommandLine espera --key=value; converte --legacy-mysql value para chave conhecida.
/// </summary>
static string[] NormalizeArgs(string[] rawArgs)
{
    var normalized = new List<string>();
    for (var i = 0; i < rawArgs.Length; i++)
    {
        var arg = rawArgs[i];
        if (string.Equals(arg, "--legacy-mysql", StringComparison.OrdinalIgnoreCase)
            && i + 1 < rawArgs.Length)
        {
            normalized.Add($"LegacyMySql={rawArgs[++i]}");
            continue;
        }

        if (string.Equals(arg, "--target-postgres", StringComparison.OrdinalIgnoreCase)
            && i + 1 < rawArgs.Length)
        {
            normalized.Add($"TargetPostgres={rawArgs[++i]}");
            continue;
        }

        if (arg.StartsWith("--wipe-target", StringComparison.OrdinalIgnoreCase)
            || arg.StartsWith("--dry-run", StringComparison.OrdinalIgnoreCase)
            || arg.StartsWith("--yes", StringComparison.OrdinalIgnoreCase)
            || arg.StartsWith("--force", StringComparison.OrdinalIgnoreCase)
            || arg.StartsWith("--help", StringComparison.OrdinalIgnoreCase)
            || arg is "-h")
        {
            continue;
        }

        normalized.Add(arg);
    }

    return normalized.ToArray();
}

static void PrintHelp()
{
    Console.WriteLine("""
        LegacyMigrator — importa dados do Desfudencify_1 (MySQL) para DesfudenciFy_2 (PostgreSQL).

        Uso:
          dotnet run --project backend/tools/LegacyMigrator -- [opções]

        Opções:
          --legacy-mysql "<conn>"     Connection string MySQL legado
          --target-postgres "<conn>"  Connection string PostgreSQL destino
          --wipe-target               Apaga dados do destino antes de importar (pede MIGRATE)
          --dry-run                   Lê e mapeia, sem gravar
          --yes / --force             Confirma wipe sem prompt (exige LEGACY_MIGRATE_CONFIRM=MIGRATE)
          --help                      Esta ajuda

        Variáveis de ambiente:
          LEGACY_MYSQL_CONNECTION
          TARGET_POSTGRES_CONNECTION
          LEGACY_MIGRATE_CONFIRM=MIGRATE   (com --yes/--force)

        Ou edite appsettings.json ao lado do executável.
        """);
}
