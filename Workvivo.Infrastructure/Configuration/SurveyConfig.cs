using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration;

public class SurveyConfig : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("Srv_Surveys");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Title_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Description_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Description_En).HasMaxLength(ColumnLengths.Description);
        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => new { x.Status, x.Start_Date, x.End_Date })
            .HasDatabaseName("IX_Srv_Surveys_Window");
    }
}

public class SurveyQuestionConfig : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
    {
        builder.ToTable("Srv_Questions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text_Ar).IsRequired().HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Text_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Help_Text_Ar).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Help_Text_En).HasMaxLength(ColumnLengths.Description);
        builder.Property(x => x.Min_Label_Ar).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Min_Label_En).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Max_Label_Ar).HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Max_Label_En).HasMaxLength(ColumnLengths.Name);

        builder.HasIndex(x => new { x.Survey_Id, x.Sort_Order })
            .HasDatabaseName("IX_Srv_Questions_Survey");

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.Survey_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyQuestionOptionConfig : IEntityTypeConfiguration<SurveyQuestionOption>
{
    public void Configure(EntityTypeBuilder<SurveyQuestionOption> builder)
    {
        builder.ToTable("Srv_QuestionOptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text_Ar).IsRequired().HasMaxLength(ColumnLengths.Name);
        builder.Property(x => x.Text_En).HasMaxLength(ColumnLengths.Name);

        builder.HasIndex(x => new { x.Question_Id, x.Sort_Order })
            .HasDatabaseName("IX_Srv_QuestionOptions_Question");

        builder.HasOne(x => x.Question)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.Question_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyResponseConfig : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("Srv_Responses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Respondent_Hash).HasMaxLength(ColumnLengths.Sha256Hex);

        // One response per person, unless the survey allows several. Enforced on the
        // identified path and, separately, on the anonymous one.
        builder.HasIndex(x => new { x.Survey_Id, x.Employee_Id })
            .IsUnique()
            .HasDatabaseName("UX_Srv_Responses_Identified")
            .HasFilter("[Employee_Id] IS NOT NULL AND [Is_Deleted] = 0");

        builder.HasIndex(x => new { x.Survey_Id, x.Respondent_Hash })
            .IsUnique()
            .HasDatabaseName("UX_Srv_Responses_Anonymous")
            .HasFilter("[Respondent_Hash] IS NOT NULL AND [Is_Deleted] = 0");

        // Results filtered by department or office, which is how survey results are
        // read in practice.
        builder.HasIndex(x => new { x.Survey_Id, x.Is_Complete, x.Department_Id_At_Submission })
            .HasDatabaseName("IX_Srv_Responses_Analysis");

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.Responses)
            .HasForeignKey(x => x.Survey_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.Employee_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurveyAnswerConfig : IEntityTypeConfiguration<SurveyAnswer>
{
    public void Configure(EntityTypeBuilder<SurveyAnswer> builder)
    {
        builder.ToTable("Srv_Answers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text_Value).HasMaxLength(ColumnLengths.LongText);

        builder.HasIndex(x => x.Response_Id).HasDatabaseName("IX_Srv_Answers_Response");

        // Aggregating one question across every response - the shape of every chart on
        // the results page.
        builder.HasIndex(x => new { x.Question_Id, x.Option_Id })
            .HasDatabaseName("IX_Srv_Answers_Question")
            .IncludeProperties(x => x.Numeric_Value);

        builder.HasOne(x => x.Response)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.Response_Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Question)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.Question_Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Option)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.Option_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurveyAudienceConfig : IEntityTypeConfiguration<SurveyAudience>
{
    public void Configure(EntityTypeBuilder<SurveyAudience> builder)
    {
        AudienceConfiguration.Apply(builder, "Srv_Audiences", nameof(SurveyAudience.Survey_Id));

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.Audiences)
            .HasForeignKey(x => x.Survey_Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
