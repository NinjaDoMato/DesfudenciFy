using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesfudenciFy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePropertyIsActiveToIsRented : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Properties",
                newName: "IsRented");

            // Former IsActive values are not equivalent to rented status.
            migrationBuilder.Sql("""UPDATE "Properties" SET "IsRented" = FALSE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsRented",
                table: "Properties",
                newName: "IsActive");
        }
    }
}
