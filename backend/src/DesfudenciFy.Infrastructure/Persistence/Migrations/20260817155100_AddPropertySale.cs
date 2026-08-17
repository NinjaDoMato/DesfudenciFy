using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DesfudenciFy.Infrastructure.Persistence;

#nullable disable

namespace DesfudenciFy.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817155100_AddPropertySale")]
    public partial class AddPropertySale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Properties" ADD COLUMN IF NOT EXISTS "Status" integer NOT NULL DEFAULT 0;
                ALTER TABLE "Properties" ADD COLUMN IF NOT EXISTS "SaleAmount" numeric(18,2) NULL;
                ALTER TABLE "Properties" ADD COLUMN IF NOT EXISTS "SoldAt" timestamp with time zone NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Properties" DROP COLUMN IF EXISTS "SoldAt";
                ALTER TABLE "Properties" DROP COLUMN IF EXISTS "SaleAmount";
                ALTER TABLE "Properties" DROP COLUMN IF EXISTS "Status";
                """);
        }
    }
}
