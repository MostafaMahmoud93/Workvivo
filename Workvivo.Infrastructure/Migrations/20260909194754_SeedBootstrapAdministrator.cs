using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBootstrapAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Security_UserGroupsLink",
                columns: new[] { "Group_Id", "User_Id" },
                values: new object[] { new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Security_UserGroupsLink",
                keyColumns: new[] { "Group_Id", "User_Id" },
                keyValues: new object[] { new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90") });
        }
    }
}
