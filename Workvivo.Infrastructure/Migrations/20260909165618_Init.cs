using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Workvivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Common_MainModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationNo = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Common_MainModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Common_ScreenActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action_Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Common_ScreenActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Changable_By_User = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategories", x => x.Id);
                    table.UniqueConstraint("AK_LookupCategories_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Security_MasterData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Category_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Master_Data_Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Master_Data_Parent_Id = table.Column<int>(type: "int", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Item_Order = table.Column<int>(type: "int", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_MasterData", x => x.Id);
                    table.UniqueConstraint("AK_Security_MasterData_Master_Data_Code", x => x.Master_Data_Code);
                    table.ForeignKey(
                        name: "FK_Security_MasterData_Security_MasterData_Master_Data_Parent_Id",
                        column: x => x.Master_Data_Parent_Id,
                        principalTable: "Security_MasterData",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VW_UserActions",
                columns: table => new
                {
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Base_Route = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Screen_Action_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Screen_Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Screen_Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Common_Screens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Main_Module_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Screen_Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Screen_Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Parent_Screen_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Branch = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Menu_Or_Not = table.Column<bool>(type: "bit", nullable: false),
                    No_Login = table.Column<bool>(type: "bit", nullable: false),
                    Screen_Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Common_Screens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Common_Screens_Common_MainModules_Main_Module_Id",
                        column: x => x.Main_Module_Id,
                        principalTable: "Common_MainModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Common_Screens_Common_Screens_Parent_Screen_Id",
                        column: x => x.Parent_Screen_Id,
                        principalTable: "Common_Screens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category_Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lookups_LookupCategories_Category_Code",
                        column: x => x.Category_Code,
                        principalTable: "LookupCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Security_UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    User_Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modify_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_UserGroups_Security_MasterData_User_Type",
                        column: x => x.User_Type,
                        principalTable: "Security_MasterData",
                        principalColumn: "Master_Data_Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Security_Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Admin = table.Column<bool>(type: "bit", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    User_Type = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Profile_PictureURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signee_PictureURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Full_Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Full_Name_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobileNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Office_TelNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentNo = table.Column<int>(type: "int", nullable: true),
                    ManagNo = table.Column<int>(type: "int", nullable: true),
                    Is_Manager = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_Users_Security_MasterData_User_Type",
                        column: x => x.User_Type,
                        principalTable: "Security_MasterData",
                        principalColumn: "Master_Data_Code");
                });

            migrationBuilder.CreateTable(
                name: "SysSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysSettingID = table.Column<int>(type: "int", nullable: false),
                    SysSettingCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SysSettingDescAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SysSettingDescEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SysSettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SysSettingValueDataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataTypeMasterDataId = table.Column<int>(type: "int", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysSetting_Security_MasterData_DataTypeMasterDataId",
                        column: x => x.DataTypeMasterDataId,
                        principalTable: "Security_MasterData",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Common_LinkScreenActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Screen_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Screen_Action_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Base_Route = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action_Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Common_LinkScreenActions", x => x.Id);
                    table.UniqueConstraint("AK_Common_LinkScreenActions_Action_Code", x => x.Action_Code);
                    table.ForeignKey(
                        name: "FK_Common_LinkScreenActions_Common_ScreenActions_Screen_Action_Id",
                        column: x => x.Screen_Action_Id,
                        principalTable: "Common_ScreenActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Common_LinkScreenActions_Common_Screens_Screen_Id",
                        column: x => x.Screen_Id,
                        principalTable: "Common_Screens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailSMSTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Screen_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HTML_Template = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HTML_Template_Defult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SMS_Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HTML_Template_Variables = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSMSTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSMSTemplates_Common_Screens_Screen_Id",
                        column: x => x.Screen_Id,
                        principalTable: "Common_Screens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_Security_UserGroups_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Security_UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_Security_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_Security_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_Security_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlobalAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Document_Id = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileMIME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalAttachments_Security_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Security_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Header_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Header_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RedirectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notification_Type = table.Column<int>(type: "int", nullable: false),
                    Notification_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Security_Users_Created_By",
                        column: x => x.Created_By,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Security_UserGroupsLink",
                columns: table => new
                {
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Group_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_UserGroupsLink", x => new { x.User_Id, x.Group_Id });
                    table.ForeignKey(
                        name: "FK_Security_UserGroupsLink_Security_UserGroups_Group_Id",
                        column: x => x.Group_Id,
                        principalTable: "Security_UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Security_UserGroupsLink_Security_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginLogs_Security_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Security_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users_ShortCuts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Page = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRouter = table.Column<bool>(type: "bit", nullable: false),
                    InsertionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IconMasterDataId = table.Column<int>(type: "int", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_ShortCuts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_ShortCuts_Security_MasterData_IconMasterDataId",
                        column: x => x.IconMasterDataId,
                        principalTable: "Security_MasterData",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_ShortCuts_Security_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Security_AccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Main_Module_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action_Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Access_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain_Controller_User = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_AccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_AccessLogs_Common_LinkScreenActions_Action_Code",
                        column: x => x.Action_Code,
                        principalTable: "Common_LinkScreenActions",
                        principalColumn: "Action_Code");
                    table.ForeignKey(
                        name: "FK_Security_AccessLogs_Common_MainModules_Main_Module_Id",
                        column: x => x.Main_Module_Id,
                        principalTable: "Common_MainModules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Security_AccessLogs_Security_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Security_GroupPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Link_Screen_Action_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Group_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_GroupPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_GroupPermissions_Common_LinkScreenActions_Link_Screen_Action_Id",
                        column: x => x.Link_Screen_Action_Id,
                        principalTable: "Common_LinkScreenActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Security_GroupPermissions_Security_UserGroups_Group_Id",
                        column: x => x.Group_Id,
                        principalTable: "Security_UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Security_UserPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Link_Screen_Action_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_UserPermissions_Common_LinkScreenActions_Link_Screen_Action_Id",
                        column: x => x.Link_Screen_Action_Id,
                        principalTable: "Common_LinkScreenActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Security_UserPermissions_Security_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailSMSHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Template_Id = table.Column<int>(type: "int", nullable: false),
                    To_Emails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<bool>(type: "bit", nullable: false),
                    Attachment_Files = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CCEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BCCEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email_Sender_Display = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Sent = table.Column<bool>(type: "bit", nullable: false),
                    Error_Id = table.Column<int>(type: "int", nullable: true),
                    Record_Insertion_Datetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resend_Try = table.Column<int>(type: "int", nullable: false),
                    Last_Resend_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSMSHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSMSHistory_EmailSMSTemplates_Template_Id",
                        column: x => x.Template_Id,
                        principalTable: "EmailSMSTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notification_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reciever_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IS_Seen = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationUsers_Notifications_Notification_Id",
                        column: x => x.Notification_Id,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationUsers_Security_Users_Reciever_Id",
                        column: x => x.Reciever_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Common_MainModules",
                columns: new[] { "Id", "ApplicationNo", "Description_Ar", "Description_En", "Is_Deleted" },
                values: new object[,]
                {
                    { new Guid("7a0b5a10-9b94-4888-a835-d2b7a9360d1e"), 2, "البوابة", "Portal", false },
                    { new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), 1, "الإدارة", "Managment", false }
                });

            migrationBuilder.InsertData(
                table: "Common_ScreenActions",
                columns: new[] { "Id", "Action_Name_Ar", "Action_Name_En", "Is_Deleted", "Order" },
                values: new object[,]
                {
                    { new Guid("211bd794-ffba-4305-a278-d25d206f18af"), "إستيراد بيانات", "Import Data", false, 12 },
                    { new Guid("2dfa1eaf-97c4-4b85-b548-6d38334735e9"), "إرسال بريد إلكتروني", "Send Email", false, 10 },
                    { new Guid("2f5a8589-6e31-4514-885c-072cfb38f000"), "تفعيل", "Activation", false, 5 },
                    { new Guid("328723da-e3c9-468e-998d-6adea7217d37"), "حذف", "Delete", false, 4 },
                    { new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), "تعديل", "Edit", false, 3 },
                    { new Guid("609b4ce8-1c5f-4ff7-a71e-3462436f3f11"), "إسترجاع", "Retrieve", false, 6 },
                    { new Guid("67fbc65e-214d-46f5-9fa6-afe35cd9c3f3"), "تقديم", "Submit", false, 8 },
                    { new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), "فتح", "View", false, 1 },
                    { new Guid("8a231277-b772-4c59-844e-54435dc912f1"), "إعتماد", "Approve", false, 11 },
                    { new Guid("c1577510-4bb5-49c1-a5e4-05b001c41e7e"), "حفظ", "Save", false, 13 },
                    { new Guid("f3fc395d-72d8-4432-b273-ea1d3efb5403"), "طباعة", "Print", false, 7 },
                    { new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), "إضافة", "Add", false, 2 }
                });

            migrationBuilder.InsertData(
                table: "Security_MasterData",
                columns: new[] { "Id", "Category_Name", "Is_Active", "Is_Deleted", "Item_Order", "Master_Data_Code", "Master_Data_Parent_Id", "Title_Ar", "Title_En" },
                values: new object[,]
                {
                    { 1, "UserType", true, false, 1, "USTYP", null, "نوع المستخدم", "User Type" },
                    { 4, "RquestStatus", true, false, 1, "REQST", null, "حالة الطلب", "Request Status" },
                    { 10, "IssuerType", true, false, 1, "ISSTY", null, "نوع المصدر", "Issuer Type" }
                });

            migrationBuilder.InsertData(
                table: "Common_Screens",
                columns: new[] { "Id", "Is_Branch", "Is_Deleted", "Link", "Main_Module_Id", "Menu_Or_Not", "No_Login", "Order", "Parent_Screen_Id", "Screen_Description_Ar", "Screen_Description_En", "Screen_Icon" },
                values: new object[,]
                {
                    { new Guid("503adb95-49a7-4fe7-b28c-0be7456c91be"), false, false, "admin/home", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 1, null, "لوحة القيادة", "Dashboard", "mat_outline:home_work" },
                    { new Guid("619bcf7f-bd18-4b93-a8ca-a0f400ed105b"), false, false, "#", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 2, null, "الحماية", "SECURITY", "heroicons_outline:shield-exclamation" },
                    { new Guid("fea9385d-7c1d-43ed-ae82-6c2fa1e48690"), false, false, "#", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 4, null, "التقارير", "Reports", "heroicons_outline:clipboard-document-list" }
                });

            migrationBuilder.InsertData(
                table: "Security_MasterData",
                columns: new[] { "Id", "Category_Name", "Is_Active", "Is_Deleted", "Item_Order", "Master_Data_Code", "Master_Data_Parent_Id", "Title_Ar", "Title_En" },
                values: new object[,]
                {
                    { 2, "UserType", true, false, 1, "MANAG", 1, "مدير إدارة", "Administrator" },
                    { 3, "UserType", true, false, 2, "PORTA", 1, "مستخدم بوابة", "User Portal" },
                    { 5, "RquestStatus", true, false, 1, "DRAFT", 4, "نسخة", "Draft" },
                    { 6, "RquestStatus", true, false, 2, "APPRO", 4, "معتمد", "Approved" },
                    { 7, "RquestStatus", true, false, 3, "REJEC", 4, "مرفوض", "Rejected" },
                    { 8, "RquestStatus", true, false, 5, "UNPRC", 4, "مرسل", "Sent" },
                    { 9, "RquestStatus", true, false, 6, "STOPD", 4, "موقوف", "Stopped" },
                    { 11, "IssuerType", true, false, 1, "DIREC", 10, "إدخال مباشر", "Direct entry" },
                    { 12, "IssuerType", true, false, 2, "IMPOR", 10, "استيراد بيانات", "Import data" }
                });

            migrationBuilder.InsertData(
                table: "Common_Screens",
                columns: new[] { "Id", "Is_Branch", "Is_Deleted", "Link", "Main_Module_Id", "Menu_Or_Not", "No_Login", "Order", "Parent_Screen_Id", "Screen_Description_Ar", "Screen_Description_En", "Screen_Icon" },
                values: new object[,]
                {
                    { new Guid("e3035fc1-9b23-49b5-90f7-470f79ad1d5b"), true, false, "admin/groupDefinition", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 1, new Guid("619bcf7f-bd18-4b93-a8ca-a0f400ed105b"), "تعريف المجموعات", "Group Definition", "heroicons_outline:user-group" },
                    { new Guid("e70a846e-7a3c-4ab4-bd6d-5f8320c3a24d"), true, false, "admin/permission", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 1, new Guid("619bcf7f-bd18-4b93-a8ca-a0f400ed105b"), "صلاحيات المستخدمين", "User Permission", "heroicons_outline:key" },
                    { new Guid("e70a846e-7a3c-4ab4-bd9d-5f8310c3a24d"), true, false, "admin/contacts", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 1, new Guid("619bcf7f-bd18-4b93-a8ca-a0f400ed105b"), "المستخدمين", "Users", "heroicons_outline:users" }
                });

            migrationBuilder.InsertData(
                table: "Security_UserGroups",
                columns: new[] { "Id", "ConcurrencyStamp", "Create_Date", "Created_By", "Is_Active", "Is_Deleted", "Last_Modify_By", "Last_Modify_Date", "Name", "Name_Ar", "Name_En", "NormalizedName", "User_Type" },
                values: new object[] { new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "HKJHW", "مدير إدارة", "Administrator", "HKJHW", "MANAG" });

            migrationBuilder.InsertData(
                table: "Security_Users",
                columns: new[] { "Id", "ConcurrencyStamp", "Create_Date", "Created_By", "DepartmentNo", "Email", "EmailConfirmed", "Full_Name_Ar", "Full_Name_En", "Is_Active", "Is_Admin", "Is_Deleted", "Is_Manager", "JobTitle", "Last_Modified_By", "Last_Modify_Date", "ManagNo", "MobileNo", "NormalizedEmail", "NormalizedUserName", "Office_TelNo", "PasswordHash", "PhoneNumber", "Profile_PictureURL", "SecurityStamp", "Signee_PictureURL", "UserName", "User_Type" },
                values: new object[] { new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), "c47b9e21-5d38-4f60-a913-7e2b8c4d6f05", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "must345@yahoo.com", true, "مصطفى محمود", "Mostafa Mahmoud", true, true, false, false, null, null, null, 1, "0564899515", "MUST345@YAHOO.COM", "DEV", null, "AQAAAAIAAYagAAAAEEOc/l+BJqo9HCOS4X6JZSMccZGEliVV0wW8Raa9FutxSoJ+HkJ3D4m/cgElTwiPBw==", null, null, "6a1f0c3d-9b2e-4a57-8c31-0d5e7f9a2b46", null, "dev", "MANAG" });

            migrationBuilder.InsertData(
                table: "Common_LinkScreenActions",
                columns: new[] { "Id", "Action_Code", "Base_Route", "Is_Deleted", "Screen_Action_Id", "Screen_Id" },
                values: new object[,]
                {
                    { new Guid("0c2244d0-fc92-479f-9d57-ffe9a38a0246"), "GGGGA", "managment/api/Group/GetGroups", false, new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("e3035fc1-9b23-49b5-90f7-470f79ad1d5b") },
                    { new Guid("1f68dc32-021a-43fd-8ee1-023c1ce402a4"), "AAEGA", "managment/api/Group/CreateGroup", false, new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), new Guid("e3035fc1-9b23-49b5-90f7-470f79ad1d5b") },
                    { new Guid("48984f43-658b-4adf-8788-248f54d1faa0"), "GUACS", "managment/api/GroupAction/Get", false, new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("e70a846e-7a3c-4ab4-bd6d-5f8320c3a24d") },
                    { new Guid("77c404d1-2f37-4728-9608-e9b19194ae07"), "EAEGA", "managment/api/Group/EditGroup", false, new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("e3035fc1-9b23-49b5-90f7-470f79ad1d5b") },
                    { new Guid("7e7e47f6-961b-4c60-9b3a-62981dc83326"), "AAEUA", "managment/api/GroupAction/AddEdit", false, new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("e70a846e-7a3c-4ab4-bd6d-5f8320c3a24d") },
                    { new Guid("8fde4946-152d-45d5-96a0-e25d5f2cb274"), "DDDUA", "managment/api/GroupAction/Delete", false, new Guid("328723da-e3c9-468e-998d-6adea7217d37"), new Guid("e70a846e-7a3c-4ab4-bd6d-5f8320c3a24d") },
                    { new Guid("9741c3a7-167b-4731-92dd-6ec28780f520"), "EUSRA", "managment/api/User/EditUser", false, new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("e70a846e-7a3c-4ab4-bd9d-5f8310c3a24d") },
                    { new Guid("b6a4e50e-9ec8-45fb-ae22-807f10b2679e"), "DUSRA", "managment/api/User/DeleteUser", false, new Guid("328723da-e3c9-468e-998d-6adea7217d37"), new Guid("e70a846e-7a3c-4ab4-bd9d-5f8310c3a24d") },
                    { new Guid("e4ffa587-2100-4f64-8794-29c9162d4ce8"), "DDDGA", "managment/api/Group/DeleteGroup", false, new Guid("328723da-e3c9-468e-998d-6adea7217d37"), new Guid("e3035fc1-9b23-49b5-90f7-470f79ad1d5b") },
                    { new Guid("e80a271d-97e9-4896-8442-4193e3d8f8c3"), "GCSRS", "managment/api/User/GetUser", false, new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("e70a846e-7a3c-4ab4-bd9d-5f8310c3a24d") },
                    { new Guid("ff0f7c6d-b778-41dc-9524-9d2961326ee6"), "CUSRA", "managment/api/User/CreateUser", false, new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), new Guid("e70a846e-7a3c-4ab4-bd9d-5f8310c3a24d") }
                });

            migrationBuilder.InsertData(
                table: "Security_UserGroupsLink",
                columns: new[] { "Group_Id", "User_Id" },
                values: new object[] { new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90") });

            migrationBuilder.InsertData(
                table: "Security_GroupPermissions",
                columns: new[] { "Id", "Group_Id", "Is_Deleted", "Link_Screen_Action_Id" },
                values: new object[,]
                {
                    { new Guid("08eb862e-814e-4de3-8827-34baba6c5868"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("48984f43-658b-4adf-8788-248f54d1faa0") },
                    { new Guid("0a1f7123-3068-453e-ae72-ec8f3f673eea"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("8fde4946-152d-45d5-96a0-e25d5f2cb274") },
                    { new Guid("13addee3-f52c-46f9-9e1b-94c44bd31d60"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("b6a4e50e-9ec8-45fb-ae22-807f10b2679e") },
                    { new Guid("32429140-179e-4df8-a2a0-7b7f187f3259"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("9741c3a7-167b-4731-92dd-6ec28780f520") },
                    { new Guid("444734f1-be36-4fab-b2c6-77e74386960e"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("7e7e47f6-961b-4c60-9b3a-62981dc83326") },
                    { new Guid("5f794278-2c3f-48d2-b7f0-f318dadd3331"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("0c2244d0-fc92-479f-9d57-ffe9a38a0246") },
                    { new Guid("852c6945-708d-4a3b-86a8-f13eb9bfc0ba"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("77c404d1-2f37-4728-9608-e9b19194ae07") },
                    { new Guid("b2b92c00-f4d0-4bea-9e48-e153e71f3649"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("ff0f7c6d-b778-41dc-9524-9d2961326ee6") },
                    { new Guid("b33feaef-5eb5-406f-8d75-3cd783eec7fb"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("e80a271d-97e9-4896-8442-4193e3d8f8c3") },
                    { new Guid("d096d0b6-6d8b-4d45-af7e-0263cb10915f"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("1f68dc32-021a-43fd-8ee1-023c1ce402a4") },
                    { new Guid("e001ef51-8af3-4bfa-87a7-361c89b8060b"), new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b"), false, new Guid("e4ffa587-2100-4f64-8794-29c9162d4ce8") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Common_LinkScreenActions_Screen_Action_Id",
                table: "Common_LinkScreenActions",
                column: "Screen_Action_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Common_LinkScreenActions_Screen_Id",
                table: "Common_LinkScreenActions",
                column: "Screen_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Common_Screens_Main_Module_Id",
                table: "Common_Screens",
                column: "Main_Module_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Common_Screens_Parent_Screen_Id",
                table: "Common_Screens",
                column: "Parent_Screen_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSMSHistory_Template_Id",
                table: "EmailSMSHistory",
                column: "Template_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSMSTemplates_Screen_Id",
                table: "EmailSMSTemplates",
                column: "Screen_Id");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalAttachments_CreatedBy",
                table: "GlobalAttachments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Lookups_Category_Code",
                table: "Lookups",
                column: "Category_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Created_By",
                table: "Notifications",
                column: "Created_By");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUsers_Notification_Id",
                table: "NotificationUsers",
                column: "Notification_Id");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUsers_Reciever_Id",
                table: "NotificationUsers",
                column: "Reciever_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_AccessLogs_Action_Code",
                table: "Security_AccessLogs",
                column: "Action_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Security_AccessLogs_Main_Module_Id",
                table: "Security_AccessLogs",
                column: "Main_Module_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_AccessLogs_User_Id",
                table: "Security_AccessLogs",
                column: "User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_GroupPermissions_Group_Id",
                table: "Security_GroupPermissions",
                column: "Group_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_GroupPermissions_Link_Screen_Action_Id",
                table: "Security_GroupPermissions",
                column: "Link_Screen_Action_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_MasterData_Master_Data_Parent_Id",
                table: "Security_MasterData",
                column: "Master_Data_Parent_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_UserGroups_User_Type",
                table: "Security_UserGroups",
                column: "User_Type");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Security_UserGroups",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Security_UserGroupsLink_Group_Id",
                table: "Security_UserGroupsLink",
                column: "Group_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_UserPermissions_Link_Screen_Action_Id",
                table: "Security_UserPermissions",
                column: "Link_Screen_Action_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_UserPermissions_User_Id",
                table: "Security_UserPermissions",
                column: "User_Id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Security_Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Security_Users_User_Type",
                table: "Security_Users",
                column: "User_Type");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Security_Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SysSetting_DataTypeMasterDataId",
                table: "SysSetting",
                column: "DataTypeMasterDataId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginLogs_UserId",
                table: "UserLoginLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ShortCuts_IconMasterDataId",
                table: "Users_ShortCuts",
                column: "IconMasterDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ShortCuts_UserID",
                table: "Users_ShortCuts",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EmailSMSHistory");

            migrationBuilder.DropTable(
                name: "GlobalAttachments");

            migrationBuilder.DropTable(
                name: "Lookups");

            migrationBuilder.DropTable(
                name: "NotificationUsers");

            migrationBuilder.DropTable(
                name: "Security_AccessLogs");

            migrationBuilder.DropTable(
                name: "Security_GroupPermissions");

            migrationBuilder.DropTable(
                name: "Security_UserGroupsLink");

            migrationBuilder.DropTable(
                name: "Security_UserPermissions");

            migrationBuilder.DropTable(
                name: "SysSetting");

            migrationBuilder.DropTable(
                name: "UserLoginLogs");

            migrationBuilder.DropTable(
                name: "Users_ShortCuts");

            migrationBuilder.DropTable(
                name: "VW_UserActions");

            migrationBuilder.DropTable(
                name: "EmailSMSTemplates");

            migrationBuilder.DropTable(
                name: "LookupCategories");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Security_UserGroups");

            migrationBuilder.DropTable(
                name: "Common_LinkScreenActions");

            migrationBuilder.DropTable(
                name: "Security_Users");

            migrationBuilder.DropTable(
                name: "Common_ScreenActions");

            migrationBuilder.DropTable(
                name: "Common_Screens");

            migrationBuilder.DropTable(
                name: "Security_MasterData");

            migrationBuilder.DropTable(
                name: "Common_MainModules");
        }
    }
}
