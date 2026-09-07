using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultFacility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Single-facility MVP: initial stock at product creation and manual "Agregar Unidades"
            // in the inventory section both need somewhere to put units, and no facility existed
            // in the schema until now.
            migrationBuilder.InsertData(
                table: "facilities",
                columns: new[] { "id", "name", "type", "address", "city" },
                values: new object[] { new Guid("99999999-9999-9999-9999-999999999999"), "Bodega Central", "WAREHOUSE", "Calle 100 # 15-20", "Bogotá" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "facilities",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));
        }
    }
}
