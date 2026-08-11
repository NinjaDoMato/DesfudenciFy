using LegacyMigrator;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
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

string target;
string targetSource;
string targetEnvironment;
try
{
    targetEnvironment = ResolveTargetEnvironment(configuration);
    (target, targetSource) = ResolveTargetConnection(configuration, targetEnvironment);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    PrintHelp();
    return 1;
}

if (string.IsNullOrWhiteSpace(legacy) || string.IsNullOrWhiteSpace(target))
{
    Console.Error.WriteLine("Connection strings obrigatórias ausentes.");
    Console.Error.WriteLine("Defina LegacyMySql e Targets:dev / Targets:prod em appsettings.json, ou:");
    Console.Error.WriteLine("  LEGACY_MYSQL_CONNECTION");
    Console.Error.WriteLine("  TARGET_POSTGRES_CONNECTION");
    Console.Error.WriteLine();
    PrintHelp();
    return 1;
}

var seedAdmin = new SeedAdminOptions(
    Email: FirstNonEmpty(configuration["Seed:AdminEmail"], "admin@desfudencify.local")!,
    Password: FirstNonEmpty(configuration["Seed:AdminPassword"], "Admin@12345")!,
    FullName: FirstNonEmpty(configuration["Seed:AdminFullName"], "Administrator")!);

Console.WriteLine($"Ambiente destino: {targetEnvironment} ({targetSource})");

try
{
    var runner = new MigrationRunner(legacy, target, wipeTarget, dryRun, skipConfirm, seedAdmin);
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

static string ResolveTargetEnvironment(IConfiguration configuration)
{
    var raw = FirstNonEmpty(
        configuration["TargetEnvironment"],
        configuration["Target"],
        Environment.GetEnvironmentVariable("TARGET_ENVIRONMENT"),
        "dev")!;

    var normalized = raw.Trim().ToLowerInvariant();
    if (normalized is "dev" or "development")
        return "dev";
    if (normalized is "prod" or "production")
        return "prod";

    throw new InvalidOperationException(
        $"Ambiente destino inválido: '{raw}'. Use 'dev' ou 'prod' (--target / TargetEnvironment).");
}

static (string? Connection, string Source) ResolveTargetConnection(
    IConfiguration configuration,
    string targetEnvironment)
{
    // 1) Override explícito (CLI --target-postgres, env, ConnectionStrings)
    var explicitOverride = FirstNonEmpty(
        AsConnectionString(configuration["TargetPostgres"]),
        AsConnectionString(configuration["ConnectionStrings:TargetPostgres"]),
        AsConnectionString(configuration["ConnectionStrings:DefaultConnection"]),
        AsConnectionString(Environment.GetEnvironmentVariable("TARGET_POSTGRES_CONNECTION")));

    if (explicitOverride is not null)
        return (explicitOverride, "override explícito");

    // 2) Targets:{dev|prod}
    var fromTargets = FirstNonEmpty(
        configuration[$"Targets:{targetEnvironment}"],
        configuration[$"TargetPostgres:{targetEnvironment}"]);

    if (!string.IsNullOrWhiteSpace(fromTargets))
        return (fromTargets, $"Targets:{targetEnvironment}");

    return (null, "nenhuma");
}

static string? AsConnectionString(string? value) =>
    LooksLikeConnectionString(value) ? value : null;

static bool LooksLikeConnectionString(string? value) =>
    !string.IsNullOrWhiteSpace(value)
    && (value.Contains('=', StringComparison.Ordinal) || value.Contains(';', StringComparison.Ordinal));

/// <summary>
/// Configuration.CommandLine espera --key=value; converte flags CLI para chaves conhecidas.
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

        if ((string.Equals(arg, "--target", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "--env", StringComparison.OrdinalIgnoreCase))
            && i + 1 < rawArgs.Length)
        {
            normalized.Add($"TargetEnvironment={rawArgs[++i]}");
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
          --target dev|prod           Ambiente PostgreSQL de destino (padrão: appsettings / dev)
          --env dev|prod              Alias de --target
          --legacy-mysql "<conn>"     Connection string MySQL legado
          --target-postgres "<conn>"  Connection string PostgreSQL (sobrescreve --target)
          --wipe-target               Apaga dados do destino antes de importar (pede MIGRATE)
          --dry-run                   Lê e mapeia, sem gravar
          --yes / --force             Confirma wipe sem prompt (exige LEGACY_MIGRATE_CONFIRM=MIGRATE)
          --help                      Esta ajuda

        Variáveis de ambiente:
          LEGACY_MYSQL_CONNECTION
          TARGET_POSTGRES_CONNECTION
          TARGET_ENVIRONMENT=dev|prod
          LEGACY_MIGRATE_CONFIRM=MIGRATE   (com --yes/--force)

        Ou edite Targets:dev / Targets:prod em appsettings.json ao lado do executável.
        """);
}
