using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesfudenciFy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyExpenseTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "PropertyExpenseTypes" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(100) NOT NULL,
                    "Description" character varying(500) NULL,
                    "IsActive" boolean NOT NULL,
                    "DateCreated" timestamp with time zone NOT NULL,
                    "LastUpdate" timestamp with time zone NULL,
                    CONSTRAINT "PK_PropertyExpenseTypes" PRIMARY KEY ("Id")
                );

                INSERT INTO "PropertyExpenseTypes" ("Id", "Name", "Description", "IsActive", "DateCreated", "LastUpdate")
                SELECT 'c1111111-1111-1111-1111-111111111111'::uuid, 'Leilão', NULL, TRUE, NOW() AT TIME ZONE 'utc', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "PropertyExpenseTypes" WHERE "Name" = 'Leilão');

                INSERT INTO "PropertyExpenseTypes" ("Id", "Name", "Description", "IsActive", "DateCreated", "LastUpdate")
                SELECT 'c2222222-2222-2222-2222-222222222222'::uuid, 'Material', NULL, TRUE, NOW() AT TIME ZONE 'utc', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "PropertyExpenseTypes" WHERE "Name" = 'Material');

                INSERT INTO "PropertyExpenseTypes" ("Id", "Name", "Description", "IsActive", "DateCreated", "LastUpdate")
                SELECT 'c3333333-3333-3333-3333-333333333333'::uuid, 'Serviços', NULL, TRUE, NOW() AT TIME ZONE 'utc', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "PropertyExpenseTypes" WHERE "Name" = 'Serviços');

                INSERT INTO "PropertyExpenseTypes" ("Id", "Name", "Description", "IsActive", "DateCreated", "LastUpdate")
                SELECT 'c4444444-4444-4444-4444-444444444444'::uuid, 'Documentação', NULL, TRUE, NOW() AT TIME ZONE 'utc', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "PropertyExpenseTypes" WHERE "Name" = 'Documentação');

                ALTER TABLE "PropertyExpenses" ADD COLUMN IF NOT EXISTS "ExpenseTypeId" uuid NULL;

                UPDATE "PropertyExpenses"
                SET "ExpenseTypeId" = (
                    SELECT "Id" FROM "PropertyExpenseTypes" WHERE "Name" = 'Serviços' LIMIT 1
                )
                WHERE "ExpenseTypeId" IS NULL;

                ALTER TABLE "PropertyExpenses" ALTER COLUMN "ExpenseTypeId" SET NOT NULL;

                CREATE INDEX IF NOT EXISTS "IX_PropertyExpenses_ExpenseTypeId" ON "PropertyExpenses" ("ExpenseTypeId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_PropertyExpenses_PropertyExpenseTypes_ExpenseTypeId'
                    ) THEN
                        ALTER TABLE "PropertyExpenses"
                            ADD CONSTRAINT "FK_PropertyExpenses_PropertyExpenseTypes_ExpenseTypeId"
                            FOREIGN KEY ("ExpenseTypeId") REFERENCES "PropertyExpenseTypes" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "PropertyExpenses" DROP CONSTRAINT IF EXISTS "FK_PropertyExpenses_PropertyExpenseTypes_ExpenseTypeId";
                DROP INDEX IF EXISTS "IX_PropertyExpenses_ExpenseTypeId";
                ALTER TABLE "PropertyExpenses" DROP COLUMN IF EXISTS "ExpenseTypeId";
                DROP TABLE IF EXISTS "PropertyExpenseTypes";
                """);
        }
    }
}
