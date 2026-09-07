using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Bootstraps a single Admin-role employee account so there's a way to log in and use
    /// EmployeesController (Admin-only) to create real accounts afterwards.
    ///
    /// Login: admin@omnihogar.com / Admin123!  — change the password after first login.
    /// The hash below is a real ASP.NET Core Identity v3 (PBKDF2) hash for that password,
    /// produced with the same PasswordHasher&lt;T&gt; the app verifies against.
    /// </summary>
    public partial class SeedAdminUser : Migration
    {
        private static readonly Guid AdminUserId = new("99999999-9999-9999-9999-999999999999");
        private static readonly Guid AdminRoleId = new("11111111-1111-1111-1111-111111111111");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "user_type", "first_name", "last_name", "email", "phone", "password_hash", "facility_id", "status", "created_at" },
                values: new object[]
                {
                    AdminUserId,
                    "employee",
                    "System",
                    "Admin",
                    "admin@omnihogar.com",
                    null,
                    "AQAAAAIAAYagAAAAEAHzriFquvYvXkMV12kG3Lfk18j2nMI8W+fupZ7+hrcUF7yFsnUZ1SQ5JcIeeviXvg==",
                    null,
                    true,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "user_id", "role_id", "assigned_at" },
                values: new object[]
                {
                    AdminUserId,
                    AdminRoleId,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "user_id", "role_id" },
                keyValues: new object[] { AdminUserId, AdminRoleId });

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: AdminUserId);
        }
    }
}
