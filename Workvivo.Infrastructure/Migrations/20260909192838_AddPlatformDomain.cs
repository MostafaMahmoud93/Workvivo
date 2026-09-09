using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Workvivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description_Ar",
                table: "Common_LinkScreenActions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description_En",
                table: "Common_LinkScreenActions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "Common_LinkScreenActions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Permission_Key",
                table: "Common_LinkScreenActions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Audit_Logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Entity_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Entity_Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Old_Values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    New_Values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Affected_Columns = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Ip_Address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    User_Agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Correlation_Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audit_Logs_Security_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Parent_Category_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Categories_Doc_Categories_Parent_Category_Id",
                        column: x => x.Parent_Category_Id,
                        principalTable: "Doc_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Storage_Provider = table.Column<int>(type: "int", nullable: false),
                    Storage_Key = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Original_File_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content_Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Size_Bytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum_Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Scan_Status = table.Column<int>(type: "int", nullable: false),
                    Scanned_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Uploaded_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Uploaded_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Container = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Files", x => x.Id);
                    table.CheckConstraint("CK_Doc_Files_SizePositive", "[Size_Bytes] > 0");
                    table.ForeignKey(
                        name: "FK_Doc_Files_Security_Users_Uploaded_By",
                        column: x => x.Uploaded_By,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Org_Interests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Is_Approved = table.Column<bool>(type: "bit", nullable: false),
                    Usage_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Interests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Org_Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Legal_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Logo_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Org_Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Is_Approved = table.Column<bool>(type: "bit", nullable: false),
                    Usage_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rec_RecognitionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Badge_Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Badge_Color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Default_Points = table.Column<int>(type: "int", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_RecognitionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Security_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token_Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Family_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Expires_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Revoked_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Revoked_Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Replaced_By_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Created_User_Agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Revoked_Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_RefreshTokens_Security_RefreshTokens_Replaced_By_Id",
                        column: x => x.Replaced_By_Id,
                        principalTable: "Security_RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Security_RefreshTokens_Security_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Srv_Surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Is_Anonymous = table.Column<bool>(type: "bit", nullable: false),
                    Allow_Multiple_Responses = table.Column<bool>(type: "bit", nullable: false),
                    Allow_Partial_Save = table.Column<bool>(type: "bit", nullable: false),
                    Start_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    End_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Response_Count = table.Column<int>(type: "int", nullable: false),
                    Invited_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srv_Surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Org_JobTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Organization_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_JobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_JobTitles_Org_Organizations_Organization_Id",
                        column: x => x.Organization_Id,
                        principalTable: "Org_Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Org_Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Organization_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Address_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TimeZone_Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_Locations_Org_Organizations_Organization_Id",
                        column: x => x.Organization_Id,
                        principalTable: "Org_Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Srv_Audiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Survey_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Audience_Type = table.Column<int>(type: "int", nullable: false),
                    Target_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Audience_Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srv_Audiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Srv_Audiences_Srv_Surveys_Survey_Id",
                        column: x => x.Survey_Id,
                        principalTable: "Srv_Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Srv_Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Survey_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question_Type = table.Column<int>(type: "int", nullable: false),
                    Text_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Help_Text_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Help_Text_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Is_Required = table.Column<bool>(type: "bit", nullable: false),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Min_Value = table.Column<int>(type: "int", nullable: true),
                    Max_Value = table.Column<int>(type: "int", nullable: true),
                    Min_Label_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Min_Label_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Max_Label_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Max_Label_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srv_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Srv_Questions_Srv_Surveys_Survey_Id",
                        column: x => x.Survey_Id,
                        principalTable: "Srv_Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Srv_QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srv_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Srv_QuestionOptions_Srv_Questions_Question_Id",
                        column: x => x.Question_Id,
                        principalTable: "Srv_Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comm_Communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Logo_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cover_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Privacy = table.Column<int>(type: "int", nullable: false),
                    Owner_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Members_Count = table.Column<int>(type: "int", nullable: false),
                    Posts_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Featured = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comm_Communities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comm_Communities_Doc_Files_Cover_File_Id",
                        column: x => x.Cover_File_Id,
                        principalTable: "Doc_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comm_Communities_Doc_Files_Logo_File_Id",
                        column: x => x.Logo_File_Id,
                        principalTable: "Doc_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comm_Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Community_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Invited_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Invited_By_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Expires_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Responded_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comm_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comm_Invitations_Comm_Communities_Community_Id",
                        column: x => x.Community_Id,
                        principalTable: "Comm_Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comm_Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Community_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Member_Role = table.Column<int>(type: "int", nullable: false),
                    Membership_Status = table.Column<int>(type: "int", nullable: false),
                    Requested_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Joined_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reviewed_By_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reviewed_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notifications_Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comm_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comm_Members_Comm_Communities_Community_Id",
                        column: x => x.Community_Id,
                        principalTable: "Comm_Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Audiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Document_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Audience_Type = table.Column<int>(type: "int", nullable: false),
                    Target_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Audience_Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Audiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Current_Version_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Owner_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Published = table.Column<bool>(type: "bit", nullable: false),
                    Published_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Review_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Download_Count = table.Column<int>(type: "int", nullable: false),
                    Version_Count = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_Documents_Doc_Categories_Category_Id",
                        column: x => x.Category_Id,
                        principalTable: "Doc_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_Versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Document_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version_Number = table.Column<int>(type: "int", nullable: false),
                    Change_Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Effective_From = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_Versions", x => x.Id);
                    table.CheckConstraint("CK_Doc_Versions_NumberPositive", "[Version_Number] > 0");
                    table.ForeignKey(
                        name: "FK_Doc_Versions_Doc_Documents_Document_Id",
                        column: x => x.Document_Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_Versions_Doc_Files_File_Id",
                        column: x => x.File_Id,
                        principalTable: "Doc_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doc_DownloadLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Document_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Downloaded_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ip_Address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    User_Agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_DownloadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doc_DownloadLogs_Doc_Documents_Document_Id",
                        column: x => x.Document_Id,
                        principalTable: "Doc_Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doc_DownloadLogs_Doc_Versions_Version_Id",
                        column: x => x.Version_Id,
                        principalTable: "Doc_Versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evt_Attendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Event_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Response = table.Column<int>(type: "int", nullable: false),
                    Responded_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Checked_In_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evt_Attendees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evt_Audiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Event_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Audience_Type = table.Column<int>(type: "int", nullable: false),
                    Target_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Audience_Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evt_Audiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evt_Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Event_Type = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Start_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeZone_Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Is_All_Day = table.Column<bool>(type: "bit", nullable: false),
                    Location_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Address_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Address_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Meeting_Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Banner_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Organizer_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Community_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    Attendees_Count = table.Column<int>(type: "int", nullable: false),
                    Requires_Rsvp = table.Column<bool>(type: "bit", nullable: false),
                    Reminder_Sent_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evt_Events", x => x.Id);
                    table.CheckConstraint("CK_Evt_Events_CapacityPositive", "[Capacity] IS NULL OR [Capacity] > 0");
                    table.CheckConstraint("CK_Evt_Events_EndAfterStart", "[End_At] >= [Start_At]");
                    table.ForeignKey(
                        name: "FK_Evt_Events_Comm_Communities_Community_Id",
                        column: x => x.Community_Id,
                        principalTable: "Comm_Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evt_Events_Doc_Files_Banner_File_Id",
                        column: x => x.Banner_File_Id,
                        principalTable: "Doc_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evt_Events_Org_Locations_Location_Id",
                        column: x => x.Location_Id,
                        principalTable: "Org_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feed_CommentMentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comment_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mentioned_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text_Offset = table.Column<int>(type: "int", nullable: true),
                    Text_Length = table.Column<int>(type: "int", nullable: true),
                    Mentioned_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_CommentMentions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feed_CommentReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comment_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reaction_Type = table.Column<int>(type: "int", nullable: false),
                    Reacted_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_CommentReactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feed_Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Author_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Parent_Comment_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    Content_Html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content_Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reactions_Count = table.Column<int>(type: "int", nullable: false),
                    Replies_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Edited = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_Comments", x => x.Id);
                    table.CheckConstraint("CK_Feed_Comments_MaxDepth", "[Depth] >= 0 AND [Depth] <= 2");
                    table.ForeignKey(
                        name: "FK_Feed_Comments_Feed_Comments_Parent_Comment_Id",
                        column: x => x.Parent_Comment_Id,
                        principalTable: "Feed_Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feed_PostAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attachment_Type = table.Column<int>(type: "int", nullable: false),
                    File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Link_Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Link_Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Link_Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Link_Image_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_PostAttachments", x => x.Id);
                    table.CheckConstraint("CK_Feed_PostAttachments_FileOrLink", "([File_Id] IS NOT NULL AND [Link_Url] IS NULL) OR ([File_Id] IS NULL AND [Link_Url] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Feed_PostAttachments_Doc_Files_File_Id",
                        column: x => x.File_Id,
                        principalTable: "Doc_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feed_PostAudiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Audience_Type = table.Column<int>(type: "int", nullable: false),
                    Target_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Audience_Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_PostAudiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feed_PostMentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mentioned_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text_Offset = table.Column<int>(type: "int", nullable: true),
                    Text_Length = table.Column<int>(type: "int", nullable: true),
                    Mentioned_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_PostMentions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feed_PostReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reaction_Type = table.Column<int>(type: "int", nullable: false),
                    Reacted_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_PostReactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feed_Posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Author_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Community_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Post_Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content_Html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content_Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Is_Pinned = table.Column<bool>(type: "bit", nullable: false),
                    Is_Featured = table.Column<bool>(type: "bit", nullable: false),
                    Is_Official = table.Column<bool>(type: "bit", nullable: false),
                    Comments_Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Published_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scheduled_Publish_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Archived_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reactions_Count = table.Column<int>(type: "int", nullable: false),
                    Comments_Count = table.Column<int>(type: "int", nullable: false),
                    Views_Count = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feed_Posts_Comm_Communities_Community_Id",
                        column: x => x.Community_Id,
                        principalTable: "Comm_Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Poll_Polls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Question_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Question_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Is_Multiple_Choice = table.Column<bool>(type: "bit", nullable: false),
                    Is_Anonymous = table.Column<bool>(type: "bit", nullable: false),
                    Show_Results_Before_Voting = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Start_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Expiry_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Total_Votes = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poll_Polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poll_Polls_Feed_Posts_Post_Id",
                        column: x => x.Post_Id,
                        principalTable: "Feed_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Poll_Audiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Poll_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Audience_Type = table.Column<int>(type: "int", nullable: false),
                    Target_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Audience_Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poll_Audiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poll_Audiences_Poll_Polls_Poll_Id",
                        column: x => x.Poll_Id,
                        principalTable: "Poll_Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Poll_Options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Poll_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Votes_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poll_Options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poll_Options_Poll_Polls_Poll_Id",
                        column: x => x.Poll_Id,
                        principalTable: "Poll_Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feed_PostViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    First_Viewed_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Viewed_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    View_Count = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feed_PostViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feed_PostViews_Feed_Posts_Post_Id",
                        column: x => x.Post_Id,
                        principalTable: "Feed_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notif_Preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notification_Type = table.Column<int>(type: "int", nullable: false),
                    In_App_Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Email_Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Push_Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Email_Frequency = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notif_Preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Org_Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Organization_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Parent_Department_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Manager_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(900)", maxLength: 900, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_Departments_Org_Departments_Parent_Department_Id",
                        column: x => x.Parent_Department_Id,
                        principalTable: "Org_Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Org_Departments_Org_Organizations_Organization_Id",
                        column: x => x.Organization_Id,
                        principalTable: "Org_Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Org_EmployeeFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Follower_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Followee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Followed_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_EmployeeFollowers", x => x.Id);
                    table.CheckConstraint("CK_Org_EmployeeFollowers_NotSelf", "[Follower_Id] <> [Followee_Id]");
                });

            migrationBuilder.CreateTable(
                name: "Org_EmployeeInterests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Interest_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Added_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_EmployeeInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_EmployeeInterests_Org_Interests_Interest_Id",
                        column: x => x.Interest_Id,
                        principalTable: "Org_Interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Org_EmployeeManagers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Manager_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Is_Primary = table.Column<bool>(type: "bit", nullable: false),
                    Effective_From = table.Column<DateOnly>(type: "date", nullable: false),
                    Effective_To = table.Column<DateOnly>(type: "date", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_EmployeeManagers", x => x.Id);
                    table.CheckConstraint("CK_Org_EmployeeManagers_NotSelf", "[Employee_Id] <> [Manager_Id]");
                });

            migrationBuilder.CreateTable(
                name: "Org_Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    User_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    First_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Middle_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Last_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Display_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Full_Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Profile_Picture_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cover_Picture_File_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Department_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Team_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Location_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Job_Title_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Biography_Ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Biography_En = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Joining_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Birth_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Show_Birthday = table.Column<bool>(type: "bit", nullable: false),
                    Preferred_Language = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Followers_Count = table.Column<int>(type: "int", nullable: false),
                    Following_Count = table.Column<int>(type: "int", nullable: false),
                    Recognition_Points = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_Employees_Org_Departments_Department_Id",
                        column: x => x.Department_Id,
                        principalTable: "Org_Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Org_Employees_Org_JobTitles_Job_Title_Id",
                        column: x => x.Job_Title_Id,
                        principalTable: "Org_JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Org_Employees_Org_Locations_Location_Id",
                        column: x => x.Location_Id,
                        principalTable: "Org_Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Org_Employees_Security_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Security_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Org_EmployeeSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Skill_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endorsement_Count = table.Column<int>(type: "int", nullable: false),
                    Sort_Order = table.Column<int>(type: "int", nullable: false),
                    Added_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_EmployeeSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_EmployeeSkills_Org_Employees_Employee_Id",
                        column: x => x.Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Org_EmployeeSkills_Org_Skills_Skill_Id",
                        column: x => x.Skill_Id,
                        principalTable: "Org_Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Org_Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Department_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Lead_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Org_Teams_Org_Departments_Department_Id",
                        column: x => x.Department_Id,
                        principalTable: "Org_Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Org_Teams_Org_Employees_Lead_Employee_Id",
                        column: x => x.Lead_Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Poll_Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Poll_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Option_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Voter_Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Voted_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poll_Votes", x => x.Id);
                    table.CheckConstraint("CK_Poll_Votes_OneVoterIdentity", "([Employee_Id] IS NOT NULL AND [Voter_Hash] IS NULL) OR ([Employee_Id] IS NULL AND [Voter_Hash] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Poll_Votes_Org_Employees_Employee_Id",
                        column: x => x.Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Poll_Votes_Poll_Options_Option_Id",
                        column: x => x.Option_Id,
                        principalTable: "Poll_Options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Poll_Votes_Poll_Polls_Poll_Id",
                        column: x => x.Poll_Id,
                        principalTable: "Poll_Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rec_LeaderboardSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<int>(type: "int", nullable: false),
                    Period_Start = table.Column<DateOnly>(type: "date", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Recognition_Count = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Department_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Generated_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_LeaderboardSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rec_LeaderboardSnapshots_Org_Departments_Department_Id",
                        column: x => x.Department_Id,
                        principalTable: "Org_Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rec_LeaderboardSnapshots_Org_Employees_Employee_Id",
                        column: x => x.Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rec_Recognitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sender_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient_Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recognition_Type_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Post_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Recognised_On = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Last_Modified_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Last_Modify_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rec_Recognitions", x => x.Id);
                    table.CheckConstraint("CK_Rec_Recognitions_NotSelf", "[Sender_Employee_Id] <> [Recipient_Employee_Id]");
                    table.CheckConstraint("CK_Rec_Recognitions_PointsNotNegative", "[Points] >= 0");
                    table.ForeignKey(
                        name: "FK_Rec_Recognitions_Feed_Posts_Post_Id",
                        column: x => x.Post_Id,
                        principalTable: "Feed_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rec_Recognitions_Org_Employees_Recipient_Employee_Id",
                        column: x => x.Recipient_Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rec_Recognitions_Org_Employees_Sender_Employee_Id",
                        column: x => x.Sender_Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rec_Recognitions_Rec_RecognitionTypes_Recognition_Type_Id",
                        column: x => x.Recognition_Type_Id,
                        principalTable: "Rec_RecognitionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Srv_Responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Survey_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Employee_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Respondent_Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Started_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Submitted_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Is_Complete = table.Column<bool>(type: "bit", nullable: false),
                    Department_Id_At_Submission = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Location_Id_At_Submission = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srv_Responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Srv_Responses_Org_Employees_Employee_Id",
                        column: x => x.Employee_Id,
                        principalTable: "Org_Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Srv_Responses_Srv_Surveys_Survey_Id",
                        column: x => x.Survey_Id,
                        principalTable: "Srv_Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Srv_Answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Response_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Option_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Text_Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Numeric_Value = table.Column<int>(type: "int", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srv_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Srv_Answers_Srv_QuestionOptions_Option_Id",
                        column: x => x.Option_Id,
                        principalTable: "Srv_QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Srv_Answers_Srv_Questions_Question_Id",
                        column: x => x.Question_Id,
                        principalTable: "Srv_Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Srv_Answers_Srv_Responses_Response_Id",
                        column: x => x.Response_Id,
                        principalTable: "Srv_Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("0c2244d0-fc92-479f-9d57-ffe9a38a0246"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("1f68dc32-021a-43fd-8ee1-023c1ce402a4"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("48984f43-658b-4adf-8788-248f54d1faa0"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("77c404d1-2f37-4728-9608-e9b19194ae07"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("7e7e47f6-961b-4c60-9b3a-62981dc83326"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("8fde4946-152d-45d5-96a0-e25d5f2cb274"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("9741c3a7-167b-4731-92dd-6ec28780f520"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("b6a4e50e-9ec8-45fb-ae22-807f10b2679e"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("e4ffa587-2100-4f64-8794-29c9162d4ce8"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("e80a271d-97e9-4896-8442-4193e3d8f8c3"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("ff0f7c6d-b778-41dc-9524-9d2961326ee6"),
                columns: new[] { "Description_Ar", "Description_En", "Module", "Permission_Key" },
                values: new object[] { null, null, null, null });

            migrationBuilder.InsertData(
                table: "Common_Screens",
                columns: new[] { "Id", "Is_Branch", "Is_Deleted", "Link", "Main_Module_Id", "Menu_Or_Not", "No_Login", "Order", "Parent_Screen_Id", "Screen_Description_Ar", "Screen_Description_En", "Screen_Icon" },
                values: new object[,]
                {
                    { new Guid("4c0ed04b-2a78-d214-c388-597859b8fb7d"), false, false, "admin/analytics", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 90, null, "التحليلات", "Analytics", "heroicons_outline:chart-bar" },
                    { new Guid("69d997ef-d97f-b6f8-aa2b-48b755da367e"), false, false, "events", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 70, null, "الفعاليات", "Events", "heroicons_outline:calendar-days" },
                    { new Guid("6d80f82b-fe84-782d-021b-5529e17c1310"), false, false, "admin/organization", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 30, null, "الهيكل التنظيمي", "Organization", "heroicons_outline:building-office" },
                    { new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a"), false, false, "feed", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 10, null, "الأخبار", "Feed", "heroicons_outline:newspaper" },
                    { new Guid("8b85f185-e437-e3b7-f7f7-8fa5a24121ae"), false, false, "admin/audit", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 100, null, "سجل التدقيق", "Audit Log", "heroicons_outline:shield-check" },
                    { new Guid("90b35569-6ef8-72b0-7b4d-0a6c5e24d854"), false, false, "recognition", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 50, null, "التقدير", "Recognition", "heroicons_outline:trophy" },
                    { new Guid("a0674b4c-3bb0-bae3-6812-081e5a6f068a"), false, false, "documents", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 80, null, "المستندات", "Documents", "heroicons_outline:document-text" },
                    { new Guid("ae006910-2221-5849-3f4d-2bdf981533d2"), false, false, "employees", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 20, null, "الموظفون", "Employees", "heroicons_outline:users" },
                    { new Guid("d62256dc-a055-31bd-4443-a86e7e3ff2d3"), false, false, "communities", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 40, null, "المجتمعات", "Communities", "heroicons_outline:user-group" },
                    { new Guid("e88a15e8-ef0a-3b68-29f5-75027d5589d4"), false, false, "surveys", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 60, null, "الاستبيانات", "Surveys", "heroicons_outline:clipboard-document-check" },
                    { new Guid("fe7f91bf-b88a-4d9c-ab5f-2a85d48619d0"), false, false, "admin/settings", new Guid("986b68ac-22ff-4cee-991d-dbdfe4b12bbb"), true, true, 110, null, "الإعدادات", "Settings", "heroicons_outline:cog-6-tooth" }
                });

            migrationBuilder.InsertData(
                table: "Doc_Categories",
                columns: new[] { "Id", "Create_Date", "Created_By", "Description_Ar", "Description_En", "Icon", "Is_Active", "Is_Deleted", "Last_Modified_By", "Last_Modify_Date", "Name_Ar", "Name_En", "Parent_Category_Id", "Sort_Order" },
                values: new object[,]
                {
                    { new Guid("5f3d48a4-ab98-f815-c355-c07fade87322"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), null, null, "document-duplicate", true, false, null, null, "النماذج", "Forms", null, 3 },
                    { new Guid("6ac47e05-7465-7846-1ef7-e72eb545b96b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), null, null, "identification", true, false, null, null, "سياسات الموارد البشرية", "HR Policies", null, 1 },
                    { new Guid("7c6c3c36-ed2d-04b3-9a63-a7b0a3697c99"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), null, null, "academic-cap", true, false, null, null, "التدريب", "Training", null, 5 },
                    { new Guid("8a4d4bb1-d57e-fcf8-91da-c422bb1ee4a0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), null, null, "computer-desktop", true, false, null, null, "سياسات تقنية المعلومات", "IT Policies", null, 2 },
                    { new Guid("d6f3a0d5-e2ec-5b10-bee1-c650cd2aa99d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), null, null, "building-office", true, false, null, null, "مستندات الشركة", "Company Documents", null, 6 },
                    { new Guid("e1569f4a-afa9-a13e-c2ac-d2b5f2df04c7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), null, null, "book-open", true, false, null, null, "الإرشادات", "Guidelines", null, 4 }
                });

            migrationBuilder.InsertData(
                table: "Rec_RecognitionTypes",
                columns: new[] { "Id", "Badge_Color", "Badge_Icon", "Code", "Create_Date", "Created_By", "Default_Points", "Description_Ar", "Description_En", "Is_Active", "Is_Deleted", "Last_Modified_By", "Last_Modify_Date", "Name_Ar", "Name_En", "Sort_Order" },
                values: new object[,]
                {
                    { new Guid("1470a250-47fa-d834-d742-6d2741cc67cd"), "#7a2ac8", "light-bulb", "INNO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), 40, null, null, true, false, null, null, "الابتكار", "Innovation", 3 },
                    { new Guid("4b441b70-dfae-4109-80c1-688f188e9180"), "#c8892a", "trophy", "PERF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), 50, null, null, true, false, null, null, "أداء متميز", "Outstanding Performance", 1 },
                    { new Guid("b21ede46-559b-d372-5e7f-38b9863db6d2"), "#2ac88a", "rocket-launch", "ABOV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), 60, null, null, true, false, null, null, "تجاوز التوقعات", "Going Above and Beyond", 6 },
                    { new Guid("d4d6c123-c797-eccd-c200-8848de399c46"), "#c82a5a", "heart", "CUST", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), 40, null, null, true, false, null, null, "تميز خدمة العملاء", "Customer Excellence", 4 },
                    { new Guid("da1489b1-7624-7975-490f-274ee0065db3"), "#1f4e79", "flag", "LEAD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), 50, null, null, true, false, null, null, "القيادة", "Leadership", 5 },
                    { new Guid("dc2385a2-adca-5440-4c2d-89094e3f0681"), "#2a7ac8", "users", "TEAM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), 30, null, null, true, false, null, null, "روح الفريق", "Teamwork", 2 }
                });

            migrationBuilder.InsertData(
                table: "Security_UserGroups",
                columns: new[] { "Id", "ConcurrencyStamp", "Create_Date", "Created_By", "Is_Active", "Is_Deleted", "Last_Modify_By", "Last_Modify_Date", "Name", "Name_Ar", "Name_En", "NormalizedName", "User_Type" },
                values: new object[,]
                {
                    { new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), "a26a3cc1-4088-908e-5079-63378a4adabe", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "Admin", "مسؤول", "Admin", "ADMIN", "MANAG" },
                    { new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), "ac9fe7f2-31eb-2dd6-3e74-08f9bc22dcc4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "Moderator", "مشرف المحتوى", "Moderator", "MODERATOR", "MANAG" },
                    { new Guid("6edc92ec-3fde-2111-be7c-4429eacaf5b3"), "11ad2a19-c2f3-5202-ef67-25823fd56176", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "Employee", "موظف", "Employee", "EMPLOYEE", "MANAG" },
                    { new Guid("82592ca4-4700-a58d-0130-901224a206be"), "0c9877f9-91d9-41ad-1573-8ae4cb67a6e5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "Communications Manager", "مدير الاتصال", "Communications Manager", "COMMUNICATIONSMANAGER", "MANAG" },
                    { new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), "e5f5dbd3-dac4-8ced-0f1c-9dda3c1e16d5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "Department Manager", "مدير إدارة", "Department Manager", "DEPARTMENTMANAGER", "MANAG" },
                    { new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), "5b2f5c98-7ea1-ef2d-c4d9-5c4969cb004f", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "Super Admin", "مدير النظام", "Super Admin", "SUPERADMIN", "MANAG" },
                    { new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), "a948924b-022d-7781-766c-e6a3315ca248", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90"), true, false, null, null, "HR", "الموارد البشرية", "HR", "HR", "MANAG" }
                });

            migrationBuilder.InsertData(
                table: "Common_LinkScreenActions",
                columns: new[] { "Id", "Action_Code", "Base_Route", "Description_Ar", "Description_En", "Is_Deleted", "Module", "Permission_Key", "Screen_Action_Id", "Screen_Id" },
                values: new object[,]
                {
                    { new Guid("14185243-001d-de0c-2c44-3450e4a9c34a"), "70CF2", "", "تعديل منشور", "Edit posts", false, "Feed", "Post.Edit", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd"), "36FA0", "", "إدارة الفعاليات", "Manage events", false, "Events", "Event.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("69d997ef-d97f-b6f8-aa2b-48b755da367e") },
                    { new Guid("19b718ef-ebd6-cfa6-f792-28183217f320"), "8377F", "", "إدارة الأدوار والصلاحيات", "Manage roles and permissions", false, "Security", "Role.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("fe7f91bf-b88a-4d9c-ab5f-2a85d48619d0") },
                    { new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c"), "66477", "", "عرض الموظفين", "View employees", false, "Employees", "Employee.View", new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("ae006910-2221-5849-3f4d-2bdf981533d2") },
                    { new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6"), "D94EC", "", "عرض المنشورات", "View posts", false, "Feed", "Post.View", new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("41794d60-8695-75ab-9012-235d173405e9"), "C55B0", "", "إدارة التقدير", "Manage recognition", false, "Recognition", "Recognition.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("90b35569-6ef8-72b0-7b4d-0a6c5e24d854") },
                    { new Guid("437392f2-83d3-41ae-6576-3808806524b5"), "41C5C", "", "إدارة المستندات", "Manage documents", false, "Documents", "Document.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("a0674b4c-3bb0-bae3-6812-081e5a6f068a") },
                    { new Guid("455498dc-69c8-94fb-e158-94820f4c13ab"), "81DE9", "", "إدارة الإعدادات", "Manage settings", false, "Settings", "Settings.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("fe7f91bf-b88a-4d9c-ab5f-2a85d48619d0") },
                    { new Guid("53015a08-7ad0-0afb-1b75-fb38207d0e97"), "FA521", "", "إدارة الاستطلاعات", "Manage polls", false, "Surveys", "Poll.Manage", new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), new Guid("e88a15e8-ef0a-3b68-29f5-75027d5589d4") },
                    { new Guid("5c168320-a722-fa1f-3ff4-746c5d77a2a4"), "CC232", "", "إضافة موظف", "Create employees", false, "Employees", "Employee.Create", new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), new Guid("ae006910-2221-5849-3f4d-2bdf981533d2") },
                    { new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0"), "41555", "", "عرض المستندات", "View documents", false, "Documents", "Document.View", new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("a0674b4c-3bb0-bae3-6812-081e5a6f068a") },
                    { new Guid("76e00ce9-38a6-eb68-07dc-f6cc4dfe50e1"), "C6D8F", "", "إنشاء إعلان", "Create announcements", false, "Feed", "Announcement.Create", new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("890a1f0e-784c-130c-b87b-c93da74d78c0"), "FB53F", "", "عرض سجل التدقيق", "View the audit log", false, "Security", "AuditLog.View", new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("8b85f185-e437-e3b7-f7f7-8fa5a24121ae") },
                    { new Guid("91f92fed-6fe7-0a48-e3bd-e70330dcbe61"), "3314E", "", "إدارة المحتوى", "Moderate posts", false, "Feed", "Post.Moderate", new Guid("8a231277-b772-4c59-844e-54435dc912f1"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb"), "170CC", "", "إنشاء منشور", "Create posts", false, "Feed", "Post.Create", new Guid("f820b435-d621-41a8-80e4-32d5d1f867c7"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("a0568325-97ec-fa2b-8efb-96227bb79739"), "5E452", "", "عرض التحليلات", "View analytics", false, "Analytics", "Analytics.View", new Guid("6ebd6ae4-7f43-45b6-9d57-aa5067a4b79f"), new Guid("4c0ed04b-2a78-d214-c388-597859b8fb7d") },
                    { new Guid("a242f278-f23d-95b0-0a2b-4edfb6431dc3"), "B6EC3", "", "إدارة الاستبيانات", "Manage surveys", false, "Surveys", "Survey.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("e88a15e8-ef0a-3b68-29f5-75027d5589d4") },
                    { new Guid("c456a7f0-68e1-7a1e-bb2d-8bf5d970a87e"), "8346B", "", "تعديل موظف", "Edit employees", false, "Employees", "Employee.Edit", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("ae006910-2221-5849-3f4d-2bdf981533d2") },
                    { new Guid("c7522cf4-6a02-cf1a-3b23-f921a35da5da"), "191AA", "", "حذف منشور", "Delete posts", false, "Feed", "Post.Delete", new Guid("328723da-e3c9-468e-998d-6adea7217d37"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("d029ea0e-e268-4181-bae4-81d2e1a2d81c"), "89808", "", "إدارة الهيكل التنظيمي", "Manage the organisation", false, "Organization", "Organization.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("6d80f82b-fe84-782d-021b-5529e17c1310") },
                    { new Guid("d2609c46-dadb-8d1d-9554-ea3bc9a496e0"), "CFD43", "", "نشر إعلان", "Publish announcements", false, "Feed", "Announcement.Publish", new Guid("8a231277-b772-4c59-844e-54435dc912f1"), new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a") },
                    { new Guid("d70facd0-829d-83f9-620b-a96c53ae3052"), "E44EC", "", "حذف موظف", "Delete employees", false, "Employees", "Employee.Delete", new Guid("328723da-e3c9-468e-998d-6adea7217d37"), new Guid("ae006910-2221-5849-3f4d-2bdf981533d2") },
                    { new Guid("e5a48e33-902f-f907-ca77-683b32b60078"), "31C6F", "", "إدارة المجتمعات", "Manage communities", false, "Communities", "Community.Manage", new Guid("4add3d50-f32c-4abe-b8e2-3af596588a71"), new Guid("d62256dc-a055-31bd-4443-a86e7e3ff2d3") }
                });

            migrationBuilder.InsertData(
                table: "Security_GroupPermissions",
                columns: new[] { "Id", "Group_Id", "Is_Deleted", "Link_Screen_Action_Id" },
                values: new object[,]
                {
                    { new Guid("039ca671-1cd4-6bb4-755e-a235b00b130d"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("41794d60-8695-75ab-9012-235d173405e9") },
                    { new Guid("075e48c6-2f78-296c-bd27-664fded52de0"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("e5a48e33-902f-f907-ca77-683b32b60078") },
                    { new Guid("077b401b-1314-2dfe-7a3b-43541a4a8d78"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("437392f2-83d3-41ae-6576-3808806524b5") },
                    { new Guid("07f67616-a255-5cf4-46f9-45bdf5ebce10"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("0b11cc3f-7c12-3942-64ac-6caaf35c2212"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("0f7bb4da-e2fb-79fb-97b8-b7c02d36b68f"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("14185243-001d-de0c-2c44-3450e4a9c34a") },
                    { new Guid("0ff8c19f-ca74-e943-9c59-c82961c0ada0"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("119f1b43-6d61-c2ed-6739-0b9d1c9617d6"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("890a1f0e-784c-130c-b87b-c93da74d78c0") },
                    { new Guid("12dfb774-1551-381f-cf72-3d8010cec0e1"), new Guid("6edc92ec-3fde-2111-be7c-4429eacaf5b3"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("134d3988-2a27-e210-47d3-4a66ab656d95"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("143e7891-f934-aa92-4f61-77e2f6ddfd7c"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("a242f278-f23d-95b0-0a2b-4edfb6431dc3") },
                    { new Guid("1589e7ce-49ea-aeed-041b-b6a5d54bd328"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("437392f2-83d3-41ae-6576-3808806524b5") },
                    { new Guid("17838ce3-ebff-ca49-b387-015dd590918d"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("c7522cf4-6a02-cf1a-3b23-f921a35da5da") },
                    { new Guid("186e1ca9-a1e3-8f6e-df9f-62941c0a7417"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("455498dc-69c8-94fb-e158-94820f4c13ab") },
                    { new Guid("18dbdb02-4eea-dda2-0767-577a0881aa7b"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("a0568325-97ec-fa2b-8efb-96227bb79739") },
                    { new Guid("19bf8ae4-5e75-1674-403f-afb81b5e5be3"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("1b77d8c4-edf2-143a-b463-d2581f0636e6"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd") },
                    { new Guid("1da8b7ec-c7bb-bad2-da81-f79b6b077537"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("a0568325-97ec-fa2b-8efb-96227bb79739") },
                    { new Guid("1dc50366-6e44-0f8f-9d36-ae3a24ce28d4"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("a0568325-97ec-fa2b-8efb-96227bb79739") },
                    { new Guid("1f08f101-37f8-2c9c-482c-e023c86ac38d"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("890a1f0e-784c-130c-b87b-c93da74d78c0") },
                    { new Guid("2719fc3d-3f61-ee84-146c-fa9200b4c9a4"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("76e00ce9-38a6-eb68-07dc-f6cc4dfe50e1") },
                    { new Guid("3333398a-e5eb-5090-9257-634125273a58"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("e5a48e33-902f-f907-ca77-683b32b60078") },
                    { new Guid("34467b52-420c-e6cf-b2a9-8c92799c454e"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("3479273a-0f91-a663-570c-1b41659bae68"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("359112d8-9dd7-fd13-b6c3-1f04ddce33a3"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("76e00ce9-38a6-eb68-07dc-f6cc4dfe50e1") },
                    { new Guid("36ac6ac5-befe-c388-d0e3-7909829dfe23"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("3980f1a8-382a-ee49-ba95-4231fb8cf5b9"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("5c168320-a722-fa1f-3ff4-746c5d77a2a4") },
                    { new Guid("3b889690-570e-efdd-0890-e58add51a84b"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("14185243-001d-de0c-2c44-3450e4a9c34a") },
                    { new Guid("3d98f770-e098-262b-3c5c-6d090ce2251e"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("4226f87d-830f-4806-1045-ed566be2ebfb"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("4346be40-ad6d-9886-2e6e-0fd4838a3953"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("91f92fed-6fe7-0a48-e3bd-e70330dcbe61") },
                    { new Guid("4f991ea7-3a1f-6da4-f406-1357ef8deed5"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("520dc2e8-0d86-6e72-1bae-108bc788ccdc"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("c7522cf4-6a02-cf1a-3b23-f921a35da5da") },
                    { new Guid("52447140-bd94-73dd-7951-ab5c332c7242"), new Guid("6edc92ec-3fde-2111-be7c-4429eacaf5b3"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("5756f157-f1be-d1ee-bcd8-0ae7de604d24"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("d70facd0-829d-83f9-620b-a96c53ae3052") },
                    { new Guid("5b693340-66fa-6a66-621d-1da5e1630fcf"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("19b718ef-ebd6-cfa6-f792-28183217f320") },
                    { new Guid("5fc407f8-cc40-e6e1-66a4-fa2278073d18"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("455498dc-69c8-94fb-e158-94820f4c13ab") },
                    { new Guid("639482c5-9523-d880-636d-ef121318cb68"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("68ed50ae-6900-84ce-f9c7-657aa0208551"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("699440be-58e4-f982-bddf-87634fe174b6"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("6a6db383-03dc-9147-2f15-19057da7daa3"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("6d0888bd-692f-4b8d-a867-66702a026d2c"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("76e00ce9-38a6-eb68-07dc-f6cc4dfe50e1") },
                    { new Guid("759f6b9f-8c4c-d6c5-611a-60d76c91b8e0"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("7d44b0d7-d38e-4133-0df8-9b6e473b36a9"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("91f92fed-6fe7-0a48-e3bd-e70330dcbe61") },
                    { new Guid("7d9d2228-9f31-2bf9-d50e-5345b422a2da"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd") },
                    { new Guid("840e799d-c9cc-3826-843b-5d1ce9b8b210"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("437392f2-83d3-41ae-6576-3808806524b5") },
                    { new Guid("84a9a7f5-e40d-f63f-31b1-b3d716a2dbc7"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("87e6e22d-b8cc-c20a-8b98-46c2f18593c8"), new Guid("6edc92ec-3fde-2111-be7c-4429eacaf5b3"), false, new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0") },
                    { new Guid("8d68280c-5862-b898-5959-9240b156d5c8"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("e5a48e33-902f-f907-ca77-683b32b60078") },
                    { new Guid("9a0a0c78-45f7-30bd-bbdf-5a01d2315f6c"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("53015a08-7ad0-0afb-1b75-fb38207d0e97") },
                    { new Guid("9af95b38-8015-137e-fc91-64871456e17c"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("d2609c46-dadb-8d1d-9554-ea3bc9a496e0") },
                    { new Guid("9b6e13af-2bba-09b1-b80c-25c86c42a85d"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("c7522cf4-6a02-cf1a-3b23-f921a35da5da") },
                    { new Guid("9b8c18b7-3d0f-11dd-94c2-708ea1496615"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("c456a7f0-68e1-7a1e-bb2d-8bf5d970a87e") },
                    { new Guid("9bae457d-f946-8107-af4e-b6ba53a37191"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("9bf7067b-6abd-241a-f212-c18a3f48dde2"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("c456a7f0-68e1-7a1e-bb2d-8bf5d970a87e") },
                    { new Guid("9f5722aa-93d5-18c6-b4e5-2215d49f1f1b"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("5c168320-a722-fa1f-3ff4-746c5d77a2a4") },
                    { new Guid("a0a3300d-068d-b5bc-92e5-040da38d876f"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("d2609c46-dadb-8d1d-9554-ea3bc9a496e0") },
                    { new Guid("a5e65ac4-8421-e279-9653-cfea6c048b1d"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("a242f278-f23d-95b0-0a2b-4edfb6431dc3") },
                    { new Guid("a7e68fff-ac5e-11b8-18cf-8c12806d82ff"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("e5a48e33-902f-f907-ca77-683b32b60078") },
                    { new Guid("a8aeddca-9a19-2323-ad03-e90a15c25fc0"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c") },
                    { new Guid("a9d7211b-ba80-21d5-edf3-9300fcf2a0e5"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("c456a7f0-68e1-7a1e-bb2d-8bf5d970a87e") },
                    { new Guid("a9f570ea-0564-0215-1f29-5d3c3eff6d15"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("aadf49c3-4203-8d16-d99d-331441dad629"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("53015a08-7ad0-0afb-1b75-fb38207d0e97") },
                    { new Guid("aeb63851-ef8f-a9ee-0f95-00a54bf61707"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("53015a08-7ad0-0afb-1b75-fb38207d0e97") },
                    { new Guid("afa6f9b2-0cb2-6c39-d474-514442cf103a"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("41794d60-8695-75ab-9012-235d173405e9") },
                    { new Guid("b1bf9764-a5f5-a1c2-cfe5-368988def47e"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("d2609c46-dadb-8d1d-9554-ea3bc9a496e0") },
                    { new Guid("b4224465-2b45-48d6-d2aa-07bfc0433427"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("a242f278-f23d-95b0-0a2b-4edfb6431dc3") },
                    { new Guid("b8b9b657-ca10-ebae-2f33-cfc5cd3d507f"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd") },
                    { new Guid("b99f6864-0081-30e1-3370-caa08069ee2d"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("91f92fed-6fe7-0a48-e3bd-e70330dcbe61") },
                    { new Guid("bae60ba1-2fc1-ba2a-aeed-97edc4356665"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd") },
                    { new Guid("bb22e323-1c32-23b0-b57d-ab2a87b178a0"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("d029ea0e-e268-4181-bae4-81d2e1a2d81c") },
                    { new Guid("c32d0232-6a62-24b1-3513-d36da00aa0a8"), new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("c4a076e3-2e61-53ef-514e-5ec997000a50"), new Guid("6edc92ec-3fde-2111-be7c-4429eacaf5b3"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("cbd1c052-107e-cc37-bfa4-b9989cf3b75a"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("a0568325-97ec-fa2b-8efb-96227bb79739") },
                    { new Guid("d0077adb-e95b-43af-7f1a-1ade9704b603"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb") },
                    { new Guid("d0497125-a7e5-c610-258f-7764192464cc"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("d029ea0e-e268-4181-bae4-81d2e1a2d81c") },
                    { new Guid("d1e397f4-8fef-626c-1988-711ae1f25fef"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("a0568325-97ec-fa2b-8efb-96227bb79739") },
                    { new Guid("d3fde401-e8ab-42d7-411a-42619f3583b4"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("5c168320-a722-fa1f-3ff4-746c5d77a2a4") },
                    { new Guid("db5df094-313c-fd1c-75fd-a75dfba4c44a"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("d029ea0e-e268-4181-bae4-81d2e1a2d81c") },
                    { new Guid("dc5b8078-ee1f-ae3f-3f98-9a3df7f8b1a4"), new Guid("6a62833b-08c9-100a-0533-3298ab96d542"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("dcc96706-6ffd-fcc0-166c-e5d43b28b535"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("d2609c46-dadb-8d1d-9554-ea3bc9a496e0") },
                    { new Guid("dd87d1f0-7e7a-85e0-592d-8fe654fe0a11"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("41794d60-8695-75ab-9012-235d173405e9") },
                    { new Guid("e197a7c4-e779-0228-d029-b056ec0223bb"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6") },
                    { new Guid("e2d21735-fbe6-e016-c9ca-1e5379bfcb02"), new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"), false, new Guid("14185243-001d-de0c-2c44-3450e4a9c34a") },
                    { new Guid("e38bbc07-8d1e-80a2-437e-e169000dc64e"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("53015a08-7ad0-0afb-1b75-fb38207d0e97") },
                    { new Guid("e49d40b2-a2fe-0ff2-aa2e-99e23ff5e598"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("91f92fed-6fe7-0a48-e3bd-e70330dcbe61") },
                    { new Guid("f1854997-3808-2ab5-f207-630ae8dfa5ca"), new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"), false, new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd") },
                    { new Guid("f467cc44-4d20-970d-5a91-a3fd426c3b50"), new Guid("82592ca4-4700-a58d-0130-901224a206be"), false, new Guid("76e00ce9-38a6-eb68-07dc-f6cc4dfe50e1") },
                    { new Guid("f56636d2-e189-2074-0f6d-09f3e055ec53"), new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"), false, new Guid("41794d60-8695-75ab-9012-235d173405e9") }
                });

            migrationBuilder.CreateIndex(
                name: "UX_Common_LinkScreenActions_PermissionKey",
                table: "Common_LinkScreenActions",
                column: "Permission_Key",
                unique: true,
                filter: "[Permission_Key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Logs_Entity",
                table: "Audit_Logs",
                columns: new[] { "Entity_Name", "Entity_Id", "Timestamp" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Logs_Timeline",
                table: "Audit_Logs",
                columns: new[] { "Timestamp", "Action" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Logs_User",
                table: "Audit_Logs",
                columns: new[] { "User_Id", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Communities_Cover_File_Id",
                table: "Comm_Communities",
                column: "Cover_File_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Communities_Discovery",
                table: "Comm_Communities",
                columns: new[] { "Is_Deleted", "Is_Active", "Privacy" })
                .Annotation("SqlServer:Include", new[] { "Name_Ar", "Name_En", "Members_Count", "Is_Featured" });

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Communities_Logo_File_Id",
                table: "Comm_Communities",
                column: "Logo_File_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Communities_Owner_Employee_Id",
                table: "Comm_Communities",
                column: "Owner_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Comm_Communities_Slug",
                table: "Comm_Communities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Invitations_Invited_By_Employee_Id",
                table: "Comm_Invitations",
                column: "Invited_By_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Invitations_Invitee",
                table: "Comm_Invitations",
                columns: new[] { "Invited_Employee_Id", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Comm_Invitations_Pending",
                table: "Comm_Invitations",
                columns: new[] { "Community_Id", "Invited_Employee_Id" },
                unique: true,
                filter: "[Status] = 0 AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Members_Community_Status",
                table: "Comm_Members",
                columns: new[] { "Community_Id", "Membership_Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Members_Employee",
                table: "Comm_Members",
                columns: new[] { "Employee_Id", "Membership_Status" })
                .Annotation("SqlServer:Include", new[] { "Community_Id", "Member_Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Comm_Members_Reviewed_By_Employee_Id",
                table: "Comm_Members",
                column: "Reviewed_By_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Comm_Members",
                table: "Comm_Members",
                columns: new[] { "Community_Id", "Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Audiences_Key",
                table: "Doc_Audiences",
                column: "Audience_Key")
                .Annotation("SqlServer:Include", new[] { "Document_Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Doc_Audiences_Owner_Key",
                table: "Doc_Audiences",
                columns: new[] { "Document_Id", "Audience_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Categories_Parent_Category_Id",
                table: "Doc_Categories",
                column: "Parent_Category_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Documents_Category",
                table: "Doc_Documents",
                columns: new[] { "Category_Id", "Is_Published" });

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Documents_Current_Version_Id",
                table: "Doc_Documents",
                column: "Current_Version_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Documents_Owner_Employee_Id",
                table: "Doc_Documents",
                column: "Owner_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Documents_Review",
                table: "Doc_Documents",
                column: "Review_Date",
                filter: "[Review_Date] IS NOT NULL AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_DownloadLogs_Document",
                table: "Doc_DownloadLogs",
                columns: new[] { "Document_Id", "Downloaded_At" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Doc_DownloadLogs_Employee",
                table: "Doc_DownloadLogs",
                columns: new[] { "Employee_Id", "Downloaded_At" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Doc_DownloadLogs_Version_Id",
                table: "Doc_DownloadLogs",
                column: "Version_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Files_Checksum",
                table: "Doc_Files",
                column: "Checksum_Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Files_ScanStatus",
                table: "Doc_Files",
                column: "Scan_Status",
                filter: "[Scan_Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Files_Uploaded_By",
                table: "Doc_Files",
                column: "Uploaded_By");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_Versions_File_Id",
                table: "Doc_Versions",
                column: "File_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Doc_Versions_Number",
                table: "Doc_Versions",
                columns: new[] { "Document_Id", "Version_Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Attendees_Employee",
                table: "Evt_Attendees",
                columns: new[] { "Employee_Id", "Response" });

            migrationBuilder.CreateIndex(
                name: "UX_Evt_Attendees",
                table: "Evt_Attendees",
                columns: new[] { "Event_Id", "Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Audiences_Key",
                table: "Evt_Audiences",
                column: "Audience_Key")
                .Annotation("SqlServer:Include", new[] { "Event_Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Evt_Audiences_Owner_Key",
                table: "Evt_Audiences",
                columns: new[] { "Event_Id", "Audience_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Events_Banner_File_Id",
                table: "Evt_Events",
                column: "Banner_File_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Events_Calendar",
                table: "Evt_Events",
                columns: new[] { "Status", "Is_Deleted", "Start_At" })
                .Annotation("SqlServer:Include", new[] { "Title_Ar", "Title_En", "End_At", "Event_Type", "Location_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Events_Community",
                table: "Evt_Events",
                column: "Community_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Events_Location_Id",
                table: "Evt_Events",
                column: "Location_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Events_Organizer_Employee_Id",
                table: "Evt_Events",
                column: "Organizer_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Evt_Events_PendingReminders",
                table: "Evt_Events",
                column: "Start_At",
                filter: "[Reminder_Sent_At] IS NULL AND [Status] = 1 AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_CommentMentions_Employee",
                table: "Feed_CommentMentions",
                columns: new[] { "Mentioned_Employee_Id", "Mentioned_At" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UX_Feed_CommentMentions",
                table: "Feed_CommentMentions",
                columns: new[] { "Comment_Id", "Mentioned_Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feed_CommentReactions_Employee_Id",
                table: "Feed_CommentReactions",
                column: "Employee_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Feed_CommentReactions",
                table: "Feed_CommentReactions",
                columns: new[] { "Comment_Id", "Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Comments_Author",
                table: "Feed_Comments",
                column: "Author_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Comments_Parent_Comment_Id",
                table: "Feed_Comments",
                column: "Parent_Comment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Comments_Thread",
                table: "Feed_Comments",
                columns: new[] { "Post_Id", "Parent_Comment_Id", "Create_Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostAttachments_File_Id",
                table: "Feed_PostAttachments",
                column: "File_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostAttachments_Post",
                table: "Feed_PostAttachments",
                column: "Post_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostAudiences_Key",
                table: "Feed_PostAudiences",
                column: "Audience_Key")
                .Annotation("SqlServer:Include", new[] { "Post_Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Feed_PostAudiences_Owner_Key",
                table: "Feed_PostAudiences",
                columns: new[] { "Post_Id", "Audience_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostMentions_Employee",
                table: "Feed_PostMentions",
                columns: new[] { "Mentioned_Employee_Id", "Mentioned_At" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UX_Feed_PostMentions",
                table: "Feed_PostMentions",
                columns: new[] { "Post_Id", "Mentioned_Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostReactions_Employee_Id",
                table: "Feed_PostReactions",
                column: "Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostReactions_Post_Type",
                table: "Feed_PostReactions",
                columns: new[] { "Post_Id", "Reaction_Type" });

            migrationBuilder.CreateIndex(
                name: "UX_Feed_PostReactions_Post_Employee",
                table: "Feed_PostReactions",
                columns: new[] { "Post_Id", "Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Posts_Author",
                table: "Feed_Posts",
                columns: new[] { "Author_Employee_Id", "Status", "Create_Date" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Posts_Community",
                table: "Feed_Posts",
                columns: new[] { "Community_Id", "Status", "Published_Date" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Posts_Pinned",
                table: "Feed_Posts",
                column: "Published_Date",
                descending: new bool[0],
                filter: "[Is_Pinned] = 1 AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Posts_Scheduled",
                table: "Feed_Posts",
                column: "Scheduled_Publish_Date",
                filter: "[Scheduled_Publish_Date] IS NOT NULL AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Feed_Posts_Timeline",
                table: "Feed_Posts",
                columns: new[] { "Status", "Is_Deleted", "Published_Date", "Id" },
                descending: new[] { false, false, true, true })
                .Annotation("SqlServer:Include", new[] { "Author_Employee_Id", "Community_Id", "Post_Type", "Is_Pinned", "Is_Official", "Reactions_Count", "Comments_Count" });

            migrationBuilder.CreateIndex(
                name: "IX_Feed_PostViews_Employee_Id",
                table: "Feed_PostViews",
                column: "Employee_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Feed_PostViews",
                table: "Feed_PostViews",
                columns: new[] { "Post_Id", "Employee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Notif_Preferences",
                table: "Notif_Preferences",
                columns: new[] { "Employee_Id", "Notification_Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_Departments_Manager_Employee_Id",
                table: "Org_Departments",
                column: "Manager_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Departments_Organization_Id",
                table: "Org_Departments",
                column: "Organization_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Departments_Parent_Department_Id",
                table: "Org_Departments",
                column: "Parent_Department_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Departments_Path",
                table: "Org_Departments",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "UX_Org_Departments_Code",
                table: "Org_Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_EmployeeFollowers_Followee",
                table: "Org_EmployeeFollowers",
                column: "Followee_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Org_EmployeeFollowers",
                table: "Org_EmployeeFollowers",
                columns: new[] { "Follower_Id", "Followee_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_EmployeeInterests_Interest_Id",
                table: "Org_EmployeeInterests",
                column: "Interest_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Org_EmployeeInterests",
                table: "Org_EmployeeInterests",
                columns: new[] { "Employee_Id", "Interest_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_EmployeeManagers_Manager",
                table: "Org_EmployeeManagers",
                column: "Manager_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Org_EmployeeManagers_CurrentPrimary",
                table: "Org_EmployeeManagers",
                column: "Employee_Id",
                unique: true,
                filter: "[Is_Primary] = 1 AND [Effective_To] IS NULL AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Org_EmployeeManagers_Relationship",
                table: "Org_EmployeeManagers",
                columns: new[] { "Employee_Id", "Manager_Id", "Effective_From" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Active_Department",
                table: "Org_Employees",
                columns: new[] { "Is_Deleted", "Is_Active", "Department_Id" })
                .Annotation("SqlServer:Include", new[] { "Display_Name", "Job_Title_Id", "Location_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Birthday",
                table: "Org_Employees",
                column: "Birth_Date",
                filter: "[Show_Birthday] = 1 AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Department_Id",
                table: "Org_Employees",
                column: "Department_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Job_Title_Id",
                table: "Org_Employees",
                column: "Job_Title_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Joining",
                table: "Org_Employees",
                column: "Joining_Date");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Location_Id",
                table: "Org_Employees",
                column: "Location_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Employees_Team_Id",
                table: "Org_Employees",
                column: "Team_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Org_Employees_Email",
                table: "Org_Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Org_Employees_Number",
                table: "Org_Employees",
                column: "Employee_Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Org_Employees_User",
                table: "Org_Employees",
                column: "User_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_EmployeeSkills_Skill",
                table: "Org_EmployeeSkills",
                column: "Skill_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Org_EmployeeSkills",
                table: "Org_EmployeeSkills",
                columns: new[] { "Employee_Id", "Skill_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Org_Interests_NameAr",
                table: "Org_Interests",
                column: "Name_Ar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_JobTitles_Organization_Id",
                table: "Org_JobTitles",
                column: "Organization_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Locations_Organization_Id",
                table: "Org_Locations",
                column: "Organization_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Org_Skills_NameAr",
                table: "Org_Skills",
                column: "Name_Ar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Org_Teams_Department",
                table: "Org_Teams",
                column: "Department_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Org_Teams_Lead_Employee_Id",
                table: "Org_Teams",
                column: "Lead_Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_Audiences_Key",
                table: "Poll_Audiences",
                column: "Audience_Key")
                .Annotation("SqlServer:Include", new[] { "Poll_Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Poll_Audiences_Owner_Key",
                table: "Poll_Audiences",
                columns: new[] { "Poll_Id", "Audience_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Poll_Options_Poll",
                table: "Poll_Options",
                column: "Poll_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_Polls_Expiry",
                table: "Poll_Polls",
                columns: new[] { "Status", "Expiry_Date" },
                filter: "[Expiry_Date] IS NOT NULL AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_Polls_Post",
                table: "Poll_Polls",
                column: "Post_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_Votes_Employee_Id",
                table: "Poll_Votes",
                column: "Employee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_Votes_Option_Id",
                table: "Poll_Votes",
                column: "Option_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Poll_Votes_Anonymous",
                table: "Poll_Votes",
                columns: new[] { "Poll_Id", "Option_Id", "Voter_Hash" },
                unique: true,
                filter: "[Voter_Hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Poll_Votes_Identified",
                table: "Poll_Votes",
                columns: new[] { "Poll_Id", "Option_Id", "Employee_Id" },
                unique: true,
                filter: "[Employee_Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Rec_LeaderboardSnapshots_Department_Id",
                table: "Rec_LeaderboardSnapshots",
                column: "Department_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Rec_LeaderboardSnapshots_Ranking",
                table: "Rec_LeaderboardSnapshots",
                columns: new[] { "Period", "Period_Start", "Department_Id", "Rank" })
                .Annotation("SqlServer:Include", new[] { "Employee_Id", "Points", "Recognition_Count" });

            migrationBuilder.CreateIndex(
                name: "UX_Rec_LeaderboardSnapshots",
                table: "Rec_LeaderboardSnapshots",
                columns: new[] { "Employee_Id", "Period", "Period_Start", "Department_Id" },
                unique: true,
                filter: "[Department_Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Rec_Recognitions_Post_Id",
                table: "Rec_Recognitions",
                column: "Post_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Rec_Recognitions_Recipient",
                table: "Rec_Recognitions",
                columns: new[] { "Recipient_Employee_Id", "Recognised_On" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Rec_Recognitions_Recognition_Type_Id",
                table: "Rec_Recognitions",
                column: "Recognition_Type_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Rec_Recognitions_Sender",
                table: "Rec_Recognitions",
                columns: new[] { "Sender_Employee_Id", "Recognised_On" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UX_Rec_RecognitionTypes_Code",
                table: "Rec_RecognitionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Security_RefreshTokens_Family",
                table: "Security_RefreshTokens",
                column: "Family_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_RefreshTokens_Replaced_By_Id",
                table: "Security_RefreshTokens",
                column: "Replaced_By_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_RefreshTokens_User",
                table: "Security_RefreshTokens",
                columns: new[] { "User_Id", "Expires_At" });

            migrationBuilder.CreateIndex(
                name: "UX_Security_RefreshTokens_Hash",
                table: "Security_RefreshTokens",
                column: "Token_Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Answers_Option_Id",
                table: "Srv_Answers",
                column: "Option_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Answers_Question",
                table: "Srv_Answers",
                columns: new[] { "Question_Id", "Option_Id" })
                .Annotation("SqlServer:Include", new[] { "Numeric_Value" });

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Answers_Response",
                table: "Srv_Answers",
                column: "Response_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Audiences_Key",
                table: "Srv_Audiences",
                column: "Audience_Key")
                .Annotation("SqlServer:Include", new[] { "Survey_Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Srv_Audiences_Owner_Key",
                table: "Srv_Audiences",
                columns: new[] { "Survey_Id", "Audience_Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Srv_QuestionOptions_Question",
                table: "Srv_QuestionOptions",
                columns: new[] { "Question_Id", "Sort_Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Questions_Survey",
                table: "Srv_Questions",
                columns: new[] { "Survey_Id", "Sort_Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Responses_Analysis",
                table: "Srv_Responses",
                columns: new[] { "Survey_Id", "Is_Complete", "Department_Id_At_Submission" });

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Responses_Employee_Id",
                table: "Srv_Responses",
                column: "Employee_Id");

            migrationBuilder.CreateIndex(
                name: "UX_Srv_Responses_Anonymous",
                table: "Srv_Responses",
                columns: new[] { "Survey_Id", "Respondent_Hash" },
                unique: true,
                filter: "[Respondent_Hash] IS NOT NULL AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Srv_Responses_Identified",
                table: "Srv_Responses",
                columns: new[] { "Survey_Id", "Employee_Id" },
                unique: true,
                filter: "[Employee_Id] IS NOT NULL AND [Is_Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Srv_Surveys_Window",
                table: "Srv_Surveys",
                columns: new[] { "Status", "Start_Date", "End_Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_Comm_Communities_Org_Employees_Owner_Employee_Id",
                table: "Comm_Communities",
                column: "Owner_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comm_Invitations_Org_Employees_Invited_By_Employee_Id",
                table: "Comm_Invitations",
                column: "Invited_By_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comm_Invitations_Org_Employees_Invited_Employee_Id",
                table: "Comm_Invitations",
                column: "Invited_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comm_Members_Org_Employees_Employee_Id",
                table: "Comm_Members",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comm_Members_Org_Employees_Reviewed_By_Employee_Id",
                table: "Comm_Members",
                column: "Reviewed_By_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doc_Audiences_Doc_Documents_Document_Id",
                table: "Doc_Audiences",
                column: "Document_Id",
                principalTable: "Doc_Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Doc_Documents_Doc_Versions_Current_Version_Id",
                table: "Doc_Documents",
                column: "Current_Version_Id",
                principalTable: "Doc_Versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doc_Documents_Org_Employees_Owner_Employee_Id",
                table: "Doc_Documents",
                column: "Owner_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doc_DownloadLogs_Org_Employees_Employee_Id",
                table: "Doc_DownloadLogs",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evt_Attendees_Evt_Events_Event_Id",
                table: "Evt_Attendees",
                column: "Event_Id",
                principalTable: "Evt_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Evt_Attendees_Org_Employees_Employee_Id",
                table: "Evt_Attendees",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evt_Audiences_Evt_Events_Event_Id",
                table: "Evt_Audiences",
                column: "Event_Id",
                principalTable: "Evt_Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Evt_Events_Org_Employees_Organizer_Employee_Id",
                table: "Evt_Events",
                column: "Organizer_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_CommentMentions_Feed_Comments_Comment_Id",
                table: "Feed_CommentMentions",
                column: "Comment_Id",
                principalTable: "Feed_Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_CommentMentions_Org_Employees_Mentioned_Employee_Id",
                table: "Feed_CommentMentions",
                column: "Mentioned_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_CommentReactions_Feed_Comments_Comment_Id",
                table: "Feed_CommentReactions",
                column: "Comment_Id",
                principalTable: "Feed_Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_CommentReactions_Org_Employees_Employee_Id",
                table: "Feed_CommentReactions",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_Comments_Feed_Posts_Post_Id",
                table: "Feed_Comments",
                column: "Post_Id",
                principalTable: "Feed_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_Comments_Org_Employees_Author_Employee_Id",
                table: "Feed_Comments",
                column: "Author_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostAttachments_Feed_Posts_Post_Id",
                table: "Feed_PostAttachments",
                column: "Post_Id",
                principalTable: "Feed_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostAudiences_Feed_Posts_Post_Id",
                table: "Feed_PostAudiences",
                column: "Post_Id",
                principalTable: "Feed_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostMentions_Feed_Posts_Post_Id",
                table: "Feed_PostMentions",
                column: "Post_Id",
                principalTable: "Feed_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostMentions_Org_Employees_Mentioned_Employee_Id",
                table: "Feed_PostMentions",
                column: "Mentioned_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostReactions_Feed_Posts_Post_Id",
                table: "Feed_PostReactions",
                column: "Post_Id",
                principalTable: "Feed_Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostReactions_Org_Employees_Employee_Id",
                table: "Feed_PostReactions",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_Posts_Org_Employees_Author_Employee_Id",
                table: "Feed_Posts",
                column: "Author_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feed_PostViews_Org_Employees_Employee_Id",
                table: "Feed_PostViews",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notif_Preferences_Org_Employees_Employee_Id",
                table: "Notif_Preferences",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_Departments_Org_Employees_Manager_Employee_Id",
                table: "Org_Departments",
                column: "Manager_Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_EmployeeFollowers_Org_Employees_Followee_Id",
                table: "Org_EmployeeFollowers",
                column: "Followee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_EmployeeFollowers_Org_Employees_Follower_Id",
                table: "Org_EmployeeFollowers",
                column: "Follower_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_EmployeeInterests_Org_Employees_Employee_Id",
                table: "Org_EmployeeInterests",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_EmployeeManagers_Org_Employees_Employee_Id",
                table: "Org_EmployeeManagers",
                column: "Employee_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_EmployeeManagers_Org_Employees_Manager_Id",
                table: "Org_EmployeeManagers",
                column: "Manager_Id",
                principalTable: "Org_Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Org_Employees_Org_Teams_Team_Id",
                table: "Org_Employees",
                column: "Team_Id",
                principalTable: "Org_Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doc_Versions_Doc_Files_File_Id",
                table: "Doc_Versions");

            migrationBuilder.DropForeignKey(
                name: "FK_Doc_Documents_Org_Employees_Owner_Employee_Id",
                table: "Doc_Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Org_Departments_Org_Employees_Manager_Employee_Id",
                table: "Org_Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Org_Teams_Org_Employees_Lead_Employee_Id",
                table: "Org_Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Doc_Versions_Doc_Documents_Document_Id",
                table: "Doc_Versions");

            migrationBuilder.DropTable(
                name: "Audit_Logs");

            migrationBuilder.DropTable(
                name: "Comm_Invitations");

            migrationBuilder.DropTable(
                name: "Comm_Members");

            migrationBuilder.DropTable(
                name: "Doc_Audiences");

            migrationBuilder.DropTable(
                name: "Doc_DownloadLogs");

            migrationBuilder.DropTable(
                name: "Evt_Attendees");

            migrationBuilder.DropTable(
                name: "Evt_Audiences");

            migrationBuilder.DropTable(
                name: "Feed_CommentMentions");

            migrationBuilder.DropTable(
                name: "Feed_CommentReactions");

            migrationBuilder.DropTable(
                name: "Feed_PostAttachments");

            migrationBuilder.DropTable(
                name: "Feed_PostAudiences");

            migrationBuilder.DropTable(
                name: "Feed_PostMentions");

            migrationBuilder.DropTable(
                name: "Feed_PostReactions");

            migrationBuilder.DropTable(
                name: "Feed_PostViews");

            migrationBuilder.DropTable(
                name: "Notif_Preferences");

            migrationBuilder.DropTable(
                name: "Org_EmployeeFollowers");

            migrationBuilder.DropTable(
                name: "Org_EmployeeInterests");

            migrationBuilder.DropTable(
                name: "Org_EmployeeManagers");

            migrationBuilder.DropTable(
                name: "Org_EmployeeSkills");

            migrationBuilder.DropTable(
                name: "Poll_Audiences");

            migrationBuilder.DropTable(
                name: "Poll_Votes");

            migrationBuilder.DropTable(
                name: "Rec_LeaderboardSnapshots");

            migrationBuilder.DropTable(
                name: "Rec_Recognitions");

            migrationBuilder.DropTable(
                name: "Security_RefreshTokens");

            migrationBuilder.DropTable(
                name: "Srv_Answers");

            migrationBuilder.DropTable(
                name: "Srv_Audiences");

            migrationBuilder.DropTable(
                name: "Evt_Events");

            migrationBuilder.DropTable(
                name: "Feed_Comments");

            migrationBuilder.DropTable(
                name: "Org_Interests");

            migrationBuilder.DropTable(
                name: "Org_Skills");

            migrationBuilder.DropTable(
                name: "Poll_Options");

            migrationBuilder.DropTable(
                name: "Rec_RecognitionTypes");

            migrationBuilder.DropTable(
                name: "Srv_QuestionOptions");

            migrationBuilder.DropTable(
                name: "Srv_Responses");

            migrationBuilder.DropTable(
                name: "Poll_Polls");

            migrationBuilder.DropTable(
                name: "Srv_Questions");

            migrationBuilder.DropTable(
                name: "Feed_Posts");

            migrationBuilder.DropTable(
                name: "Srv_Surveys");

            migrationBuilder.DropTable(
                name: "Comm_Communities");

            migrationBuilder.DropTable(
                name: "Doc_Files");

            migrationBuilder.DropTable(
                name: "Org_Employees");

            migrationBuilder.DropTable(
                name: "Org_JobTitles");

            migrationBuilder.DropTable(
                name: "Org_Locations");

            migrationBuilder.DropTable(
                name: "Org_Teams");

            migrationBuilder.DropTable(
                name: "Org_Departments");

            migrationBuilder.DropTable(
                name: "Org_Organizations");

            migrationBuilder.DropTable(
                name: "Doc_Documents");

            migrationBuilder.DropTable(
                name: "Doc_Categories");

            migrationBuilder.DropTable(
                name: "Doc_Versions");

            migrationBuilder.DropIndex(
                name: "UX_Common_LinkScreenActions_PermissionKey",
                table: "Common_LinkScreenActions");

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("039ca671-1cd4-6bb4-755e-a235b00b130d"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("075e48c6-2f78-296c-bd27-664fded52de0"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("077b401b-1314-2dfe-7a3b-43541a4a8d78"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("07f67616-a255-5cf4-46f9-45bdf5ebce10"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("0b11cc3f-7c12-3942-64ac-6caaf35c2212"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("0f7bb4da-e2fb-79fb-97b8-b7c02d36b68f"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("0ff8c19f-ca74-e943-9c59-c82961c0ada0"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("119f1b43-6d61-c2ed-6739-0b9d1c9617d6"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("12dfb774-1551-381f-cf72-3d8010cec0e1"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("134d3988-2a27-e210-47d3-4a66ab656d95"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("143e7891-f934-aa92-4f61-77e2f6ddfd7c"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1589e7ce-49ea-aeed-041b-b6a5d54bd328"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("17838ce3-ebff-ca49-b387-015dd590918d"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("186e1ca9-a1e3-8f6e-df9f-62941c0a7417"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("18dbdb02-4eea-dda2-0767-577a0881aa7b"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("19bf8ae4-5e75-1674-403f-afb81b5e5be3"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1b77d8c4-edf2-143a-b463-d2581f0636e6"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1da8b7ec-c7bb-bad2-da81-f79b6b077537"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1dc50366-6e44-0f8f-9d36-ae3a24ce28d4"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1f08f101-37f8-2c9c-482c-e023c86ac38d"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("2719fc3d-3f61-ee84-146c-fa9200b4c9a4"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("3333398a-e5eb-5090-9257-634125273a58"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("34467b52-420c-e6cf-b2a9-8c92799c454e"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("3479273a-0f91-a663-570c-1b41659bae68"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("359112d8-9dd7-fd13-b6c3-1f04ddce33a3"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("36ac6ac5-befe-c388-d0e3-7909829dfe23"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("3980f1a8-382a-ee49-ba95-4231fb8cf5b9"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("3b889690-570e-efdd-0890-e58add51a84b"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("3d98f770-e098-262b-3c5c-6d090ce2251e"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("4226f87d-830f-4806-1045-ed566be2ebfb"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("4346be40-ad6d-9886-2e6e-0fd4838a3953"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("4f991ea7-3a1f-6da4-f406-1357ef8deed5"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("520dc2e8-0d86-6e72-1bae-108bc788ccdc"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("52447140-bd94-73dd-7951-ab5c332c7242"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("5756f157-f1be-d1ee-bcd8-0ae7de604d24"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("5b693340-66fa-6a66-621d-1da5e1630fcf"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("5fc407f8-cc40-e6e1-66a4-fa2278073d18"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("639482c5-9523-d880-636d-ef121318cb68"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("68ed50ae-6900-84ce-f9c7-657aa0208551"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("699440be-58e4-f982-bddf-87634fe174b6"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("6a6db383-03dc-9147-2f15-19057da7daa3"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("6d0888bd-692f-4b8d-a867-66702a026d2c"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("759f6b9f-8c4c-d6c5-611a-60d76c91b8e0"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("7d44b0d7-d38e-4133-0df8-9b6e473b36a9"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("7d9d2228-9f31-2bf9-d50e-5345b422a2da"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("840e799d-c9cc-3826-843b-5d1ce9b8b210"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("84a9a7f5-e40d-f63f-31b1-b3d716a2dbc7"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("87e6e22d-b8cc-c20a-8b98-46c2f18593c8"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("8d68280c-5862-b898-5959-9240b156d5c8"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9a0a0c78-45f7-30bd-bbdf-5a01d2315f6c"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9af95b38-8015-137e-fc91-64871456e17c"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9b6e13af-2bba-09b1-b80c-25c86c42a85d"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9b8c18b7-3d0f-11dd-94c2-708ea1496615"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9bae457d-f946-8107-af4e-b6ba53a37191"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9bf7067b-6abd-241a-f212-c18a3f48dde2"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9f5722aa-93d5-18c6-b4e5-2215d49f1f1b"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("a0a3300d-068d-b5bc-92e5-040da38d876f"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("a5e65ac4-8421-e279-9653-cfea6c048b1d"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("a7e68fff-ac5e-11b8-18cf-8c12806d82ff"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("a8aeddca-9a19-2323-ad03-e90a15c25fc0"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("a9d7211b-ba80-21d5-edf3-9300fcf2a0e5"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("a9f570ea-0564-0215-1f29-5d3c3eff6d15"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("aadf49c3-4203-8d16-d99d-331441dad629"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("aeb63851-ef8f-a9ee-0f95-00a54bf61707"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("afa6f9b2-0cb2-6c39-d474-514442cf103a"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("b1bf9764-a5f5-a1c2-cfe5-368988def47e"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("b4224465-2b45-48d6-d2aa-07bfc0433427"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("b8b9b657-ca10-ebae-2f33-cfc5cd3d507f"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("b99f6864-0081-30e1-3370-caa08069ee2d"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("bae60ba1-2fc1-ba2a-aeed-97edc4356665"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("bb22e323-1c32-23b0-b57d-ab2a87b178a0"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("c32d0232-6a62-24b1-3513-d36da00aa0a8"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("c4a076e3-2e61-53ef-514e-5ec997000a50"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("cbd1c052-107e-cc37-bfa4-b9989cf3b75a"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("d0077adb-e95b-43af-7f1a-1ade9704b603"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("d0497125-a7e5-c610-258f-7764192464cc"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("d1e397f4-8fef-626c-1988-711ae1f25fef"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("d3fde401-e8ab-42d7-411a-42619f3583b4"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("db5df094-313c-fd1c-75fd-a75dfba4c44a"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("dc5b8078-ee1f-ae3f-3f98-9a3df7f8b1a4"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("dcc96706-6ffd-fcc0-166c-e5d43b28b535"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("dd87d1f0-7e7a-85e0-592d-8fe654fe0a11"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("e197a7c4-e779-0228-d029-b056ec0223bb"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("e2d21735-fbe6-e016-c9ca-1e5379bfcb02"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("e38bbc07-8d1e-80a2-437e-e169000dc64e"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("e49d40b2-a2fe-0ff2-aa2e-99e23ff5e598"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("f1854997-3808-2ab5-f207-630ae8dfa5ca"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("f467cc44-4d20-970d-5a91-a3fd426c3b50"));

            migrationBuilder.DeleteData(
                table: "Security_GroupPermissions",
                keyColumn: "Id",
                keyValue: new Guid("f56636d2-e189-2074-0f6d-09f3e055ec53"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("14185243-001d-de0c-2c44-3450e4a9c34a"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("1999a305-e4cb-ec1f-9e29-5f2af97575fd"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("19b718ef-ebd6-cfa6-f792-28183217f320"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("21bda175-e250-57fc-bbb3-5cb6a621ab6c"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("345aea7f-194e-472e-c3c3-aaf7b44ac8f6"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("41794d60-8695-75ab-9012-235d173405e9"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("437392f2-83d3-41ae-6576-3808806524b5"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("455498dc-69c8-94fb-e158-94820f4c13ab"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("53015a08-7ad0-0afb-1b75-fb38207d0e97"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("5c168320-a722-fa1f-3ff4-746c5d77a2a4"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("5f13b5c8-e0b7-575b-d99b-4e1aae7992d0"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("76e00ce9-38a6-eb68-07dc-f6cc4dfe50e1"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("890a1f0e-784c-130c-b87b-c93da74d78c0"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("91f92fed-6fe7-0a48-e3bd-e70330dcbe61"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("9aad7c8a-4fdc-54a5-d631-ea6a44900dfb"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("a0568325-97ec-fa2b-8efb-96227bb79739"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("a242f278-f23d-95b0-0a2b-4edfb6431dc3"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("c456a7f0-68e1-7a1e-bb2d-8bf5d970a87e"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("c7522cf4-6a02-cf1a-3b23-f921a35da5da"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("d029ea0e-e268-4181-bae4-81d2e1a2d81c"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("d2609c46-dadb-8d1d-9554-ea3bc9a496e0"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("d70facd0-829d-83f9-620b-a96c53ae3052"));

            migrationBuilder.DeleteData(
                table: "Common_LinkScreenActions",
                keyColumn: "Id",
                keyValue: new Guid("e5a48e33-902f-f907-ca77-683b32b60078"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("0e4a582f-39a2-17c1-ffe2-2b6e11e4abf3"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("6a62833b-08c9-100a-0533-3298ab96d542"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("6edc92ec-3fde-2111-be7c-4429eacaf5b3"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("82592ca4-4700-a58d-0130-901224a206be"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("b9693c5b-2adf-b85e-f144-706fa89a797f"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("cadba0a2-ba84-9d11-0f46-ada26199cc0c"));

            migrationBuilder.DeleteData(
                table: "Security_UserGroups",
                keyColumn: "Id",
                keyValue: new Guid("e5c866a8-231a-ec9f-1d65-c44e2965179f"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("4c0ed04b-2a78-d214-c388-597859b8fb7d"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("69d997ef-d97f-b6f8-aa2b-48b755da367e"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("6d80f82b-fe84-782d-021b-5529e17c1310"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("6f74d6ea-4310-24e4-ad12-a1a376c8b46a"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("8b85f185-e437-e3b7-f7f7-8fa5a24121ae"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("90b35569-6ef8-72b0-7b4d-0a6c5e24d854"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("a0674b4c-3bb0-bae3-6812-081e5a6f068a"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("ae006910-2221-5849-3f4d-2bdf981533d2"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("d62256dc-a055-31bd-4443-a86e7e3ff2d3"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("e88a15e8-ef0a-3b68-29f5-75027d5589d4"));

            migrationBuilder.DeleteData(
                table: "Common_Screens",
                keyColumn: "Id",
                keyValue: new Guid("fe7f91bf-b88a-4d9c-ab5f-2a85d48619d0"));

            migrationBuilder.DropColumn(
                name: "Description_Ar",
                table: "Common_LinkScreenActions");

            migrationBuilder.DropColumn(
                name: "Description_En",
                table: "Common_LinkScreenActions");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "Common_LinkScreenActions");

            migrationBuilder.DropColumn(
                name: "Permission_Key",
                table: "Common_LinkScreenActions");
        }
    }
}
