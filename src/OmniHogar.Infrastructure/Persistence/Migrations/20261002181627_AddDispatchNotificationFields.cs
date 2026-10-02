using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniHogar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchNotificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_channel",
                table: "notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications");

            migrationBuilder.AddColumn<bool>(
                name: "is_read",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "order_id",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "read_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "notified_at",
                table: "dispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_order_id",
                table: "notifications",
                column: "order_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_channel",
                table: "notifications",
                sql: "channel IN ('email','whatsapp','sms','in_app')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications",
                sql: "type IN ('order_confirmed','payment_approved','in_dispatch','delivered','dispatch_ready')");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_orders_order_id",
                table: "notifications",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notifications_orders_order_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_order_id",
                table: "notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_channel",
                table: "notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "is_read",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "read_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "notified_at",
                table: "dispatches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_channel",
                table: "notifications",
                sql: "channel IN ('email','whatsapp','sms')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_type",
                table: "notifications",
                sql: "type IN ('order_confirmed','payment_approved','in_dispatch','delivered')");
        }
    }
}
