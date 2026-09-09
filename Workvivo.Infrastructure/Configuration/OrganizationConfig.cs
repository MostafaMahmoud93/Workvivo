using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class OrganizationConfig : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Org_Organizations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Legal_Name).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Website).HasMaxLength(ColumnLengths.Url);
    }
}

public class DepartmentConfig : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Org_Departments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Path).IsRequired().HasMaxLength(ColumnLengths.Path);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Org_Departments_Code");

        // Subtree lookups run as Path LIKE 'prefix%', which is a range seek only while
        // Path leads the index.
        builder.HasIndex(x => x.Path).HasDatabaseName("IX_Org_Departments_Path");

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.Departments)
            .HasForeignKey(x => x.Organization_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict on the self-reference: cascading would let one delete take out an
        // entire branch of the org chart, and SQL Server refuses cascade on a
        // self-referencing foreign key in any case.
        builder.HasOne(x => x.ParentDepartment)
            .WithMany(x => x.ChildDepartments)
            .HasForeignKey(x => x.Parent_Department_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Department -> Manager -> Employee -> Department is a cycle; every edge of it
        // is Restrict so SQL Server has no multiple cascade path to reject.
        builder.HasOne(x => x.Manager)
            .WithMany()
            .HasForeignKey(x => x.Manager_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TeamConfig : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Org_Teams");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);

        builder.HasIndex(x => x.Department_Id).HasDatabaseName("IX_Org_Teams_Department");

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Teams)
            .HasForeignKey(x => x.Department_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Lead)
            .WithMany()
            .HasForeignKey(x => x.Lead_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LocationConfig : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Org_Locations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Country).HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.City).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Address_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Address_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.TimeZone_Id).HasMaxLength(ColumnLengths.Code);

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.Locations)
            .HasForeignKey(x => x.Organization_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobTitleConfig : IEntityTypeConfiguration<JobTitle>
{
    public void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        builder.ToTable("Org_JobTitles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Grade).HasMaxLength(ColumnLengths.Code);

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.JobTitles)
            .HasForeignKey(x => x.Organization_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeConfig : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Org_Employees");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Employee_Number).IsRequired().HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.First_Name).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Middle_Name).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Last_Name).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Display_Name).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Full_Name_Ar).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(ColumnLengths.Email);
        builder.Property(x => x.Mobile).HasMaxLength(ColumnLengths.Phone);
        builder.Property(x => x.Extension).HasMaxLength(ColumnLengths.Phone);
        builder.Property(x => x.Biography_Ar).HasMaxLength(ColumnLengths.LongText);
        builder.Property(x => x.Biography_En).HasMaxLength(ColumnLengths.LongText);
        builder.Property(x => x.Preferred_Language).IsRequired().HasMaxLength(8);

        builder.Property(x => x.RowVersion).IsRowVersion();

        // One profile per login, enforced by the database rather than by convention:
        // a second Employee row for the same user would silently split someone's
        // identity across the product.
        builder.HasIndex(x => x.User_Id).IsUnique().HasDatabaseName("UX_Org_Employees_User");
        builder.HasIndex(x => x.Employee_Number).IsUnique().HasDatabaseName("UX_Org_Employees_Number");
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_Org_Employees_Email");

        // The employee directory's default listing, and the shape every departmental
        // filter takes.
        builder.HasIndex(x => new { x.Is_Deleted, x.Is_Active, x.Department_Id })
            .HasDatabaseName("IX_Org_Employees_Active_Department")
            .IncludeProperties(x => new { x.Display_Name, x.Job_Title_Id, x.Location_Id });

        // Drives the birthdays panel, which asks for a day and month across the whole
        // company - so the filtered index carries only the rows that can ever match.
        builder.HasIndex(x => x.Birth_Date)
            .HasDatabaseName("IX_Org_Employees_Birthday")
            .HasFilter("[Show_Birthday] = 1 AND [Is_Deleted] = 0");

        // New joiners panel.
        builder.HasIndex(x => x.Joining_Date).HasDatabaseName("IX_Org_Employees_Joining");

        builder.HasOne(x => x.User)
            .WithOne(x => x.Employee)
            .HasForeignKey<Employee>(x => x.User_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.Department_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.Team_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.Location_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.JobTitle)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.Job_Title_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeManagerConfig : IEntityTypeConfiguration<EmployeeManager>
{
    public void Configure(EntityTypeBuilder<EmployeeManager> builder)
    {
        builder.ToTable("Org_EmployeeManagers");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Employee_Id, x.Manager_Id, x.Effective_From })
            .IsUnique()
            .HasDatabaseName("UX_Org_EmployeeManagers_Relationship");

        // At most one current primary manager per employee. A filtered unique index
        // says exactly that, where a plain unique index would also forbid the historic
        // rows that make the dated design worth having.
        builder.HasIndex(x => x.Employee_Id)
            .IsUnique()
            .HasDatabaseName("UX_Org_EmployeeManagers_CurrentPrimary")
            .HasFilter("[Is_Primary] = 1 AND [Effective_To] IS NULL AND [Is_Deleted] = 0");

        builder.HasIndex(x => x.Manager_Id).HasDatabaseName("IX_Org_EmployeeManagers_Manager");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.Managers)
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Manager)
            .WithMany(x => x.DirectReports)
            .HasForeignKey(x => x.Manager_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Org_EmployeeManagers_NotSelf",
            "[Employee_Id] <> [Manager_Id]"));
    }
}

public class SkillConfig : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Org_Skills");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Category).HasMaxLength(ColumnLengths.Code);

        builder.HasIndex(x => x.Name_Ar).IsUnique().HasDatabaseName("UX_Org_Skills_NameAr");
    }
}

public class EmployeeSkillConfig : IEntityTypeConfiguration<EmployeeSkill>
{
    public void Configure(EntityTypeBuilder<EmployeeSkill> builder)
    {
        builder.ToTable("Org_EmployeeSkills");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Employee_Id, x.Skill_Id })
            .IsUnique()
            .HasDatabaseName("UX_Org_EmployeeSkills");

        // "Who here knows X" - the reason the skill catalogue exists at all.
        builder.HasIndex(x => x.Skill_Id).HasDatabaseName("IX_Org_EmployeeSkills_Skill");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.Skills)
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Skill)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.Skill_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InterestConfig : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("Org_Interests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Icon).HasMaxLength(ColumnLengths.Code);

        builder.HasIndex(x => x.Name_Ar).IsUnique().HasDatabaseName("UX_Org_Interests_NameAr");
    }
}

public class EmployeeInterestConfig : IEntityTypeConfiguration<EmployeeInterest>
{
    public void Configure(EntityTypeBuilder<EmployeeInterest> builder)
    {
        builder.ToTable("Org_EmployeeInterests");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Employee_Id, x.Interest_Id })
            .IsUnique()
            .HasDatabaseName("UX_Org_EmployeeInterests");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.Interests)
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Interest)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.Interest_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeFollowerConfig : IEntityTypeConfiguration<EmployeeFollower>
{
    public void Configure(EntityTypeBuilder<EmployeeFollower> builder)
    {
        builder.ToTable("Org_EmployeeFollowers");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Follower_Id, x.Followee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Org_EmployeeFollowers");

        // Both directions are read: "who follows me" and "whose posts should reach me".
        builder.HasIndex(x => x.Followee_Id).HasDatabaseName("IX_Org_EmployeeFollowers_Followee");

        builder.HasOne(x => x.Follower)
            .WithMany(x => x.Following)
            .HasForeignKey(x => x.Follower_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Followee)
            .WithMany(x => x.Followers)
            .HasForeignKey(x => x.Followee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Org_EmployeeFollowers_NotSelf",
            "[Follower_Id] <> [Followee_Id]"));
    }
}
