using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusChangedNotificationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications",
                sql: "type IN ('order_confirmed','payment_approved','in_dispatch','delivered','dispatch_ready','order_status_changed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications",
                sql: "type IN ('order_confirmed','payment_approved','in_dispatch','delivered','dispatch_ready')");
        }
    }
}
