using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvivo.Infrastructure.Migrations
{
    /// <summary>
    /// Replaces the accidental VW_UserActions table with a real SQL view, and gives
    /// per-user permissions a deny flag.
    ///
    /// The table was never intentional: the entity was configured with HasNoKey() but
    /// no ToView(), so EF scaffolded it as an ordinary table. It was created empty,
    /// nothing ever wrote to it, and every permission check in the application therefore
    /// resolved to "no permissions" - silently, because an empty result is
    /// indistinguishable from a user who genuinely has none.
    /// </summary>
    public partial class PermissionViewAndUserDeny : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default true, not false.
            //
            // EF scaffolds a bool column with defaultValue: false. Applied here that
            // would flip every existing per-user permission row from "granted" to
            // "denied" - silently revoking access rather than preserving it. Existing
            // rows were all grants, so true is the value that keeps their meaning.
            migrationBuilder.AddColumn<bool>(
                name: "Is_Granted",
                table: "Security_UserPermissions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.DropTable(
                name: "VW_UserActions");

            migrationBuilder.Sql(@"
                -- Flattens the permission model into one row per (user, permission).
                --
                -- Resolution order, which this view encodes:
                --   1. An explicit user-level DENY beats everything.
                --   2. An explicit user-level GRANT applies.
                --   3. Otherwise, any of the user's roles granting it applies.
                --
                -- Deny-wins is the safe direction: the failure mode of the opposite rule is somebody
                -- keeping access that was explicitly taken away from them.
                --
                -- Deactivated and deleted accounts resolve to no permissions at all, so revoking
                -- access takes effect on the next check rather than at the next sign-in.
                CREATE VIEW VW_UserActions AS

                    -- Permissions granted through a role.
                    SELECT
                        ugl.User_Id,
                        lsa.Base_Route,
                        lsa.Screen_Action_Id,
                        lsa.Action_Code,
                        lsa.Permission_Key,
                        s.Screen_Description_Ar,
                        s.Screen_Description_En,
                        sa.Action_Name_Ar,
                        sa.Action_Name_En
                    FROM Security_UserGroupsLink   ugl
                    JOIN Security_Users            u   ON u.Id  = ugl.User_Id
                                                      AND u.Is_Active = 1
                                                      AND u.Is_Deleted = 0
                    JOIN Security_UserGroups       g   ON g.Id  = ugl.Group_Id
                                                      AND g.Is_Active = 1
                                                      AND g.Is_Deleted = 0
                    JOIN Security_GroupPermissions gp  ON gp.Group_Id = ugl.Group_Id
                                                      AND gp.Is_Deleted = 0
                    JOIN Common_LinkScreenActions  lsa ON lsa.Id = gp.Link_Screen_Action_Id
                                                      AND lsa.Is_Deleted = 0
                    JOIN Common_Screens            s   ON s.Id  = lsa.Screen_Id
                    JOIN Common_ScreenActions      sa  ON sa.Id = lsa.Screen_Action_Id
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM Security_UserPermissions denial
                        WHERE denial.User_Id              = ugl.User_Id
                          AND denial.Link_Screen_Action_Id = lsa.Id
                          AND denial.Is_Granted            = 0
                          AND denial.Is_Deleted            = 0)

                    UNION

                    -- Permissions granted to the user directly.
                    SELECT
                        up.User_Id,
                        lsa.Base_Route,
                        lsa.Screen_Action_Id,
                        lsa.Action_Code,
                        lsa.Permission_Key,
                        s.Screen_Description_Ar,
                        s.Screen_Description_En,
                        sa.Action_Name_Ar,
                        sa.Action_Name_En
                    FROM Security_UserPermissions  up
                    JOIN Security_Users            u   ON u.Id  = up.User_Id
                                                      AND u.Is_Active = 1
                                                      AND u.Is_Deleted = 0
                    JOIN Common_LinkScreenActions  lsa ON lsa.Id = up.Link_Screen_Action_Id
                                                      AND lsa.Is_Deleted = 0
                    JOIN Common_Screens            s   ON s.Id  = lsa.Screen_Id
                    JOIN Common_ScreenActions      sa  ON sa.Id = lsa.Screen_Action_Id
                    WHERE up.Is_Granted = 1
                      AND up.Is_Deleted = 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS VW_UserActions;");

            migrationBuilder.DropColumn(
                name: "Is_Granted",
                table: "Security_UserPermissions");

            // Recreated as the keyless table EF originally scaffolded, so Down() is a
            // true inverse of Up() even though that table was a mistake.
            migrationBuilder.CreateTable(
                name: "VW_UserActions",
                columns: table => new
                {
                    Action_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Base_Route = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Permission_Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Screen_Action_Id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: false),
                    Screen_Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Screen_Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    User_Id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                });
        }
    }
}
