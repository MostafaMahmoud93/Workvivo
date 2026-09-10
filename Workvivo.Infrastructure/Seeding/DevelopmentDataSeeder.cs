using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Infrastructure.Abstractions;
using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Seeding;

/// <summary>
/// Sample organisation and staff, for development only.
///
/// Deliberately not in a migration. Reference data - roles, permissions, categories -
/// is part of the schema contract and ships everywhere; invented people are not, and a
/// migration is the wrong place to define them. Keeping them here means production
/// simply never runs this.
///
/// Idempotent: it checks for its own marker and does nothing on a second run, so
/// restarting the API does not accumulate duplicate staff.
/// </summary>
public sealed class DevelopmentDataSeeder
{
    /// <summary>
    /// The one credential every seeded account shares.
    ///
    /// Safe only because this class refuses to run outside Development. It is a known
    /// value in a public repository, which is exactly why the environment check below
    /// is a hard guard rather than a convention.
    /// </summary>
    private const string SeedPassword = "Passw0rd!Dev";

    private const string MarkerCode = "SEED";

    private readonly Workvivo_DbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        Workvivo_DbContext context,
        UserManager<ApplicationUser> userManager,
        IHostEnvironment environment,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            // Not a warning to be tuned out - a refusal. Sample staff with a shared,
            // published password must never exist in a real environment.
            _logger.LogInformation(
                "Development data seeding skipped: environment is {Environment}",
                _environment.EnvironmentName);
            return;
        }

        // Per-area rather than one gate over everything.
        //
        // A single "is anything seeded?" check was right while there was one phase of
        // content. It is wrong now: each phase adds sample data, and a developer whose
        // database predates that phase would have to drop it to see the new feature.
        // Every step below is a no-op when its own tables already hold rows.
        var seededPeople = false;

        if (!await _context.Org_Organizations.AnyAsync(cancellationToken))
        {
            _logger.LogWarning(
                "Seeding development data. Every seeded account uses the same well-known password.");

            var organization = await SeedOrganizationAsync(cancellationToken);
            var locations = await SeedLocationsAsync(organization.Id, cancellationToken);
            var jobTitles = await SeedJobTitlesAsync(organization.Id, cancellationToken);
            var departments = await SeedDepartmentsAsync(organization.Id, cancellationToken);
            var teams = await SeedTeamsAsync(departments, cancellationToken);
            var people = await SeedEmployeesAsync(departments, teams, locations, jobTitles, cancellationToken);

            await SeedReportingLinesAsync(people, departments, cancellationToken);
            await SeedFollowsAsync(people, cancellationToken);
            await SeedSkillsAsync(people, cancellationToken);
            await SeedPostsAsync(people, departments, locations, cancellationToken);

            _logger.LogInformation("Seeded {Count} development employees", people.Count);
            seededPeople = true;
        }

        var employees = await _context.Org_Employees.ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return;
        }

        await SeedCommunitiesAsync(employees, cancellationToken);
        await SeedPollsAndSurveysAsync(employees, cancellationToken);
        await SeedEventsAsync(employees, cancellationToken);

        if (!seededPeople)
        {
            _logger.LogInformation("Development data topped up for the newer feature areas");
        }
    }

    /// <summary>
    /// Three events: one company-wide and physical, one online, and one already full.
    ///
    /// The full one exists so the capacity rule is reachable without setting it up by
    /// hand - it is the case where an RSVP has to be refused for a joiner and still
    /// permitted for somebody withdrawing.
    /// </summary>
    private async Task SeedEventsAsync(List<Employee> employees, CancellationToken cancellationToken)
    {
        if (await _context.Evt_Events.AnyAsync(cancellationToken))
        {
            return;
        }

        Employee Person(string key) =>
            employees.FirstOrDefault(employee => employee.Id == DeterministicGuid.From($"dev:emp:{key}"))
            ?? employees[0];

        var now = DateTime.UtcNow;

        var definitions = new (string Key, string En, string Ar, EventFormat Format, int DaysAhead,
            int Hours, int? Capacity, string? Url, string Organizer)[]
        {
            ("townhall", "Quarterly town hall", "اللقاء الربعي",
                EventFormat.Hybrid, 3, 1, null, "https://meet.example.com/townhall", "khalid"),

            ("onboarding", "New joiner welcome", "ترحيب بالموظفين الجدد",
                EventFormat.Online, 5, 1, null, "https://meet.example.com/welcome", "khalid"),

            ("workshop", "Design systems workshop", "ورشة أنظمة التصميم",
                EventFormat.Physical, 8, 3, 2, null, "amira"),
        };

        foreach (var definition in definitions)
        {
            var id = DeterministicGuid.From($"dev:event:{definition.Key}");
            var organizer = Person(definition.Organizer);
            var start = now.AddDays(definition.DaysAhead);

            _context.Evt_Events.Add(new Event
            {
                Id = id,
                Title_En = definition.En,
                Title_Ar = definition.Ar,
                Description_En = "Seeded development event.",
                Description_Ar = "فعالية تجريبية للتطوير.",
                Event_Type = EventType.Company,
                Format = definition.Format,
                Status = EventStatus.Published,
                Start_At = start,
                End_At = start.AddHours(definition.Hours),
                TimeZone_Id = "Asia/Dubai",
                Is_All_Day = false,
                Meeting_Url = definition.Url,
                Organizer_Employee_Id = organizer.Id,
                Capacity = definition.Capacity,
                Attendees_Count = 0,
                Requires_Rsvp = true,
                Is_Deleted = false,
                Created_By = organizer.User_Id,
                Create_Date = now,
            });

            _context.Evt_Audiences.Add(new EventAudience(id, AudienceType.AllEmployees, null)
            {
                Id = DeterministicGuid.From($"dev:eventaudience:{definition.Key}"),
            });
        }

        // The workshop is seeded at its capacity of two, so "full" is reachable
        // immediately rather than after somebody sets it up.
        var workshopId = DeterministicGuid.From("dev:event:workshop");

        foreach (var key in new[] { "yusuf", "tariq" })
        {
            var person = Person(key);

            _context.Evt_Attendees.Add(new EventAttendee
            {
                Id = DeterministicGuid.From($"dev:eventattendee:{key}"),
                Event_Id = workshopId,
                Employee_Id = person.Id,
                Response = EventResponse.Attending,
                Responded_At = now,
                Is_Deleted = false,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var workshop = await _context.Evt_Events.FirstAsync(e => e.Id == workshopId, cancellationToken);
        workshop.Attendees_Count = 2;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} development events", definitions.Length);
    }

    /// <summary>
    /// One open poll and one open survey, both addressed to everybody.
    ///
    /// The survey deliberately mixes question types - a scale, a choice and free text -
    /// because the results aggregation treats each differently and a seed with one
    /// type would exercise a third of it.
    /// </summary>
    private async Task SeedPollsAndSurveysAsync(
        List<Employee> employees,
        CancellationToken cancellationToken)
    {
        if (await _context.Poll_Polls.AnyAsync(cancellationToken)
            || await _context.Srv_Surveys.AnyAsync(cancellationToken))
        {
            return;
        }

        var author = employees.FirstOrDefault(
            employee => employee.Id == DeterministicGuid.From("dev:emp:khalid")) ?? employees[0];

        var now = DateTime.UtcNow;

        var pollId = DeterministicGuid.From("dev:poll:lunch");

        _context.Poll_Polls.Add(new Poll
        {
            Id = pollId,
            Question_En = "Where should the next team lunch be?",
            Question_Ar = "أين تفضّل غداء الفريق القادم؟",
            Is_Multiple_Choice = false,
            Is_Anonymous = false,
            Show_Results_Before_Voting = false,
            Status = PollStatus.Open,
            Start_Date = now.AddHours(-2),
            Expiry_Date = now.AddDays(7),
            Total_Votes = 0,
            Is_Deleted = false,
            Created_By = author.User_Id,
            Create_Date = now.AddHours(-2),
        });

        var options = new (string Key, string En, string Ar)[]
        {
            ("levant", "The Levantine place downstairs", "المطعم الشامي في الأسفل"),
            ("indian", "Indian on the corner", "المطعم الهندي في الزاوية"),
            ("picnic", "Picnic in the park", "نزهة في الحديقة"),
        };

        var order = 0;

        foreach (var option in options)
        {
            _context.Poll_Options.Add(new PollOption
            {
                Id = DeterministicGuid.From($"dev:polloption:{option.Key}"),
                Poll_Id = pollId,
                Text_En = option.En,
                Text_Ar = option.Ar,
                Sort_Order = order++,
                Votes_Count = 0,
                Is_Deleted = false,
            });
        }

        _context.Poll_Audiences.Add(new PollAudience(pollId, AudienceType.AllEmployees, null)
        {
            Id = DeterministicGuid.From("dev:pollaudience:lunch"),
        });

        var surveyId = DeterministicGuid.From("dev:survey:pulse");

        _context.Srv_Surveys.Add(new Survey
        {
            Id = surveyId,
            Title_En = "Quarterly pulse check",
            Title_Ar = "استبيان النبض الربعي",
            Description_En = "Five minutes, and it is anonymous. Tell us how this quarter has felt.",
            Description_Ar = "خمس دقائق، وهو مجهول الهوية. أخبرنا كيف كان هذا الربع.",
            Status = SurveyStatus.Published,

            // Anonymous on purpose: it is the case with the interesting behaviour -
            // the keyed respondent hash, and verbatim comments withheld below the
            // threshold.
            Is_Anonymous = true,
            Allow_Multiple_Responses = false,
            Start_Date = now.AddDays(-1),
            End_Date = now.AddDays(14),
            Response_Count = 0,
            Invited_Count = employees.Count,
            Is_Deleted = false,
            Created_By = author.User_Id,
            Create_Date = now.AddDays(-1),
        });

        _context.Srv_Audiences.Add(new SurveyAudience(surveyId, AudienceType.AllEmployees, null)
        {
            Id = DeterministicGuid.From("dev:surveyaudience:pulse"),
        });

        var questions = new (string Key, SurveyQuestionType Type, string En, string Ar, bool Required)[]
        {
            ("workload", SurveyQuestionType.Scale,
                "How manageable has your workload been?", "كيف كان حجم عملك؟", true),
            ("recommend", SurveyQuestionType.Nps,
                "How likely are you to recommend us as a place to work?",
                "ما مدى احتمال أن توصي بالعمل معنا؟", true),
            ("blocker", SurveyQuestionType.SingleChoice,
                "What slowed you down most?", "ما الذي أعاقك أكثر؟", false),
            ("anything", SurveyQuestionType.Text,
                "Anything else we should know?", "هل من شيء آخر ينبغي أن نعرفه؟", false),
        };

        var questionOrder = 0;

        foreach (var question in questions)
        {
            var questionId = DeterministicGuid.From($"dev:surveyq:{question.Key}");

            _context.Srv_Questions.Add(new SurveyQuestion
            {
                Id = questionId,
                Survey_Id = surveyId,
                Question_Type = question.Type,
                Text_En = question.En,
                Text_Ar = question.Ar,
                Is_Required = question.Required,
                Sort_Order = questionOrder++,
                Min_Value = question.Type == SurveyQuestionType.Scale ? 1 : null,
                Max_Value = question.Type == SurveyQuestionType.Scale ? 5 : null,
                Min_Label_En = question.Type == SurveyQuestionType.Scale ? "Overwhelming" : null,
                Min_Label_Ar = question.Type == SurveyQuestionType.Scale ? "مرهق" : null,
                Max_Label_En = question.Type == SurveyQuestionType.Scale ? "Comfortable" : null,
                Max_Label_Ar = question.Type == SurveyQuestionType.Scale ? "مريح" : null,
                Is_Deleted = false,
                Created_By = author.User_Id,
                Create_Date = now.AddDays(-1),
            });

            if (question.Type != SurveyQuestionType.SingleChoice)
            {
                continue;
            }

            var choices = new (string Key, string En, string Ar)[]
            {
                ("meetings", "Too many meetings", "كثرة الاجتماعات"),
                ("waiting", "Waiting on other teams", "انتظار فرق أخرى"),
                ("tools", "Tooling and access", "الأدوات والصلاحيات"),
                ("nothing", "Nothing in particular", "لا شيء بعينه"),
            };

            var choiceOrder = 0;

            foreach (var choice in choices)
            {
                _context.Srv_QuestionOptions.Add(new SurveyQuestionOption
                {
                    Id = DeterministicGuid.From($"dev:surveyopt:{choice.Key}"),
                    Question_Id = questionId,
                    Text_En = choice.En,
                    Text_Ar = choice.Ar,
                    Sort_Order = choiceOrder++,
                    Is_Deleted = false,
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded a development poll and survey");
    }

    /// <summary>
    /// Two communities with different privacy settings and a pending request.
    ///
    /// Chosen so the interesting cases are reachable without setting them up by hand:
    /// a Public one anybody can join, a Restricted one that exercises the approval
    /// queue, and somebody already waiting in it.
    /// </summary>
    private async Task SeedCommunitiesAsync(List<Employee> employees, CancellationToken cancellationToken)
    {
        if (await _context.Comm_Communities.AnyAsync(cancellationToken))
        {
            return;
        }

        Employee? Person(string key) =>
            employees.FirstOrDefault(employee => employee.Id == DeterministicGuid.From($"dev:emp:{key}"));

        var owner = Person("amira") ?? employees[0];
        var moderator = Person("hana") ?? employees[0];
        var now = DateTime.UtcNow;

        var definitions = new (string Key, string NameEn, string NameAr, string DescriptionEn,
            string DescriptionAr, CommunityPrivacy Privacy, Employee Owner)[]
        {
            ("football", "Football Club", "نادي كرة القدم",
                "Five-a-side every Wednesday evening. All levels welcome.",
                "مباراة كل أربعاء مساءً. الجميع مرحّب بهم.",
                CommunityPrivacy.Public, owner),

            ("design-guild", "Design Guild", "مجلس التصميم",
                "Critique, patterns and the design system. Ask to join.",
                "نقد التصاميم والأنماط ونظام التصميم. اطلب الانضمام.",
                CommunityPrivacy.Restricted, moderator),
        };

        foreach (var definition in definitions)
        {
            var id = DeterministicGuid.From($"dev:community:{definition.Key}");

            _context.Comm_Communities.Add(new Community
            {
                Id = id,
                Slug = CommunitySlug.From(definition.NameEn, definition.NameAr),
                Name_En = definition.NameEn,
                Name_Ar = definition.NameAr,
                Description_En = definition.DescriptionEn,
                Description_Ar = definition.DescriptionAr,
                Privacy = definition.Privacy,
                Owner_Employee_Id = definition.Owner.Id,
                Is_Active = true,
                Is_Featured = definition.Key == "football",
                Members_Count = 0,
                Is_Deleted = false,
                Created_By = definition.Owner.User_Id,
                Create_Date = now,
            });

            _context.Comm_Members.Add(new CommunityMember
            {
                Id = DeterministicGuid.From($"dev:commmember:{definition.Key}:owner"),
                Community_Id = id,
                Employee_Id = definition.Owner.Id,
                Member_Role = CommunityMemberRole.Owner,
                Membership_Status = MembershipStatus.Approved,
                Requested_At = now,
                Joined_At = now,
                Is_Deleted = false,
                Created_By = definition.Owner.User_Id,
                Create_Date = now,
            });
        }

        var footballId = DeterministicGuid.From("dev:community:football");
        var guildId = DeterministicGuid.From("dev:community:design-guild");

        var joiners = new (string Key, Guid CommunityId, MembershipStatus Status)[]
        {
            ("yusuf", footballId, MembershipStatus.Approved),
            ("tariq", footballId, MembershipStatus.Approved),

            // The one that makes the approval queue non-empty on a fresh database.
            ("yusuf", guildId, MembershipStatus.Pending),
        };

        foreach (var (key, communityId, status) in joiners)
        {
            if (Person(key) is not { } person)
            {
                continue;
            }

            _context.Comm_Members.Add(new CommunityMember
            {
                Id = DeterministicGuid.From($"dev:commmember:{communityId}:{key}"),
                Community_Id = communityId,
                Employee_Id = person.Id,
                Member_Role = CommunityMemberRole.Member,
                Membership_Status = status,
                Requested_At = now,
                Joined_At = status == MembershipStatus.Approved ? now : null,
                Is_Deleted = false,
                Created_By = person.User_Id,
                Create_Date = now,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Counted from the rows just written rather than incremented as they were
        // added, so the seed cannot disagree with itself the way a hand-maintained
        // total eventually does.
        foreach (var community in await _context.Comm_Communities.ToListAsync(cancellationToken))
        {
            community.Members_Count = await _context.Comm_Members.CountAsync(
                member => member.Community_Id == community.Id
                    && member.Membership_Status == MembershipStatus.Approved,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} development communities", definitions.Length);
    }

    private async Task<Organization> SeedOrganizationAsync(CancellationToken cancellationToken)
    {
        var organization = new Organization
        {
            Id = DeterministicGuid.From("dev:org"),
            Name_Ar = "شركة بن غاطي",
            Name_En = "Binghatti",
            Legal_Name = "Binghatti Holding Ltd.",
            Description_En = "Sample organisation for local development.",
            Description_Ar = "منظمة تجريبية للتطوير المحلي.",
            Website = "https://example.invalid",
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SeedUsers.SystemUserId,
            Create_Date = DateTime.UtcNow,
        };

        _context.Org_Organizations.Add(organization);
        await _context.SaveChangesAsync(cancellationToken);

        return organization;
    }

    private async Task<List<Location>> SeedLocationsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var definitions = new (string Key, string Ar, string En, string City, string Zone)[]
        {
            ("dubai", "دبي", "Dubai HQ", "Dubai", "Asia/Dubai"),
            ("abudhabi", "أبوظبي", "Abu Dhabi Office", "Abu Dhabi", "Asia/Dubai"),
            ("london", "لندن", "London Office", "London", "Europe/London"),
        };

        var locations = definitions.Select(definition => new Location
        {
            Id = DeterministicGuid.From($"dev:loc:{definition.Key}"),
            Organization_Id = organizationId,
            Name_Ar = definition.Ar,
            Name_En = definition.En,
            Country = definition.Key == "london" ? "GB" : "AE",
            City = definition.City,
            TimeZone_Id = definition.Zone,
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SeedUsers.SystemUserId,
            Create_Date = DateTime.UtcNow,
        }).ToList();

        _context.Org_Locations.AddRange(locations);
        await _context.SaveChangesAsync(cancellationToken);

        return locations;
    }

    private async Task<List<JobTitle>> SeedJobTitlesAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var definitions = new (string Key, string Ar, string En, string Grade)[]
        {
            ("cto", "الرئيس التقني", "Chief Technology Officer", "E1"),
            ("eng-manager", "مدير هندسة", "Engineering Manager", "M2"),
            ("senior-engineer", "مهندس أول", "Senior Engineer", "P4"),
            ("engineer", "مهندس", "Engineer", "P3"),
            ("designer", "مصمم", "Product Designer", "P3"),
            ("hr-manager", "مدير الموارد البشرية", "HR Manager", "M2"),
            ("hr-officer", "أخصائي موارد بشرية", "HR Officer", "P2"),
            ("accountant", "محاسب", "Accountant", "P3"),
            ("sales-lead", "مدير مبيعات", "Sales Lead", "M1"),
        };

        var titles = definitions.Select(definition => new JobTitle
        {
            Id = DeterministicGuid.From($"dev:job:{definition.Key}"),
            Organization_Id = organizationId,
            Name_Ar = definition.Ar,
            Name_En = definition.En,
            Grade = definition.Grade,
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SeedUsers.SystemUserId,
            Create_Date = DateTime.UtcNow,
        }).ToList();

        _context.Org_JobTitles.AddRange(titles);
        await _context.SaveChangesAsync(cancellationToken);

        return titles;
    }

    /// <summary>
    /// Three levels deep on purpose, so the tree, the materialised path and the
    /// subtree filter all have something real to exercise.
    /// </summary>
    private async Task<List<Department>> SeedDepartmentsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var definitions = new (string Key, string? ParentKey, string Code, string Ar, string En)[]
        {
            ("tech", null, "TECH", "التقنية", "Technology"),
            ("eng", "tech", "ENG", "الهندسة", "Engineering"),
            ("platform", "eng", "ENG-PLT", "منصة", "Platform"),
            ("apps", "eng", "ENG-APP", "التطبيقات", "Applications"),
            ("design", "tech", "DSN", "التصميم", "Design"),
            ("corporate", null, "CORP", "الشؤون المؤسسية", "Corporate"),
            ("hr", "corporate", "HR", "الموارد البشرية", "Human Resources"),
            ("finance", "corporate", "FIN", "المالية", "Finance"),
            ("sales", null, "SLS", "المبيعات", "Sales"),
        };

        var byKey = new Dictionary<string, Department>();

        // In definition order, so a parent always exists before its children and the
        // path can be built from it in one pass.
        foreach (var definition in definitions)
        {
            var id = DeterministicGuid.From($"dev:dept:{definition.Key}");
            var parent = definition.ParentKey is null ? null : byKey[definition.ParentKey];

            var department = new Department
            {
                Id = id,
                Organization_Id = organizationId,
                Parent_Department_Id = parent?.Id,
                Code = definition.Code,
                Name_Ar = definition.Ar,
                Name_En = definition.En,
                Path = DepartmentPath.Build(parent?.Path, id),
                Sort_Order = 0,
                Is_Active = true,
                Is_Deleted = false,
                Created_By = SeedUsers.SystemUserId,
                Create_Date = DateTime.UtcNow,
            };

            department.Level = DepartmentPath.LevelOf(department.Path);

            byKey[definition.Key] = department;
        }

        _context.Org_Departments.AddRange(byKey.Values);
        await _context.SaveChangesAsync(cancellationToken);

        return [.. byKey.Values];
    }

    private async Task<List<Team>> SeedTeamsAsync(
        List<Department> departments,
        CancellationToken cancellationToken)
    {
        var byCode = departments.ToDictionary(department => department.Code);

        var definitions = new (string Key, string DepartmentCode, string Ar, string En)[]
        {
            ("core", "ENG-PLT", "الفريق الأساسي", "Core Platform"),
            ("mobile", "ENG-APP", "فريق الجوال", "Mobile"),
            ("web", "ENG-APP", "فريق الويب", "Web"),
            ("people", "HR", "فريق شؤون الموظفين", "People Operations"),
        };

        var teams = definitions.Select(definition => new Team
        {
            Id = DeterministicGuid.From($"dev:team:{definition.Key}"),
            Department_Id = byCode[definition.DepartmentCode].Id,
            Name_Ar = definition.Ar,
            Name_En = definition.En,
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SeedUsers.SystemUserId,
            Create_Date = DateTime.UtcNow,
        }).ToList();

        _context.Org_Teams.AddRange(teams);
        await _context.SaveChangesAsync(cancellationToken);

        return teams;
    }

    private async Task<List<Employee>> SeedEmployeesAsync(
        List<Department> departments,
        List<Team> teams,
        List<Location> locations,
        List<JobTitle> jobTitles,
        CancellationToken cancellationToken)
    {
        var departmentByCode = departments.ToDictionary(department => department.Code);
        var teamById = teams.ToDictionary(team => team.Id);
        var jobByKey = jobTitles.ToDictionary(
            job => job.Id,
            job => job);

        Guid Job(string key) => DeterministicGuid.From($"dev:job:{key}");
        Guid Team(string key) => DeterministicGuid.From($"dev:team:{key}");
        Guid Loc(string key) => DeterministicGuid.From($"dev:loc:{key}");

        var definitions = new (string Key, string First, string Last, string Ar, string Dept, Guid? Team, Guid Job, Guid Location, string Role)[]
        {
            ("nadia", "Nadia", "Al Rashid", "نادية الراشد", "TECH", null, Job("cto"), Loc("dubai"), Roles.Admin),
            ("omar", "Omar", "Haddad", "عمر حداد", "ENG", null, Job("eng-manager"), Loc("dubai"), Roles.DepartmentManager),
            ("layla", "Layla", "Mansour", "ليلى منصور", "ENG-PLT", Team("core"), Job("senior-engineer"), Loc("dubai"), Roles.Employee),
            ("yusuf", "Yusuf", "Karim", "يوسف كريم", "ENG-PLT", Team("core"), Job("engineer"), Loc("dubai"), Roles.Employee),
            ("hana", "Hana", "Suleiman", "هناء سليمان", "ENG-APP", Team("mobile"), Job("senior-engineer"), Loc("abudhabi"), Roles.Employee),
            ("tariq", "Tariq", "Nasser", "طارق ناصر", "ENG-APP", Team("web"), Job("engineer"), Loc("london"), Roles.Employee),
            ("amira", "Amira", "Fadel", "أميرة فاضل", "DSN", null, Job("designer"), Loc("dubai"), Roles.Employee),
            ("khalid", "Khalid", "Bakr", "خالد بكر", "HR", Team("people"), Job("hr-manager"), Loc("dubai"), Roles.Hr),
            ("sara", "Sara", "Idris", "سارة إدريس", "HR", Team("people"), Job("hr-officer"), Loc("dubai"), Roles.Hr),
            ("rami", "Rami", "Aziz", "رامي عزيز", "FIN", null, Job("accountant"), Loc("abudhabi"), Roles.Employee),
            ("dina", "Dina", "Farouk", "دينا فاروق", "SLS", null, Job("sales-lead"), Loc("london"), Roles.CommunicationsManager),
        };

        var employees = new List<Employee>();
        var number = 1000;

        foreach (var definition in definitions)
        {
            number++;

            var userId = DeterministicGuid.From($"dev:user:{definition.Key}");
            var userName = definition.Key;
            var email = $"{definition.Key}@example.invalid";

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                Full_Name_Ar = definition.Ar,
                Full_Name_En = $"{definition.First} {definition.Last}",
                User_Type = "MANAG",
                Is_Admin = false,
                Is_Active = true,
                Is_Deleted = false,
                Created_By = SeedUsers.SystemUserId,
                Create_Date = DateTime.UtcNow,
            };

            // Through UserManager rather than by inserting a row: it applies the
            // configured password policy and produces a hash in the format the sign-in
            // path expects.
            var created = await _userManager.CreateAsync(user, SeedPassword);

            if (!created.Succeeded)
            {
                _logger.LogError(
                    "Could not create the development user {UserName}: {Errors}",
                    userName,
                    string.Join("; ", created.Errors.Select(error => error.Description)));
                continue;
            }

            await _userManager.AddToRoleAsync(user, definition.Role);

            var employee = new Employee
            {
                Id = DeterministicGuid.From($"dev:emp:{definition.Key}"),
                User_Id = user.Id,
                Employee_Number = $"E-{number}",
                First_Name = definition.First,
                Last_Name = definition.Last,
                Display_Name = $"{definition.First} {definition.Last}",
                Full_Name_Ar = definition.Ar,
                Email = email,
                Mobile = $"+9715{number:0000000}",
                Department_Id = departmentByCode[definition.Dept].Id,
                Team_Id = definition.Team,
                Location_Id = definition.Location,
                Job_Title_Id = definition.Job,
                Biography_En = $"{definition.First} works in {departmentByCode[definition.Dept].Name_En}.",
                Joining_Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-number)),
                Birth_Date = new DateOnly(1990, (number % 12) + 1, (number % 27) + 1),
                Show_Birthday = true,
                Preferred_Language = definition.Key is "nadia" or "khalid" ? "ar" : "en",
                Status = EmployeeStatus.Active,
                Is_Active = true,
                Is_Deleted = false,
                Created_By = SeedUsers.SystemUserId,
                Create_Date = DateTime.UtcNow,
            };

            _context.Org_Employees.Add(employee);
            employees.Add(employee);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return employees;
    }

    private async Task SeedReportingLinesAsync(
        List<Employee> employees,
        List<Department> departments,
        CancellationToken cancellationToken)
    {
        var byKey = employees.ToDictionary(
            employee => employee.Employee_Number,
            employee => employee);

        Employee Find(string key) =>
            employees.First(employee => employee.Id == DeterministicGuid.From($"dev:emp:{key}"));

        var lines = new (string Employee, string Manager)[]
        {
            ("omar", "nadia"),
            ("amira", "nadia"),
            ("layla", "omar"),
            ("yusuf", "omar"),
            ("hana", "omar"),
            ("tariq", "omar"),
            ("sara", "khalid"),
        };

        foreach (var (employeeKey, managerKey) in lines)
        {
            _context.Org_EmployeeManagers.Add(new EmployeeManager
            {
                Id = DeterministicGuid.From($"dev:mgr:{employeeKey}"),
                Employee_Id = Find(employeeKey).Id,
                Manager_Id = Find(managerKey).Id,
                Is_Primary = true,
                Effective_From = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                Is_Deleted = false,
                Created_By = SeedUsers.SystemUserId,
                Create_Date = DateTime.UtcNow,
            });
        }

        // Department heads, so the org chart has managers on its nodes.
        var heads = new (string Code, string EmployeeKey)[]
        {
            ("TECH", "nadia"),
            ("ENG", "omar"),
            ("HR", "khalid"),
            ("SLS", "dina"),
        };

        foreach (var (code, employeeKey) in heads)
        {
            var department = departments.First(candidate => candidate.Code == code);
            department.Manager_Employee_Id = Find(employeeKey).Id;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFollowsAsync(List<Employee> employees, CancellationToken cancellationToken)
    {
        // Everyone follows the CTO, and the engineers follow each other - enough for the
        // follower counts and the "only people I follow" filter to show something.
        var cto = employees.First(employee => employee.Id == DeterministicGuid.From("dev:emp:nadia"));

        foreach (var employee in employees.Where(candidate => candidate.Id != cto.Id))
        {
            _context.Org_EmployeeFollowers.Add(new EmployeeFollower
            {
                Id = DeterministicGuid.From($"dev:follow:{employee.Id}:{cto.Id}"),
                Follower_Id = employee.Id,
                Followee_Id = cto.Id,
                Followed_At = DateTime.UtcNow,
                Is_Deleted = false,
            });

            employee.Following_Count++;
            cto.Followers_Count++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSkillsAsync(List<Employee> employees, CancellationToken cancellationToken)
    {
        var definitions = new (string Key, string Ar, string En, string Category)[]
        {
            ("dotnet", "دوت نت", ".NET", "Engineering"),
            ("angular", "أنغولار", "Angular", "Engineering"),
            ("sql", "قواعد البيانات", "SQL Server", "Engineering"),
            ("figma", "فيغما", "Figma", "Design"),
            ("recruiting", "التوظيف", "Recruiting", "People"),
        };

        var skills = definitions.Select(definition => new Skill
        {
            Id = DeterministicGuid.From($"dev:skill:{definition.Key}"),
            Name_Ar = definition.Ar,
            Name_En = definition.En,
            Category = definition.Category,
            Is_Approved = true,
            Is_Deleted = false,
            Created_By = SeedUsers.SystemUserId,
            Create_Date = DateTime.UtcNow,
        }).ToList();

        _context.Org_Skills.AddRange(skills);

        var assignments = new (string EmployeeKey, string[] SkillKeys)[]
        {
            ("layla", ["dotnet", "sql"]),
            ("yusuf", ["dotnet"]),
            ("hana", ["angular"]),
            ("tariq", ["angular", "dotnet"]),
            ("amira", ["figma"]),
            ("khalid", ["recruiting"]),
        };

        foreach (var (employeeKey, skillKeys) in assignments)
        {
            var employeeId = DeterministicGuid.From($"dev:emp:{employeeKey}");

            if (!employees.Any(employee => employee.Id == employeeId))
            {
                continue;
            }

            foreach (var skillKey in skillKeys)
            {
                _context.Org_EmployeeSkills.Add(new EmployeeSkill
                {
                    Id = DeterministicGuid.From($"dev:empskill:{employeeKey}:{skillKey}"),
                    Employee_Id = employeeId,
                    Skill_Id = DeterministicGuid.From($"dev:skill:{skillKey}"),
                    Endorsement_Count = skillKey.Length,
                    Added_At = DateTime.UtcNow,
                    Is_Deleted = false,
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    /// A handful of posts, deliberately targeted at different audiences.
    ///
    /// The point is not to have content on screen - it is that the audience resolver has
    /// something real to filter. One post for everybody, one for a division (so the
    /// ancestor-key rule is exercised by people several levels below it), one for a
    /// single office, and one for one person.
    /// </summary>
    private async Task SeedPostsAsync(
        List<Employee> employees,
        List<Department> departments,
        List<Location> locations,
        CancellationToken cancellationToken)
    {
        Employee Person(string key) =>
            employees.First(employee => employee.Id == DeterministicGuid.From($"dev:emp:{key}"));

        Guid Dept(string code) => departments.First(department => department.Code == code).Id;
        Guid Office(string key) => DeterministicGuid.From($"dev:loc:{key}");

        var now = DateTime.UtcNow;

        var definitions = new (string Key, string Author, PostType Type, bool Official, string Title, string Body,
            (AudienceType Type, Guid? Target)[] Audiences, int MinutesAgo)[]
        {
            ("welcome", "khalid", PostType.Announcement, true,
                "Welcome to Workvivo",
                "<p>This is our new internal home. Say hello, share what you are working on, and follow the people whose work you want to keep up with.</p>",
                [(AudienceType.AllEmployees, null)], 240),

            ("eng-allhands", "omar", PostType.Normal, false,
                "Engineering all-hands moved to Thursday",
                "<p>We have moved this month's all-hands to Thursday 10:00 so the London team can join without staying late.</p>",
                [(AudienceType.Department, Dept("TECH"))], 180),

            ("dubai-parking", "sara", PostType.Normal, false,
                "Parking level 2 closed on Sunday",
                "<p>Level 2 will be resurfaced on Sunday. Please use level 3 or the street entrance.</p>",
                [(AudienceType.Location, Office("dubai"))], 120),

            ("design-review", "amira", PostType.Normal, false,
                "New design review format",
                "<p>Starting next sprint we are running design reviews as async written critique, with a 30 minute live session only for the contested points.</p>",
                [(AudienceType.Department, Dept("TECH")), (AudienceType.Department, Dept("SLS"))], 90),

            ("nadia-note", "nadia", PostType.Celebration, false,
                "Congratulations to the platform team",
                "<p>The migration finished a week early and with no downtime. Thank you all.</p>",
                [(AudienceType.AllEmployees, null)], 45),
        };

        var posts = new List<Post>();

        foreach (var definition in definitions)
        {
            var author = Person(definition.Author);
            var publishedAt = now.AddMinutes(-definition.MinutesAgo);

            var post = new Post
            {
                Id = DeterministicGuid.From($"dev:post:{definition.Key}"),
                Author_Employee_Id = author.Id,
                Post_Type = definition.Type,
                Status = PostStatus.Published,
                Visibility = PostVisibility.Organization,
                Title_En = definition.Title,
                Title_Ar = definition.Title,
                Content_Html = definition.Body,
                Content_Text = definition.Body.Replace("<p>", string.Empty).Replace("</p>", string.Empty),
                Is_Official = definition.Official,
                Comments_Enabled = true,
                Published_Date = publishedAt,
                Is_Deleted = false,
                Created_By = author.User_Id,
                Create_Date = publishedAt,
            };

            foreach (var (audienceType, target) in definition.Audiences)
            {
                post.Audiences.Add(new PostAudience(post.Id, audienceType, target));
            }

            posts.Add(post);
        }

        // One post addressed to a single person, so the EMP: key is covered too.
        var directPost = new Post
        {
            Id = DeterministicGuid.From("dev:post:direct"),
            Author_Employee_Id = Person("khalid").Id,
            Post_Type = PostType.Normal,
            Status = PostStatus.Published,
            Visibility = PostVisibility.Organization,
            Title_En = "Your probation review",
            Title_Ar = "مراجعة فترة التجربة",
            Content_Html = "<p>Your six month review is scheduled for next week. Nothing to prepare.</p>",
            Content_Text = "Your six month review is scheduled for next week.",
            Comments_Enabled = false,
            Published_Date = now.AddMinutes(-30),
            Is_Deleted = false,
            Created_By = Person("khalid").User_Id,
            Create_Date = now.AddMinutes(-30),
        };
        directPost.Audiences.Add(new PostAudience(directPost.Id, AudienceType.Employee, Person("yusuf").Id));
        posts.Add(directPost);

        _context.Feed_Posts.AddRange(posts);
        await _context.SaveChangesAsync(cancellationToken);

        // A couple of reactions and a short thread, with the denormalised counters set
        // to match - they are maintained by the write paths in the running application,
        // so seeded rows have to keep them consistent by hand.
        var welcome = posts.First(post => post.Id == DeterministicGuid.From("dev:post:welcome"));

        var reactors = new[] { "omar", "layla", "hana", "amira" };

        foreach (var (key, index) in reactors.Select((key, index) => (key, index)))
        {
            _context.Feed_PostReactions.Add(new PostReaction
            {
                Id = DeterministicGuid.From($"dev:react:{key}"),
                Post_Id = welcome.Id,
                Employee_Id = Person(key).Id,
                Reaction_Type = (ReactionType)(index % 3),
                Reacted_At = now.AddMinutes(-200 + index),
                Is_Deleted = false,
            });
        }

        welcome.Reactions_Count = reactors.Length;

        var rootComment = new Comment
        {
            Id = DeterministicGuid.From("dev:comment:root"),
            Post_Id = welcome.Id,
            Author_Employee_Id = Person("layla").Id,
            Depth = 0,
            Content_Html = "<p>Good to have somewhere that is not email.</p>",
            Content_Text = "Good to have somewhere that is not email.",
            Replies_Count = 1,
            Is_Deleted = false,
            Created_By = Person("layla").User_Id,
            Create_Date = now.AddMinutes(-150),
        };

        var reply = new Comment
        {
            Id = DeterministicGuid.From("dev:comment:reply"),
            Post_Id = welcome.Id,
            Author_Employee_Id = Person("khalid").Id,
            Parent_Comment_Id = rootComment.Id,
            Depth = 1,
            Content_Html = "<p>That was the idea. Tell us what is missing.</p>",
            Content_Text = "That was the idea. Tell us what is missing.",
            Is_Deleted = false,
            Created_By = Person("khalid").User_Id,
            Create_Date = now.AddMinutes(-140),
        };

        _context.Feed_Comments.AddRange(rootComment, reply);
        welcome.Comments_Count = 2;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} development posts", posts.Count);
    }

}

/// <summary>Ids shared between the reference and development seeders.</summary>
internal static class SeedUsers
{
    public static readonly Guid SystemUserId = new("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90");
}
