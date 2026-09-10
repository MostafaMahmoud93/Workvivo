using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotificationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Security_Users_Created_By",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_NotificationUsers_Notification_Id",
                table: "NotificationUsers");

            migrationBuilder.DropIndex(
                name: "IX_NotificationUsers_Reciever_Id",
                table: "NotificationUsers");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Created_By",
                table: "Notifications");

            // SYSUTCDATETIME rather than the scaffolded DateTime.MinValue. The table is
            // empty today, so nothing is being backfilled - but a column whose default
            // is year 1 turns any future insert that forgets to set it into a row that
            // sorts to the bottom of somebody's notification list forever.
            migrationBuilder.AddColumn<DateTime>(
                name: "Create_Date",
                table: "NotificationUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "Seen_Date",
                table: "NotificationUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RedirectUrl",
                table: "Notifications",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notification_Status",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Header_En",
                table: "Notifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Header_Ar",
                table: "Notifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "Actor_Employee_Id",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Creator_User_Id",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Entity_Id",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Entity_Type",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUsers_Recipient",
                table: "NotificationUsers",
                columns: new[] { "Reciever_Id", "Create_Date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUsers_Unread",
                table: "NotificationUsers",
                column: "Reciever_Id",
                filter: "[IS_Seen] = 0 AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_NotificationUsers_Delivery",
                table: "NotificationUsers",
                columns: new[] { "Notification_Id", "Reciever_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Actor_Employee_Id",
                table: "Notifications",
                column: "Actor_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Creator_User_Id",
                table: "Notifications",
                column: "Creator_User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Entity",
                table: "Notifications",
                columns: new[] { "Entity_Type", "Entity_Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Org_Employees_Actor_Employee_Id",
                table: "Notifications",
                column: "Actor_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Security_Users_Creator_User_Id",
                table: "Notifications",
                column: "Creator_User_Id",
                principalTable: "Security_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Org_Employees_Actor_Employee_Id",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Security_Users_Creator_User_Id",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_NotificationUsers_Recipient",
                table: "NotificationUsers");

            migrationBuilder.DropIndex(
                name: "IX_NotificationUsers_Unread",
                table: "NotificationUsers");

            migrationBuilder.DropIndex(
                name: "UX_NotificationUsers_Delivery",
                table: "NotificationUsers");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Actor_Employee_Id",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Creator_User_Id",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Entity",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Create_Date",
                table: "NotificationUsers");

            migrationBuilder.DropColumn(
                name: "Seen_Date",
                table: "NotificationUsers");

            migrationBuilder.DropColumn(
                name: "Actor_Employee_Id",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Creator_User_Id",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Entity_Id",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Entity_Type",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "RedirectUrl",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notification_Status",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Header_En",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Header_Ar",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUsers_Notification_Id",
                table: "NotificationUsers",
                column: "Notification_Id");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUsers_Reciever_Id",
                table: "NotificationUsers",
                column: "Reciever_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Created_By",
                table: "Notifications",
                column: "Created_By");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Security_Users_Created_By",
                table: "Notifications",
                column: "Created_By",
                principalTable: "Security_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
