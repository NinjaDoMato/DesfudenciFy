using System;
using DesfudenciFy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesfudenciFy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817122400_LinkPurchaseToReserve")]
    public partial class LinkPurchaseToReserve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReserveId",
                table: "Purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntryId",
                table: "PurchaseInstallments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ReserveId",
                table: "Purchases",
                column: "ReserveId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInstallments_EntryId",
                table: "PurchaseInstallments",
                column: "EntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Reserves_ReserveId",
                table: "Purchases",
                column: "ReserveId",
                principalTable: "Reserves",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInstallments_Entries_EntryId",
                table: "PurchaseInstallments",
                column: "EntryId",
                principalTable: "Entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Reserves_ReserveId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInstallments_Entries_EntryId",
                table: "PurchaseInstallments");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ReserveId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInstallments_EntryId",
                table: "PurchaseInstallments");

            migrationBuilder.DropColumn(
                name: "ReserveId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "EntryId",
                table: "PurchaseInstallments");
        }
    }
}
