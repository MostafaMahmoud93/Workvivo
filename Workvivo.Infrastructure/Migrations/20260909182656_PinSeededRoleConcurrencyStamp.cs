using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PinSeededRoleConcurrencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"),
                column: "ConcurrencyStamp",
                value: "3e9a71d4-0b62-4c8f-95a1-7d4e2f6b8c30");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"),
                column: "ConcurrencyStamp",
                value: null);
        }
    }
}
