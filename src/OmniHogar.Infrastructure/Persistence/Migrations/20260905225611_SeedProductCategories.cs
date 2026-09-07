using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "product_categories",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("c1000000-0000-0000-0000-000000000001"), "Cocina" },
                    { new Guid("c1000000-0000-0000-0000-000000000002"), "Baño" },
                    { new Guid("c1000000-0000-0000-0000-000000000003"), "Dormitorio" },
                    { new Guid("c1000000-0000-0000-0000-000000000004"), "Sala y Comedor" },
                    { new Guid("c1000000-0000-0000-0000-000000000005"), "Electrodomésticos" },
                    { new Guid("c1000000-0000-0000-0000-000000000006"), "Iluminación" },
                    { new Guid("c1000000-0000-0000-0000-000000000007"), "Decoración" },
                    { new Guid("c1000000-0000-0000-0000-000000000008"), "Jardín y Exteriores" },
                    { new Guid("c1000000-0000-0000-0000-000000000009"), "Organización y Almacenamiento" },
                    { new Guid("c100000a-0000-0000-0000-000000000010"), "Herramientas y Ferretería" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000001"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000002"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000003"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000004"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000005"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000006"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000007"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000008"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c1000000-0000-0000-0000-000000000009"));
            migrationBuilder.DeleteData(table: "product_categories", keyColumn: "id", keyValue: new Guid("c100000a-0000-0000-0000-000000000010"));
        }
    }
}
