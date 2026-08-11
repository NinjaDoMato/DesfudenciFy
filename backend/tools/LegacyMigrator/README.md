# LegacyMigrator

Ferramenta de console (.NET 8) para migrar dados do **Desfudencify_1** (MySQL / Pomelo) para o **DesfudenciFy_2** (PostgreSQL / EF Core).

Isolada do app principal: lê o legado via **MySqlConnector** e grava no destino com um `DbContext` próprio (mesmas configurações de entidade, sem sobrescrever `DateCreated`).

## Pré-requisitos

- .NET 8 SDK
- MySQL do app legado acessível
- PostgreSQL do DesfudenciFy_2 acessível (o migrator aplica `Database.Migrate()` se necessário)

## Configuração

Connection strings (qualquer uma das fontes):

| Fonte | Chaves |
|--------|--------|
| `appsettings.json` | `LegacyMySql`, `Targets:dev`, `Targets:prod`, `TargetEnvironment` |
| `appsettings.Local.json` | Sobrescreve o template — **não versionar** (senha local) |
| Variáveis de ambiente | `LEGACY_MYSQL_CONNECTION`, `TARGET_POSTGRES_CONNECTION`, `TARGET_ENVIRONMENT` |
| CLI | `--target dev\|prod`, `--legacy-mysql "..."`, `--target-postgres "..."` |

O destino é escolhido por **ambiente** (`dev` ou `prod`). Padrão: `TargetEnvironment` no `appsettings.json` (ou `dev`).

| Ambiente | Porta host | Database | Compose |
|----------|------------|----------|---------|
| `dev` | **5434** | `desfudencify_dev` | `docker-compose.development.yml` |
| `prod` | **5433** | `desfudencify` | `docker-compose.production.yml` |

Exemplo de `appsettings.json`:

```json
{
  "LegacyMySql": "Server=HOST;Port=3306;Database=finances;User=root;Password=SENHA;SslMode=None;AllowPublicKeyRetrieval=True;",
  "TargetEnvironment": "dev",
  "Targets": {
    "dev": "Host=localhost;Port=5434;Database=desfudencify_dev;Username=desfudencify;Password=desfudencify",
    "prod": "Host=localhost;Port=5433;Database=desfudencify;Username=desfudencify;Password=SENHA_PROD"
  },
  "Seed": {
    "AdminEmail": "admin@desfudencify.local",
    "AdminPassword": "Admin@12345",
    "AdminFullName": "Administrator"
  }
}
```

`--target-postgres` / `TARGET_POSTGRES_CONNECTION` sobrescrevem a seleção por ambiente.

## Como executar

Na raiz do repositório DesfudenciFy_2:

```powershell
# Somente leitura / contagem no ambiente de desenvolvimento
dotnet run --project backend/tools/LegacyMigrator -- --target dev --dry-run

# Importar para desenvolvimento
dotnet run --project backend/tools/LegacyMigrator -- --target dev

# Importar para produção (apaga destino — digite MIGRATE quando pedido)
dotnet run --project backend/tools/LegacyMigrator -- --target prod --wipe-target

# Wipe não interativo (CI / script)
$env:LEGACY_MIGRATE_CONFIRM = "MIGRATE"
dotnet run --project backend/tools/LegacyMigrator -- --target prod --wipe-target --yes
```
Build:

```powershell
dotnet build backend/tools/LegacyMigrator
```

## Modos de segurança

| Modo | Comportamento |
|------|----------------|
| Padrão | Recusa se o PostgreSQL já tiver usuários, tipos, contas ou dados financeiros |
| `--wipe-target` | `TRUNCATE ... CASCADE` em todas as tabelas de negócio e reimporta (confirmação `MIGRATE`) |
| `--dry-run` | Lê MySQL, monta o plano e imprime contagens — não grava |
| `--yes` / `--force` | Pula o prompt; exige `LEGACY_MIGRATE_CONFIRM=MIGRATE` |

IDs (`Guid`) das entidades principais são **preservados**. Contas e tipos criados a partir de enums usam Guids **estáveis** (hash MD5 do nome).

## Mapeamento legado → novo

| Legado (MySQL) | Novo (PostgreSQL) | Observações |
|---------------|-------------------|-------------|
| `Users` | `Users` | `FullName` derivado do e-mail; 1º usuário (por data) vira `Admin`; hash BCrypt mantido. **Sempre** inclui também o admin seed (configurável em `Seed:*`; no template de dev: `admin@desfudencify.local` / `Admin@12345`) |
| `AccountType` (enum em Investment) | `BankAccounts` | Uma conta por valor usado (Modal, XP, NuInvest, Bradesco, Wise) |
| `InvestmentType` (enum) | `InvestmentTypes` | Nomes: CDB, Tesouro SELIC, FII, LCI, LCA, Viagem (+ seeds padrão) |
| `Reserves` | `Reserves` | **`Owner` (Daniel/Cassia/Comum) descartado** |
| `Entries` | `Entries` | `Destination=Reserve`; `OccurredAt=DateCreated` |
| `Investments` | `Investments` | `Rentability` decimal → **string**; `StartDate=DateCreated`; `Status=Active` |
| `ReserveInvestmentsMaps` (ou nome antigo) | `ReserveInvestments` | Se `Id` for Guid vazio, gera Id estável; PK composta legada → PK `Id` |
| `Costs` | `FixedCosts` | `Type` → `Recurrence`; **% Daniel/Cassia** vão para nota na `Description` |
| `CostPayments` | `CostPayments` | `EntryId` fica `null` (débito automático não existia no legado) |
| `IncomeSources` | `IncomeSources` | **`Owner` descartado** |
| `Purchases` | `Purchases` | **`Owner` descartado** |
| `PurchaseInstallments` | `PurchaseInstallments` | Mapeamento direto |
| — | `Properties` / amortizações | **Sem equivalente legado** — não migrado |
| `InvestmentHistory` (se existir) | — | Ignorado (documentado no log) |
| Transferências | — | Continuam sendo pares de `Entries` (texto em `Observation`) |

## Campos / conceitos removidos de propósito

- Donos e splits Daniel/Cassia (`AccountUser`, `DanielPercentage`, `CassiaPercentage`)
- Conta/tipo como enum embutido no investimento (viram tabelas de lookup)
- Histórico de investimento legado

## Caveats importantes

1. **Rentabilidade**: no legado é `decimal` (às vezes % absoluto, às vezes % do CDI). No app novo é texto livre — o migrator grava o número em cultura invariante (ex.: `112.5`). Ajuste manual se quiser rótulos como `100% CDI`.
2. **Owners**: informação de dono não existe no schema novo; não inventamos FKs. Splits de custo ficam só como texto na descrição.
3. **Imóveis**: cadastre manualmente no DesfudenciFy_2 após a migração.
4. **Seed da API**: se a API já rodou e populou admin/tipos, use `--wipe-target` antes de migrar (senão o migrator recusa o destino “não vazio”).
5. **Schema legado antigo**: colunas opcionais (`DisplayColor`, `MonthlyGoal`, `Costs.ReserveId`) e nomes de tabela de mapa são detectados em runtime.

## Estrutura

```
backend/tools/LegacyMigrator/
  Program.cs           CLI / config
  LegacyReader.cs      Leitura MySQL
  MigrationRunner.cs   Plano + wipe + escrita
  LegacyMappings.cs    Enums / Guids estáveis
  TargetDbContext.cs   EF destino (sem stamp de datas)
  README.md
```
