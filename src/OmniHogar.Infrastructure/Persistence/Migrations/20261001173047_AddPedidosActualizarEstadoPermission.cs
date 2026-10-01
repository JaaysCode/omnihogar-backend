using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidosActualizarEstadoPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "description", "name" },
                values: new object[] { new Guid("11110000-0000-0000-0000-000000000008"), "Actualizar el estado de un pedido.", "pedidos.actualizar_estado" });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("11110000-0000-0000-0000-000000000008"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("11110000-0000-0000-0000-000000000008"), new Guid("44444444-4444-4444-4444-444444444444") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("11110000-0000-0000-0000-000000000008"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("11110000-0000-0000-0000-000000000008"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("11110000-0000-0000-0000-000000000008"));
        }
    }
}
