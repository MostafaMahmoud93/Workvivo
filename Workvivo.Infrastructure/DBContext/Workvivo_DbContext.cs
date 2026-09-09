namespace Workvivo.Infrastructure.DBContext;
public class Workvivo_DbContext : IdentityDbContext<ApplicationUser, UserGroup, Guid, IdentityUserClaim<Guid>, UserGroupsLink, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    private readonly IUserAccessor _userAccessor;
    public Workvivo_DbContext(DbContextOptions<Workvivo_DbContext> options, IUserAccessor userAccessor) : base(options)
    {
        _userAccessor = userAccessor;
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
        ViewsConfiguration.Configuration(builder);
        FunctionsConfiguration.Configuration(builder);
        ProceduresConfiguration.Configuration(builder);
        #region Identity Table Configuration
        //builder.Ignore<IdentityUserClaim<Guid>>();
        //builder.Ignore<IdentityUserLogin<Guid>>();
        //builder.Ignore<IdentityRoleClaim<Guid>>();
        //builder.Ignore<IdentityUserToken<Guid>>();
        builder.Entity<UserGroupsLink>(ur =>
        {
            ur.ToTable("Security_UserGroupsLink");
            ur.Property(a => a.UserId).HasColumnName("User_Id");
            ur.Property(a => a.RoleId).HasColumnName("Group_Id");
            ur.HasKey(ur => new { ur.UserId, ur.RoleId });
            ur.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).IsRequired();
            ur.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).IsRequired();
        });
        builder.Entity<UserGroup>(q =>
        {
            q.ToTable("Security_UserGroups");
            q.HasMany(ur => ur.UserRoles).WithOne(u => u.Role).HasForeignKey(ur => ur.RoleId).IsRequired();
            q.HasMany(ur => ur.RoleActions).WithOne(r => r.Role).HasForeignKey(ur => ur.Group_Id).IsRequired();
        });
        builder.Entity<ApplicationUser>(p =>
        {
            p.ToTable("Security_Users");
            p.HasMany(ur => ur.UserRoles).WithOne(u => u.User).HasForeignKey(ur => ur.UserId).IsRequired();
            p.HasMany(ur => ur.UserActions).WithOne(r => r.User).HasForeignKey(ur => ur.User_Id).IsRequired();
            p.Ignore(u => u.PhoneNumberConfirmed);
            p.Ignore(u => u.TwoFactorEnabled);
            p.Ignore(u => u.LockoutEnd);
            p.Ignore(u => u.LockoutEnabled);
            p.Ignore(u => u.AccessFailedCount);
        });
        #endregion
        // Seed the first user, Security_MasterData, and Security_UserGroups
        SeedInitialData(builder);

        // Roles, the named-permission catalogue, recognition categories and document
        // categories. Part of the schema contract, so it ships in the migration and is
        // present in every environment. Sample employees and posts are not seeded here -
        // those belong to a development-only runtime seeder.
        ReferenceDataSeeder.Seed(builder);
    }
    #region Tables
    public virtual DbSet<GroupPermissions> Security_GroupPermissions { get; set; }
    public virtual DbSet<LinkScreenAction> Common_LinkScreenActions { get; set; }
    public virtual DbSet<UserPermissions> Security_UserPermissions { get; set; }
    public virtual DbSet<GlobalAttachment> GlobalAttachments { get; set; }
    public virtual DbSet<EmailSMSTemplate> EmailSMSTemplates { get; set; }
    public virtual DbSet<NotificationUser> NotificationUsers { get; set; }
    public virtual DbSet<ScreenAction> Common_ScreenActions { get; set; }
    public virtual DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public virtual DbSet<EmailSMSHistory> EmailSMSHistory { get; set; }
    public virtual DbSet<LookupCategory> LookupCategories { get; set; }
    public virtual DbSet<MasterData> Security_MasterData { get; set; }
    public virtual DbSet<UsersShortCuts> Users_ShortCuts { get; set; }
    public virtual DbSet<AccessLog> Security_AccessLogs { get; set; }
    public virtual DbSet<MainModule> Common_MainModules { get; set; }
    public virtual DbSet<UserGroupsLink> UserGroupLinks { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<UserLoginLog> UserLoginLogs { get; set; }
    public virtual DbSet<Screen> Common_Screens { get; set; }
    public virtual DbSet<SysSetting> SysSetting { get; set; }
    public virtual DbSet<Lookup> Lookups { get; set; }

    #endregion

    #region Platform tables

    // Organisation
    public virtual DbSet<Organization> Org_Organizations { get; set; }
    public virtual DbSet<Department> Org_Departments { get; set; }
    public virtual DbSet<Team> Org_Teams { get; set; }
    public virtual DbSet<Location> Org_Locations { get; set; }
    public virtual DbSet<JobTitle> Org_JobTitles { get; set; }
    public virtual DbSet<Employee> Org_Employees { get; set; }
    public virtual DbSet<EmployeeManager> Org_EmployeeManagers { get; set; }
    public virtual DbSet<Skill> Org_Skills { get; set; }
    public virtual DbSet<EmployeeSkill> Org_EmployeeSkills { get; set; }
    public virtual DbSet<Interest> Org_Interests { get; set; }
    public virtual DbSet<EmployeeInterest> Org_EmployeeInterests { get; set; }
    public virtual DbSet<EmployeeFollower> Org_EmployeeFollowers { get; set; }

    // Feed
    public virtual DbSet<Post> Feed_Posts { get; set; }
    public virtual DbSet<PostAttachment> Feed_PostAttachments { get; set; }
    public virtual DbSet<PostAudience> Feed_PostAudiences { get; set; }
    public virtual DbSet<PostReaction> Feed_PostReactions { get; set; }
    public virtual DbSet<PostMention> Feed_PostMentions { get; set; }
    public virtual DbSet<PostView> Feed_PostViews { get; set; }
    public virtual DbSet<Comment> Feed_Comments { get; set; }
    public virtual DbSet<CommentReaction> Feed_CommentReactions { get; set; }
    public virtual DbSet<CommentMention> Feed_CommentMentions { get; set; }

    // Communities
    public virtual DbSet<Community> Comm_Communities { get; set; }
    public virtual DbSet<CommunityMember> Comm_Members { get; set; }
    public virtual DbSet<CommunityInvitation> Comm_Invitations { get; set; }

    // Recognition
    public virtual DbSet<RecognitionType> Rec_RecognitionTypes { get; set; }
    public virtual DbSet<Recognition> Rec_Recognitions { get; set; }
    public virtual DbSet<RecognitionLeaderboardSnapshot> Rec_LeaderboardSnapshots { get; set; }

    // Polls
    public virtual DbSet<Poll> Poll_Polls { get; set; }
    public virtual DbSet<PollOption> Poll_Options { get; set; }
    public virtual DbSet<PollVote> Poll_Votes { get; set; }
    public virtual DbSet<PollAudience> Poll_Audiences { get; set; }

    // Surveys
    public virtual DbSet<Survey> Srv_Surveys { get; set; }
    public virtual DbSet<SurveyQuestion> Srv_Questions { get; set; }
    public virtual DbSet<SurveyQuestionOption> Srv_QuestionOptions { get; set; }
    public virtual DbSet<SurveyResponse> Srv_Responses { get; set; }
    public virtual DbSet<SurveyAnswer> Srv_Answers { get; set; }
    public virtual DbSet<SurveyAudience> Srv_Audiences { get; set; }

    // Events
    public virtual DbSet<Event> Evt_Events { get; set; }
    public virtual DbSet<EventAttendee> Evt_Attendees { get; set; }
    public virtual DbSet<EventAudience> Evt_Audiences { get; set; }

    // Documents
    public virtual DbSet<FileAsset> Doc_Files { get; set; }
    public virtual DbSet<DocumentCategory> Doc_Categories { get; set; }
    public virtual DbSet<Document> Doc_Documents { get; set; }
    public virtual DbSet<DocumentVersion> Doc_Versions { get; set; }
    public virtual DbSet<DocumentAudience> Doc_Audiences { get; set; }
    public virtual DbSet<DocumentDownloadLog> Doc_DownloadLogs { get; set; }

    // Platform
    public virtual DbSet<NotificationPreference> Notif_Preferences { get; set; }
    public virtual DbSet<AuditLog> Audit_Logs { get; set; }
    public virtual DbSet<RefreshToken> Security_RefreshTokens { get; set; }

    #endregion

    #region Views
    public virtual DbSet<VW_UserActions> VW_UserActions { get; set; }
    #endregion
    #region Procedures
    #endregion
    #region AuditSaveChanges
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        // Resolved lazily and at most once per save.
        //
        // This used to be an unconditional query on every SaveChangesAsync - one extra
        // round trip per write for the whole application, to fetch a fallback that is
        // only needed when there is no signed-in user (seeding, background jobs). Now
        // the query only runs on that path, and only if something audited actually
        // changed.
        Guid? auditUserId = null;
        var auditUserResolved = false;

        async Task<Guid> ResolveAuditUserAsync()
        {
            if (!auditUserResolved)
            {
                auditUserResolved = true;

                var current = _userAccessor.GetCurrentUserId();
                if (!string.IsNullOrEmpty(current) && Guid.TryParse(current, out var parsed))
                {
                    auditUserId = parsed;
                }
                else
                {
                    var fallback = await ApplicationUsers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Is_Active && x.Is_Admin, cancellationToken);

                    auditUserId = fallback?.Id;
                }
            }

            // A save with no identifiable actor would write Guid.Empty into Created_By,
            // producing an audit trail that says a change happened but not who made it.
            // Better to fail loudly than to record a lie.
            return auditUserId
                ?? throw new InvalidOperationException(
                    "Cannot stamp audit fields: there is no signed-in user and no active administrator to fall back to.");
        }

        foreach (var entry in ChangeTracker.Entries<FullBaseEntity<Guid>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created_By = await ResolveAuditUserAsync();
                    entry.Entity.Create_Date = DateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.Last_Modified_By ??= await ResolveAuditUserAsync();
                    entry.Entity.Last_Modify_Date = DateTime.Now;
                    break;

            }
        }
        foreach (var entry in ChangeTracker.Entries<FullBaseEntity<int>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created_By = await ResolveAuditUserAsync();
                    entry.Entity.Create_Date = DateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.Last_Modified_By ??= await ResolveAuditUserAsync();
                    entry.Entity.Last_Modify_Date = DateTime.Now;
                    break;

            }
        }
        if (ChangeTracker.Entries<ApplicationUser>().Any())
        {
            foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.Created_By = await ResolveAuditUserAsync();
                        entry.Entity.Create_Date = DateTime.Now;
                        break;

                    case EntityState.Modified:
                        entry.Entity.Last_Modified_By ??= await ResolveAuditUserAsync();
                        entry.Entity.Last_Modify_Date = DateTime.Now;
                        break;

                }
            }

        }

        return await base.SaveChangesAsync(cancellationToken);
    }
    #endregion
    #region SeedInitData
    private void SeedInitialData(ModelBuilder builder)
    {
        // Define the first user data
        // Static on purpose: a Guid.NewGuid() here makes the model
        // non-deterministic and "ef database update" refuses to run.
        var userId = new Guid("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90");
        var groupId = new Guid("b2d4e6f8-1a3c-4e5f-8b7a-9c0d1e2f3a4b");
        var firstUser = new ApplicationUser
        {
            Id = userId,
            UserName = "dev",
            NormalizedUserName = "DEV",
            Email = "must345@yahoo.com",
            NormalizedEmail = "MUST345@YAHOO.COM",
            EmailConfirmed = true,
            // Identity v3 hash of "P@55w0rd". Hashing here instead would
            // salt randomly on every build and break the migration.
            PasswordHash = "AQAAAAIAAYagAAAAEEOc/l+BJqo9HCOS4X6JZSMccZGEliVV0wW8Raa9FutxSoJ+HkJ3D4m/cgElTwiPBw==",
            Full_Name_Ar = "مصطفى محمود",
            Full_Name_En = "Mostafa Mahmoud",
            MobileNo = "0564899515",
            User_Type = "MANAG",
            Is_Admin = true,
            Is_Active = true,
            ManagNo = 1,
            Is_Manager = false,
            SecurityStamp = "6a1f0c3d-9b2e-4a57-8c31-0d5e7f9a2b46",
            // IdentityUser's constructor generates this, so it must be
            // pinned too or the model drifts on every build.
            ConcurrencyStamp = "c47b9e21-5d38-4f60-a913-7e2b8c4d6f05"
        };
        // Add the first user to ApplicationUsers
        builder.Entity<ApplicationUser>().HasData(firstUser);
        // Define the master data
        builder.Entity<MasterData>().HasData(
            new MasterData { Id = 1, Category_Name = "UserType", Master_Data_Code = "USTYP", Master_Data_Parent_Id = null, Title_Ar = "نوع المستخدم", Title_En = "User Type", Item_Order = 1, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 2, Category_Name = "UserType", Master_Data_Code = "MANAG", Master_Data_Parent_Id = 1, Title_Ar = "مدير إدارة", Title_En = "Administrator", Item_Order = 1, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 3, Category_Name = "UserType", Master_Data_Code = "PORTA", Master_Data_Parent_Id = 1, Title_Ar = "مستخدم بوابة", Title_En = "User Portal", Item_Order = 2, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 4, Category_Name = "RquestStatus", Master_Data_Code = "REQST", Master_Data_Parent_Id = null, Title_Ar = "حالة الطلب", Title_En = "Request Status", Item_Order = 1, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 5, Category_Name = "RquestStatus", Master_Data_Code = "DRAFT", Master_Data_Parent_Id = 4, Title_Ar = "نسخة", Title_En = "Draft", Item_Order = 1, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 6, Category_Name = "RquestStatus", Master_Data_Code = "APPRO", Master_Data_Parent_Id = 4, Title_Ar = "معتمد", Title_En = "Approved", Item_Order = 2, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 7, Category_Name = "RquestStatus", Master_Data_Code = "REJEC", Master_Data_Parent_Id = 4, Title_Ar = "مرفوض", Title_En = "Rejected", Item_Order = 3, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 8, Category_Name = "RquestStatus", Master_Data_Code = "UNPRC", Master_Data_Parent_Id = 4, Title_Ar = "مرسل", Title_En = "Sent", Item_Order = 5, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 9, Category_Name = "RquestStatus", Master_Data_Code = "STOPD", Master_Data_Parent_Id = 4, Title_Ar = "موقوف", Title_En = "Stopped", Item_Order = 6, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 10, Category_Name = "IssuerType", Master_Data_Code = "ISSTY", Master_Data_Parent_Id = null, Title_Ar = "نوع المصدر", Title_En = "Issuer Type", Item_Order = 1, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 11, Category_Name = "IssuerType", Master_Data_Code = "DIREC", Master_Data_Parent_Id = 10, Title_Ar = "إدخال مباشر", Title_En = "Direct entry", Item_Order = 1, Is_Active = true, Is_Deleted = false },
            new MasterData { Id = 12, Category_Name = "IssuerType", Master_Data_Code = "IMPOR", Master_Data_Parent_Id = 10, Title_Ar = "استيراد بيانات", Title_En = "Import data", Item_Order = 2, Is_Active = true, Is_Deleted = false }
        );
        // Define the first group in Security_UserGroups
        builder.Entity<UserGroup>().HasData(
            new UserGroup
            {
                Id = groupId,
                Name_Ar = "مدير إدارة",
                Name_En = "Administrator",
                Is_Active = true,
                Created_By = userId,
                Create_Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Name = "HKJHW",
                NormalizedName = "HKJHW",
                User_Type = "MANAG",
                // IdentityRole initialises this to Guid.NewGuid().ToString(), so leaving
                // it unset makes the model non-deterministic: every build produces a
                // different value and "ef database update" refuses to run. The seeded
                // user above already pins its stamp for exactly this reason.
                ConcurrencyStamp = "3e9a71d4-0b62-4c8f-95a1-7d4e2f6b8c30"
            }
        );
        // Link the first user with the first group in UserGroupsLink
        builder.Entity<UserGroupsLink>().HasData(
            new UserGroupsLink
            {
                UserId = userId,
                RoleId = groupId
            }
        );
        builder.Entity<ScreenAction>().HasData(
            new ScreenAction { Id = Guid.Parse("C1577510-4BB5-49C1-A5E4-05B001C41E7E"), Action_Name_Ar = "حفظ", Action_Name_En = "Save", Order = 13, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("2F5A8589-6E31-4514-885C-072CFB38F000"), Action_Name_Ar = "تفعيل", Action_Name_En = "Activation", Order = 5, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("F820B435-D621-41A8-80E4-32D5D1F867C7"), Action_Name_Ar = "إضافة", Action_Name_En = "Add", Order = 2, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("609B4CE8-1C5F-4FF7-A71E-3462436F3F11"), Action_Name_Ar = "إسترجاع", Action_Name_En = "Retrieve", Order = 6, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("4ADD3D50-F32C-4ABE-B8E2-3AF596588A71"), Action_Name_Ar = "تعديل", Action_Name_En = "Edit", Order = 3, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("8A231277-B772-4C59-844E-54435DC912F1"), Action_Name_Ar = "إعتماد", Action_Name_En = "Approve", Order = 11, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("328723DA-E3C9-468E-998D-6ADEA7217D37"), Action_Name_Ar = "حذف", Action_Name_En = "Delete", Order = 4, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("2DFA1EAF-97C4-4B85-B548-6D38334735E9"), Action_Name_Ar = "إرسال بريد إلكتروني", Action_Name_En = "Send Email", Order = 10, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("6EBD6AE4-7F43-45B6-9D57-AA5067A4B79F"), Action_Name_Ar = "فتح", Action_Name_En = "View", Order = 1, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("67FBC65E-214D-46F5-9FA6-AFE35CD9C3F3"), Action_Name_En = "Submit", Action_Name_Ar = "تقديم", Order = 8, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("211BD794-FFBA-4305-A278-D25D206F18AF"), Action_Name_Ar = "إستيراد بيانات", Action_Name_En = "Import Data", Order = 12, Is_Deleted = false },
            new ScreenAction { Id = Guid.Parse("F3FC395D-72D8-4432-B273-EA1D3EFB5403"), Action_Name_Ar = "طباعة", Action_Name_En = "Print", Order = 7, Is_Deleted = false }
        );
        builder.Entity<MainModule>().HasData(
            new MainModule { Id = Guid.Parse("7A0B5A10-9B94-4888-A835-D2B7A9360D1E"), Description_Ar = "البوابة", Description_En = "Portal", ApplicationNo = 2, Is_Deleted = false },
            new MainModule { Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Description_Ar = "الإدارة", Description_En = "Managment", ApplicationNo = 1, Is_Deleted = false }
        );
        builder.Entity<Screen>().HasData(
            new Screen { Id = Guid.Parse("503ADB95-49A7-4FE7-B28C-0BE7456C91BE"), Main_Module_Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Screen_Description_Ar = "لوحة القيادة", Screen_Description_En = "Dashboard", Parent_Screen_Id = null, Link = "admin/home", Is_Branch = false, Order = 1, Menu_Or_Not = true, No_Login = true, Screen_Icon = "mat_outline:home_work", Is_Deleted = false },
            new Screen { Id = Guid.Parse("E3035FC1-9B23-49B5-90F7-470F79AD1D5B"), Main_Module_Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Screen_Description_Ar = "تعريف المجموعات", Screen_Description_En = "Group Definition", Parent_Screen_Id = Guid.Parse("619BCF7F-BD18-4B93-A8CA-A0F400ED105B"), Link = "admin/groupDefinition", Is_Branch = true, Order = 1, Menu_Or_Not = true, No_Login = true, Screen_Icon = "heroicons_outline:user-group", Is_Deleted = false },
            new Screen { Id = Guid.Parse("E70A846E-7A3C-4AB4-BD9D-5F8310C3A24D"), Main_Module_Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Screen_Description_Ar = "المستخدمين", Screen_Description_En = "Users", Parent_Screen_Id = Guid.Parse("619BCF7F-BD18-4B93-A8CA-A0F400ED105B"), Link = "admin/contacts", Is_Branch = true, Order = 1, Menu_Or_Not = true, No_Login = true, Screen_Icon = "heroicons_outline:users", Is_Deleted = false },
            new Screen { Id = Guid.Parse("E70A846E-7A3C-4AB4-BD6D-5F8320C3A24D"), Main_Module_Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Screen_Description_Ar = "صلاحيات المستخدمين", Screen_Description_En = "User Permission", Parent_Screen_Id = Guid.Parse("619BCF7F-BD18-4B93-A8CA-A0F400ED105B"), Link = "admin/permission", Is_Branch = true, Order = 1, Menu_Or_Not = true, No_Login = true, Screen_Icon = "heroicons_outline:key", Is_Deleted = false },
            new Screen { Id = Guid.Parse("FEA9385D-7C1D-43ED-AE82-6C2FA1E48690"), Main_Module_Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Screen_Description_Ar = "التقارير", Screen_Description_En = "Reports", Parent_Screen_Id = null, Link = "#", Is_Branch = false, Order = 4, Menu_Or_Not = true, No_Login = true, Screen_Icon = "heroicons_outline:clipboard-document-list", Is_Deleted = false },
            new Screen { Id = Guid.Parse("619BCF7F-BD18-4B93-A8CA-A0F400ED105B"), Main_Module_Id = Guid.Parse("986B68AC-22FF-4CEE-991D-DBDFE4B12BBB"), Screen_Description_Ar = "الحماية", Screen_Description_En = "SECURITY", Parent_Screen_Id = null, Link = "#", Is_Branch = false, Order = 2, Menu_Or_Not = true, No_Login = true, Screen_Icon = "heroicons_outline:shield-exclamation", Is_Deleted = false }
        );
        builder.Entity<LinkScreenAction>().HasData(
            new LinkScreenAction { Id = Guid.Parse("1F68DC32-021A-43FD-8EE1-023C1CE402A4"), Screen_Id = Guid.Parse("E3035FC1-9B23-49B5-90F7-470F79AD1D5B"), Screen_Action_Id = Guid.Parse("F820B435-D621-41A8-80E4-32D5D1F867C7"), Base_Route = "managment/api/Group/CreateGroup", Action_Code = "AAEGA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("48984F43-658B-4ADF-8788-248F54D1FAA0"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD6D-5F8320C3A24D"), Screen_Action_Id = Guid.Parse("6EBD6AE4-7F43-45B6-9D57-AA5067A4B79F"), Base_Route = "managment/api/GroupAction/Get", Action_Code = "GUACS", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("E4FFA587-2100-4F64-8794-29C9162D4CE8"), Screen_Id = Guid.Parse("E3035FC1-9B23-49B5-90F7-470F79AD1D5B"), Screen_Action_Id = Guid.Parse("328723DA-E3C9-468E-998D-6ADEA7217D37"), Base_Route = "managment/api/Group/DeleteGroup", Action_Code = "DDDGA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("E80A271D-97E9-4896-8442-4193E3D8F8C3"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD9D-5F8310C3A24D"), Screen_Action_Id = Guid.Parse("6EBD6AE4-7F43-45B6-9D57-AA5067A4B79F"), Base_Route = "managment/api/User/GetUser", Action_Code = "GCSRS", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("7E7E47F6-961B-4C60-9B3A-62981DC83326"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD6D-5F8320C3A24D"), Screen_Action_Id = Guid.Parse("4ADD3D50-F32C-4ABE-B8E2-3AF596588A71"), Base_Route = "managment/api/GroupAction/AddEdit", Action_Code = "AAEUA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("9741C3A7-167B-4731-92DD-6EC28780F520"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD9D-5F8310C3A24D"), Screen_Action_Id = Guid.Parse("4ADD3D50-F32C-4ABE-B8E2-3AF596588A71"), Base_Route = "managment/api/User/EditUser", Action_Code = "EUSRA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("B6A4E50E-9EC8-45FB-AE22-807F10B2679E"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD9D-5F8310C3A24D"), Screen_Action_Id = Guid.Parse("328723DA-E3C9-468E-998D-6ADEA7217D37"), Base_Route = "managment/api/User/DeleteUser", Action_Code = "DUSRA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("FF0F7C6D-B778-41DC-9524-9D2961326EE6"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD9D-5F8310C3A24D"), Screen_Action_Id = Guid.Parse("F820B435-D621-41A8-80E4-32D5D1F867C7"), Base_Route = "managment/api/User/CreateUser", Action_Code = "CUSRA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("8FDE4946-152D-45D5-96A0-E25D5F2CB274"), Screen_Id = Guid.Parse("E70A846E-7A3C-4AB4-BD6D-5F8320C3A24D"), Screen_Action_Id = Guid.Parse("328723DA-E3C9-468E-998D-6ADEA7217D37"), Base_Route = "managment/api/GroupAction/Delete", Action_Code = "DDDUA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("77C404D1-2F37-4728-9608-E9B19194AE07"), Screen_Id = Guid.Parse("E3035FC1-9B23-49B5-90F7-470F79AD1D5B"), Screen_Action_Id = Guid.Parse("4ADD3D50-F32C-4ABE-B8E2-3AF596588A71"), Base_Route = "managment/api/Group/EditGroup", Action_Code = "EAEGA", Is_Deleted = false },
            new LinkScreenAction { Id = Guid.Parse("0C2244D0-FC92-479F-9D57-FFE9A38A0246"), Screen_Id = Guid.Parse("E3035FC1-9B23-49B5-90F7-470F79AD1D5B"), Screen_Action_Id = Guid.Parse("6EBD6AE4-7F43-45B6-9D57-AA5067A4B79F"), Base_Route = "managment/api/Group/GetGroups", Action_Code = "GGGGA", Is_Deleted = false }
        );
        builder.Entity<GroupPermissions>().HasData(
            new GroupPermissions { Id = Guid.Parse("D096D0B6-6D8B-4D45-AF7E-0263CB10915F"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("1F68DC32-021A-43FD-8EE1-023C1CE402A4"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("08EB862E-814E-4DE3-8827-34BABA6C5868"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("48984F43-658B-4ADF-8788-248F54D1FAA0"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("E001EF51-8AF3-4BFA-87A7-361C89B8060B"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("E4FFA587-2100-4F64-8794-29C9162D4CE8"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("B33FEAEF-5EB5-406F-8D75-3CD783EEC7FB"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("E80A271D-97E9-4896-8442-4193E3D8F8C3"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("444734F1-BE36-4FAB-B2C6-77E74386960E"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("7E7E47F6-961B-4C60-9B3A-62981DC83326"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("32429140-179E-4DF8-A2A0-7B7F187F3259"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("9741C3A7-167B-4731-92DD-6EC28780F520"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("13ADDEE3-F52C-46F9-9E1B-94C44BD31D60"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("B6A4E50E-9EC8-45FB-AE22-807F10B2679E"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("B2B92C00-F4D0-4BEA-9E48-E153E71F3649"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("FF0F7C6D-B778-41DC-9524-9D2961326EE6"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("0A1F7123-3068-453E-AE72-EC8F3F673EEA"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("8FDE4946-152D-45D5-96A0-E25D5F2CB274"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("852C6945-708D-4A3B-86A8-F13EB9BFC0BA"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("77C404D1-2F37-4728-9608-E9B19194AE07"), Is_Deleted = false },
            new GroupPermissions { Id = Guid.Parse("5F794278-2C3F-48D2-B7F0-F318DADD3331"), Group_Id = groupId, Link_Screen_Action_Id = Guid.Parse("0C2244D0-FC92-479F-9D57-FFE9A38A0246"), Is_Deleted = false }
        );
    }
    #endregion
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies();
    }
}
