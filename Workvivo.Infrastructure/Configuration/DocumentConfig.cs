using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class FileAssetConfig : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        builder.ToTable("Doc_Files");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Storage_Key).IsRequired().HasMaxLength(ColumnLengths.Url);
        builder.Property(x => x.Original_File_Name).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Content_Type).IsRequired().HasMaxLength(ColumnLengths.Code);
        builder.Property(x => x.Extension).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Checksum_Sha256).IsRequired().HasMaxLength(ColumnLengths.Sha256Hex);
        builder.Property(x => x.Container).IsRequired().HasMaxLength(ColumnLengths.Code);

        // De-duplication: the same file uploaded twice should be stored once.
        builder.HasIndex(x => x.Checksum_Sha256).HasDatabaseName("IX_Doc_Files_Checksum");

        // The cleanup job's query, and the scanner's backlog.
        builder.HasIndex(x => x.Scan_Status)
            .HasDatabaseName("IX_Doc_Files_ScanStatus")
            .HasFilter("[Scan_Status] = 0");

        builder.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.Uploaded_By)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Doc_Files_SizePositive",
            "[Size_Bytes] > 0"));
    }
}

public class DocumentCategoryConfig : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> builder)
    {
        builder.ToTable("Doc_Categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Name_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Icon).HasMaxLength(ColumnLengths.Code);

        builder.HasOne(x => x.ParentCategory)
            .WithMany(x => x.ChildCategories)
            .HasForeignKey(x => x.Parent_Category_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentConfig : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Doc_Documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Title_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => new { x.Category_Id, x.Is_Published })
            .HasDatabaseName("IX_Doc_Documents_Category");

        // The review-due report.
        builder.HasIndex(x => x.Review_Date)
            .HasDatabaseName("IX_Doc_Documents_Review")
            .HasFilter("[Review_Date] IS NOT NULL AND [Is_Deleted] = 0");

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.Category_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.Owner_Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Document and DocumentVersion point at each other. This side stays Restrict
        // and has no inverse navigation, so EF treats it as its own relationship rather
        // than pairing it with Document.Versions.
        builder.HasOne(x => x.CurrentVersion)
            .WithMany()
            .HasForeignKey(x => x.Current_Version_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentVersionConfig : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("Doc_Versions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Change_Note).HasMaxLength(ColumnLengths.LongText);

        builder.HasIndex(x => new { x.Document_Id, x.Version_Number })
            .IsUnique()
            .HasDatabaseName("UX_Doc_Versions_Number");

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.Document_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.File)
            .WithMany()
            .HasForeignKey(x => x.File_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Doc_Versions_NumberPositive",
            "[Version_Number] > 0"));
    }
}

public class DocumentAudienceConfig : IEntityTypeConfiguration<DocumentAudience>
{
    public void Configure(EntityTypeBuilder<DocumentAudience> builder)
    {
        AudienceConfiguration.Apply(builder, "Doc_Audiences", nameof(DocumentAudience.Document_Id));

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Audiences)
            .HasForeignKey(x => x.Document_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentDownloadLogConfig : IEntityTypeConfiguration<DocumentDownloadLog>
{
    public void Configure(EntityTypeBuilder<DocumentDownloadLog> builder)
    {
        builder.ToTable("Doc_DownloadLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Ip_Address).HasMaxLength(ColumnLengths.IpAddress);
        builder.Property(x => x.User_Agent).HasMaxLength(ColumnLengths.UserAgent);

        // "Who has read the updated code of conduct" - the compliance question this
        // table exists to answer.
        builder.HasIndex(x => new { x.Document_Id, x.Downloaded_At })
            .HasDatabaseName("IX_Doc_DownloadLogs_Document")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.Employee_Id, x.Downloaded_At })
            .HasDatabaseName("IX_Doc_DownloadLogs_Employee")
            .IsDescending(false, true);

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Downloads)
            .HasForeignKey(x => x.Document_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Version)
            .WithMany(x => x.Downloads)
            .HasForeignKey(x => x.Version_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
