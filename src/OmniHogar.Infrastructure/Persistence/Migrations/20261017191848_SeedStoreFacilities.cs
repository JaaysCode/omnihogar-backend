using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStoreFacilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Points of sale, so inventory transfers have origins/destinations besides the
            // warehouse. Names sort after "Bodega Central" on purpose: the single-facility flows
            // (manual stock, store sales, chat orders) pick the first facility by name.
            migrationBuilder.InsertData(
                table: "facilities",
                columns: new[] { "id", "name", "type", "address", "city" },
                values: new object[,]
                {
                    { new Guid("88888888-8888-8888-8888-888888888881"), "Tienda 1", "POS", "Carrera 7 # 72-41", "Bogotá" },
                    { new Guid("88888888-8888-8888-8888-888888888882"), "Tienda 2", "POS", "Avenida El Poblado # 10-25", "Medellín" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "facilities",
                keyColumn: "id",
                keyValues: new object[]
                {
                    new Guid("88888888-8888-8888-8888-888888888881"),
                    new Guid("88888888-8888-8888-8888-888888888882")
                });
        }
    }
}
