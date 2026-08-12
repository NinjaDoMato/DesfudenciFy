using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesfudenciFy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyEconomicsAndIncomeTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent SQL so a partially applied previous attempt can complete.
            migrationBuilder.Sql("""
                ALTER TABLE "Properties" ADD COLUMN IF NOT EXISTS "AppraisedValue" numeric(18,2) NOT NULL DEFAULT 0.0;
                ALTER TABLE "Properties" ADD COLUMN IF NOT EXISTS "RentalAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

                ALTER TABLE "FixedCosts" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
                ALTER TABLE "FixedCosts" ADD COLUMN IF NOT EXISTS "PropertyId" uuid NULL;

                CREATE TABLE IF NOT EXISTS "IncomeTypes" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(100) NOT NULL,
                    "Description" character varying(500) NULL,
                    "IsActive" boolean NOT NULL,
                    "DateCreated" timestamp with time zone NOT NULL,
                    "LastUpdate" timestamp with time zone NULL,
                    CONSTRAINT "PK_IncomeTypes" PRIMARY KEY ("Id")
                );

                INSERT INTO "IncomeTypes" ("Id", "Name", "Description", "IsActive", "DateCreated", "LastUpdate")
                SELECT 'a1111111-1111-1111-1111-111111111111'::uuid, 'Renda extra', NULL, TRUE, NOW() AT TIME ZONE 'utc', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "IncomeTypes" WHERE "Id" = 'a1111111-1111-1111-1111-111111111111'::uuid);

                ALTER TABLE "IncomeSources" ADD COLUMN IF NOT EXISTS "IncomeTypeId" uuid NULL;
                UPDATE "IncomeSources"
                SET "IncomeTypeId" = 'a1111111-1111-1111-1111-111111111111'::uuid
                WHERE "IncomeTypeId" IS NULL;
                ALTER TABLE "IncomeSources" ALTER COLUMN "IncomeTypeId" SET NOT NULL;
                ALTER TABLE "IncomeSources" ALTER COLUMN "IncomeTypeId" SET DEFAULT 'a1111111-1111-1111-1111-111111111111'::uuid;
                ALTER TABLE "IncomeSources" ADD COLUMN IF NOT EXISTS "PropertyId" uuid NULL;

                CREATE TABLE IF NOT EXISTS "PropertyExpenses" (
                    "Id" uuid NOT NULL,
                    "PropertyId" uuid NOT NULL,
                    "Amount" numeric(18,2) NOT NULL,
                    "Observation" character varying(500) NOT NULL,
                    "OccurredAt" timestamp with time zone NOT NULL,
                    "EntryId" uuid NULL,
                    "DateCreated" timestamp with time zone NOT NULL,
                    "LastUpdate" timestamp with time zone NULL,
                    CONSTRAINT "PK_PropertyExpenses" PRIMARY KEY ("Id")
                );
                ALTER TABLE "PropertyExpenses" ADD COLUMN IF NOT EXISTS "EntryId" uuid NULL;

                CREATE TABLE IF NOT EXISTS "PropertyRentPayments" (
                    "Id" uuid NOT NULL,
                    "PropertyId" uuid NOT NULL,
                    "Amount" numeric(18,2) NOT NULL,
                    "Observation" character varying(500) NULL,
                    "PaidAt" timestamp with time zone NOT NULL,
                    "EntryId" uuid NOT NULL,
                    "DateCreated" timestamp with time zone NOT NULL,
                    "LastUpdate" timestamp with time zone NULL,
                    CONSTRAINT "PK_PropertyRentPayments" PRIMARY KEY ("Id")
                );
                ALTER TABLE "PropertyRentPayments" ADD COLUMN IF NOT EXISTS "EntryId" uuid NULL;
                DELETE FROM "PropertyRentPayments" WHERE "EntryId" IS NULL;
                ALTER TABLE "PropertyRentPayments" ALTER COLUMN "EntryId" SET NOT NULL;

                CREATE INDEX IF NOT EXISTS "IX_IncomeSources_IncomeTypeId" ON "IncomeSources" ("IncomeTypeId");
                CREATE INDEX IF NOT EXISTS "IX_IncomeSources_PropertyId" ON "IncomeSources" ("PropertyId");
                CREATE INDEX IF NOT EXISTS "IX_FixedCosts_PropertyId" ON "FixedCosts" ("PropertyId");
                CREATE INDEX IF NOT EXISTS "IX_PropertyExpenses_EntryId" ON "PropertyExpenses" ("EntryId");
                CREATE INDEX IF NOT EXISTS "IX_PropertyExpenses_PropertyId" ON "PropertyExpenses" ("PropertyId");
                CREATE INDEX IF NOT EXISTS "IX_PropertyRentPayments_EntryId" ON "PropertyRentPayments" ("EntryId");
                CREATE INDEX IF NOT EXISTS "IX_PropertyRentPayments_PropertyId" ON "PropertyRentPayments" ("PropertyId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_FixedCosts_Properties_PropertyId'
                    ) THEN
                        ALTER TABLE "FixedCosts"
                            ADD CONSTRAINT "FK_FixedCosts_Properties_PropertyId"
                            FOREIGN KEY ("PropertyId") REFERENCES "Properties" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_IncomeSources_IncomeTypes_IncomeTypeId'
                    ) THEN
                        ALTER TABLE "IncomeSources"
                            ADD CONSTRAINT "FK_IncomeSources_IncomeTypes_IncomeTypeId"
                            FOREIGN KEY ("IncomeTypeId") REFERENCES "IncomeTypes" ("Id") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_IncomeSources_Properties_PropertyId'
                    ) THEN
                        ALTER TABLE "IncomeSources"
                            ADD CONSTRAINT "FK_IncomeSources_Properties_PropertyId"
                            FOREIGN KEY ("PropertyId") REFERENCES "Properties" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_PropertyExpenses_Entries_EntryId'
                    ) THEN
                        ALTER TABLE "PropertyExpenses"
                            ADD CONSTRAINT "FK_PropertyExpenses_Entries_EntryId"
                            FOREIGN KEY ("EntryId") REFERENCES "Entries" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_PropertyExpenses_Properties_PropertyId'
                    ) THEN
                        ALTER TABLE "PropertyExpenses"
                            ADD CONSTRAINT "FK_PropertyExpenses_Properties_PropertyId"
                            FOREIGN KEY ("PropertyId") REFERENCES "Properties" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_PropertyRentPayments_Entries_EntryId'
                    ) THEN
                        ALTER TABLE "PropertyRentPayments"
                            ADD CONSTRAINT "FK_PropertyRentPayments_Entries_EntryId"
                            FOREIGN KEY ("EntryId") REFERENCES "Entries" ("Id") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_PropertyRentPayments_Properties_PropertyId'
                    ) THEN
                        ALTER TABLE "PropertyRentPayments"
                            ADD CONSTRAINT "FK_PropertyRentPayments_Properties_PropertyId"
                            FOREIGN KEY ("PropertyId") REFERENCES "Properties" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "FixedCosts" DROP CONSTRAINT IF EXISTS "FK_FixedCosts_Properties_PropertyId";
                ALTER TABLE "IncomeSources" DROP CONSTRAINT IF EXISTS "FK_IncomeSources_IncomeTypes_IncomeTypeId";
                ALTER TABLE "IncomeSources" DROP CONSTRAINT IF EXISTS "FK_IncomeSources_Properties_PropertyId";

                DROP TABLE IF EXISTS "PropertyExpenses";
                DROP TABLE IF EXISTS "PropertyRentPayments";

                DROP INDEX IF EXISTS "IX_IncomeSources_IncomeTypeId";
                DROP INDEX IF EXISTS "IX_IncomeSources_PropertyId";
                DROP INDEX IF EXISTS "IX_FixedCosts_PropertyId";

                ALTER TABLE "Properties" DROP COLUMN IF EXISTS "AppraisedValue";
                ALTER TABLE "Properties" DROP COLUMN IF EXISTS "RentalAmount";
                ALTER TABLE "IncomeSources" DROP COLUMN IF EXISTS "IncomeTypeId";
                ALTER TABLE "IncomeSources" DROP COLUMN IF EXISTS "PropertyId";
                ALTER TABLE "FixedCosts" DROP COLUMN IF EXISTS "IsActive";
                ALTER TABLE "FixedCosts" DROP COLUMN IF EXISTS "PropertyId";

                DROP TABLE IF EXISTS "IncomeTypes";
                """);
        }
    }
}
