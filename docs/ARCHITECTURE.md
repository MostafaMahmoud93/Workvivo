# Workvivo — Employee Experience & Internal Communication Platform
## Architecture & Delivery Plan

> Living document. Updated at the start and end of every delivery phase.
> Baseline: **.NET 10** / EF Core 10 / ASP.NET Core 10, Angular 21, Node 22.16, SQL Server.
> Verified on this machine (SDK 10.0.401): solution builds with 0 errors, 49 backend tests
> pass, Angular builds and its 11 tests pass.

---

## 0. Foundational decision — extend the existing template, do not restart

The brief proposed a greenfield layout (`src/EmployeeExperience.*`). This repository already
contains a **working, building .NET + Angular 21 Clean Architecture template** named
`Workvivo.*`, with an established house style: bilingual entities, a DB-driven permission
engine, a generic repository + unit of work, `ServiceResponse<T>` envelopes, and an EF
migration already written.

**Decision: build the platform on that template.** Renaming to `EmployeeExperience.*` would
discard the existing migration, the permission engine, the Angular shell/i18n/guard
scaffolding, and the team's conventions — for no functional gain. The layer boundaries the
brief asks for already exist under different names:

| Brief                               | This repository                                 |
| ----------------------------------- | ----------------------------------------------- |
| `EmployeeExperience.Domain`         | `Workvivo.Domain`                               |
| `EmployeeExperience.Application`    | `Workvivo.Application`                          |
| `EmployeeExperience.Infrastructure` | `Workvivo.Infrastructure`                       |
| `EmployeeExperience.API`            | `Workvivo.API`                                  |
| `EmployeeExperience.Web`            | `WorkvivoUI` (Angular 21)                       |
| —                                   | `Workvivo.Resources` (localised server strings) |
| `tests/*`                           | `tests/Workvivo.*.Tests`                        |

This is cheap to reverse now and expensive later — say so before Phase 2 if the
`EmployeeExperience.*` naming is a hard requirement.

### Deviations from the brief, and why

| Brief asked                 | Delivered                                                                                           | Reason |
| --------------------------- | --------------------------------------------------------------------------------------------------- | ------ |
| CQRS                        | MediatR CQRS for **new feature modules**; the 8 existing security/admin services stay service-based   | Rewriting working security code to change pattern is churn, not value. New modules get the pipeline (validation / logging / transaction / performance) benefits. |
| Angular Material            | The template's existing CSS-token design system, extended                                             | Material's default look is exactly the "generic admin dashboard" the brief rules out. The tokens are already defined, RTL-aware, and cost 8 kB. |
| `ProblemDetails` everywhere | `ApiResponse<T>` on success, RFC 7807 `ProblemDetails` on failure — both carrying `traceId`            | The Angular client already consumes `{success, message, data}`. `ApiResponse<T>` is a superset, so no client churn. |
| FluentAssertions            | **Shouldly**                                                                                          | FluentAssertions 8.x moved to a paid commercial licence. Shouldly is BSD-3 and reads as well. |
| ngx-translate               | The template's typed `messages.ts` catalogue, split per feature                                       | `AR` is typed against `EN`, so a missing Arabic string is a **compile error**, not an English word on an Arabic screen. Strictly better than runtime JSON lookup. |
| .NET 9                      | **.NET 10** (LTS)                                                                                     | .NET 9 is an STS release whose support window has closed. Staying on it would ship an enterprise platform on an unpatched runtime. |
| Hangfire                    | Hangfire on **SQL Server** storage                                                                    | No extra infrastructure — the database is already there. Redis stays optional. |

### Pre-existing issues found during analysis

Each is tracked to the phase that fixes it.

1. **Committed SMTP credential.** `Workvivo.API/appsettings.json` holds a live mailbox password
   in plain text. **That credential should be rotated** — it is in the working tree regardless of
   what we do next. Moved to user-secrets / environment in Phase 1.
2. **Hard-coded JWT signing key**, duplicated in `ConfigureAuthuntication.cs` and
   `AuthService._publicKey`. Moved to configuration in Phase 1, distinct per environment.
3. **`SaveChangesAsync` issues an extra query on every save** (`ApplicationUsers.FirstOrDefaultAsync`)
   only as an audit-user fallback. Made lazy in Phase 1.
4. **`UseLazyLoadingProxies()` is enabled globally** — an N+1 generator that directly conflicts with
   the performance requirements. Existing services depend on it, so it stays for now; **every new
   read path projects to a DTO and never materialises a proxy**. Removal is a Phase 16 task.
5. **`ActionFilter` calls `.Result` on an async call** (sync-over-async — deadlock and
   thread-starvation risk). Replaced by a proper `IAuthorizationHandler` in Phase 4.
6. **`AllowFiltered` is a hard-coded allow-list of controller and action names.** Replaced by
   `[AllowAnonymous]` / `[HasPermission]` attributes in Phase 4.
7. **433 compiler warnings**, mostly nullability. New projects opt into warnings-as-errors;
   existing projects are cleaned incrementally rather than in one risky sweep.

---

## 1. High-level architecture

```
                       ┌───────────────────────────────────────────────┐
   Browser / mobile    │  WorkvivoUI — Angular 21, standalone, signals │
   (desktop, tablet,   │  lazy routes · guards · interceptors · WS     │
    phone; LTR + RTL)  └───────────────┬───────────────────────────────┘
                                       │ HTTPS (REST + SignalR)
                       ┌───────────────▼───────────────────────────────┐
                       │  Workvivo.API — thin host                     │
                       │  controllers · hubs · Hangfire dashboard      │
                       │  middleware: correlation · exceptions ·       │
                       │  security headers · rate limit · localisation │
                       └───────────────┬───────────────────────────────┘
                                       │ MediatR (ISender)
                       ┌───────────────▼───────────────────────────────┐
                       │  Workvivo.Application — use cases             │
                       │  commands · queries · validators · policies   │
                       │  pipeline: validation → logging → txn → perf  │
                       │  mapping profiles · domain-event handlers     │
                       └───────────────┬───────────────────────────────┘
                                       │ abstractions only
        ┌──────────────────────────────┼───────────────────────────────┐
        │                              │                               │
┌───────▼──────────┐      ┌────────────▼────────────┐      ┌───────────▼─────────┐
│ Workvivo.Domain  │      │ Workvivo.Infrastructure │      │ Workvivo.Resources  │
│ entities · enums │      │ EF Core · repositories  │      │ ar/en server strings│
│ invariants       │      │ storage · cache · mail  │      └─────────────────────┘
│ domain events    │      │ jobs · realtime · audit │
└──────────────────┘      └────────────┬────────────┘
                                       │
              ┌────────────────┬───────┴────────┬──────────────────┐
        ┌─────▼─────┐   ┌──────▼──────┐  ┌──────▼──────┐  ┌────────▼───────┐
        │ SQL Server│   │ Redis (opt) │  │ Blob / S3   │  │ SMTP / Graph   │
        │ + full-text│  │ cache + WS  │  │ or local FS │  │ (abstracted)   │
        │ + Hangfire│   │ backplane   │  │             │  │                │
        └───────────┘   └─────────────┘  └─────────────┘  └────────────────┘
```

**Dependency rule.** `Domain` depends on nothing. `Application` depends on `Domain` and on
abstractions only. `Infrastructure` implements those abstractions. `API` composes them.

> The template's reference graph is `Application → Infrastructure` (not the textbook
> `Infrastructure → Application`). That is left in place. The discipline that actually matters
> is enforced by rule instead — **Application code references `IUnitOfWork`,
> `IFileStorageService`, `ICacheService`… never a concrete Infrastructure type** — and is
> verified by an architecture test in `Workvivo.Application.Tests` (Phase 16).

---

## 2. Solution and project structure

```
Workvivo.sln
├── Core/
│   ├── Workvivo.Domain
│   │   ├── Abstractions/   Classes · Enums · Interfaces
│   │   ├── Exceptions/     AppException hierarchy (see note below)
│   │   ├── Entities/       BaseEntities · Identity · Common · Org · Feed ·
│   │   │                   Communities · Recognition · Polls · Surveys ·
│   │   │                   Events · Documents · RealTime · Audit · Views
│   │   ├── Events/         domain events (records)
│   │   └── Models/         request / response contracts
│   ├── Workvivo.Application
│   │   ├── Abstractions/   cross-cutting contracts (storage, cache, clock, …)
│   │   ├── Bases/          ApiResponse · ServiceResponse · ServiceBase · RouteClass
│   │   ├── Common/Paging/  PagedResult · CursorPagedResult · Cursor · PageRequest
│   │   ├── Common/Messaging/ ICommand · IQuery · ICacheableQuery
│   │   ├── Behaviors/      MediatR pipeline behaviours
│   │   ├── Common/         guards · HTML sanitisation · audience resolution
│   │   ├── Features/       one folder per module — CQRS lives here
│   │   │   └── <Module>/{Commands,Queries,Validators,Dtos,Mappings,EventHandlers}
│   │   ├── Implementation/ existing security & admin services (unchanged)
│   │   └── Interfaces/
│   └── Workvivo.Resources  ClutureResource.resx / .ar.resx
├── Infrastructure/
│   └── Workvivo.Infrastructure
│       ├── Configuration/  one IEntityTypeConfiguration per entity
│       ├── DBContext/      Workvivo_DbContext + interceptors
│       ├── Migrations/
│       ├── Repositories/   BaseRepository · CustomBaseRepository · UnitOfWork
│       ├── Storage/        Local · AzureBlob · S3 providers + validation
│       ├── Caching/        Memory · Redis
│       ├── Messaging/      email senders, templates
│       ├── RealTime/       SignalR notifier
│       ├── Jobs/           Hangfire jobs + recurring schedule
│       ├── Identity/       token service, refresh-token store, password policy
│       ├── Auditing/       change interceptor + audit writer
│       └── Seeding/        idempotent runtime seeders
├── Presentation/
│   ├── Workvivo.API        controllers · hubs · middleware · extensions
│   └── WorkvivoUI          Angular 21
└── tests/
    ├── Workvivo.Domain.Tests          invariants and value rules
    ├── Workvivo.Application.Tests     handlers, validators, policies, arch tests
    ├── Workvivo.API.Tests             controllers, filters, auth handlers
    └── Workvivo.IntegrationTests      WebApplicationFactory + Testcontainers (SQL Server)
```

### Conventions inherited from the template and kept throughout

- Entity properties use `Underscore_Pascal` (`Is_Deleted`, `Create_Date`, `Full_Name_Ar`).
- Every user-facing text column is a **pair** — `*_Ar` / `*_En` — plus a `[NotMapped]`
  culture-aware accessor returning the right one for `CurrentCulture`.
- Table names are prefixed by area: existing `Security_`, `Common_`; new `Org_`, `Feed_`,
  `Comm_`, `Rec_`, `Poll_`, `Srv_`, `Evt_`, `Doc_`, `Notif_`, `Audit_`.
- Audited entities derive from `FullBaseEntity<TKey>` (`Created_By`, `Create_Date`,
  `Last_Modified_By`, `Last_Modify_Date`, `Is_Deleted`).
- Routes are constants in `RouteClass`, never inline in a controller.
- Each `IEntityTypeConfiguration<T>` lives in `Infrastructure/Configuration`; `OnModelCreating`
  only calls `ApplyConfigurationsFromAssembly`.
- **The exception hierarchy lives in `Workvivo.Domain/Exceptions`, not in Application.** With the
  template's `Application → Infrastructure` reference direction, Infrastructure cannot see
  Application, and the storage, caching and identity code all need to throw these. Domain is the
  only project every layer can reference.

---

## 3. Database entity diagram (text form)

`PK` primary key · `FK` foreign key · `UQ` unique · `IX` indexed · `†` soft-deletable
(`Is_Deleted`) · `‡` audited (`FullBaseEntity`) · `®` `RowVersion` concurrency token.

### 3.1 Security and identity

```
Security_Users (ApplicationUser : IdentityUser<Guid>)  †
  PK Id (Guid)
  UQ NormalizedUserName, UQ NormalizedEmail
     Full_Name_Ar, Full_Name_En, MobileNo, User_Type FK→Security_MasterData.Master_Data_Code
     Is_Admin, Is_Active, Profile_PictureURL, ...
  1───1  Org_Employees                 (the HR-facing profile hangs off the login)
  1───n  Security_UserGroupsLink       (roles)
  1───n  Security_UserPermissions      (per-user permission overrides)
  1───n  Security_RefreshTokens

Security_UserGroups (UserGroup : IdentityRole<Guid>)  †
  PK Id · Name_Ar · Name_En · User_Type · Is_Active
  1───n  Security_UserGroupsLink
  1───n  Security_GroupPermissions

Security_UserGroupsLink            PK (User_Id, Group_Id)
Common_MainModules  1───n  Common_Screens  1───n  Common_LinkScreenActions  n───1  Common_ScreenActions
Common_LinkScreenActions           PK Id · Screen_Id FK · Screen_Action_Id FK
                                   Base_Route · Action_Code
                                   UQ Permission_Key    ← added: "Post.Create", "Analytics.View"
                                   Module · Description_Ar · Description_En
Security_GroupPermissions          PK Id · Group_Id FK · Link_Screen_Action_Id FK   UQ(Group_Id, Link_Screen_Action_Id)
Security_UserPermissions           PK Id · User_Id  FK · Link_Screen_Action_Id FK · Is_Granted (allow/deny override)
VW_UserActions (view)              flattens user → group → permission into (User_Id, Permission_Key, Base_Route, Action_Code)

Security_RefreshTokens  ‡
  PK Id · IX User_Id FK→Security_Users
  UQ Token_Hash (SHA-256, never the raw token)
  IX Family_Id · Expires_At · Revoked_At · Replaced_By_Id FK→self
     Created_Ip · Created_User_Agent · Revoked_Reason
```

### 3.2 Organisation

```
Org_Organizations ‡†      PK Id · Name_Ar/En · Legal_Name · Logo_File_Id · Website · Description_Ar/En
  │
  ├─1──n Org_Departments ‡†   PK Id · Organization_Id FK · Parent_Department_Id FK→self (hierarchy)
  │        Name_Ar/En · Code UQ · Manager_Employee_Id FK→Org_Employees · Path (materialised, IX)
  │        └─1──n Org_Teams ‡†   PK Id · Department_Id FK · Name_Ar/En · Lead_Employee_Id FK
  ├─1──n Org_Locations ‡†     PK Id · Organization_Id FK · Name_Ar/En · Country · City · Address · TimeZone_Id
  └─1──n Org_JobTitles  ‡†    PK Id · Organization_Id FK · Name_Ar/En · Grade

Org_Employees ‡† ®
  PK Id (Guid)
  UQ User_Id FK→Security_Users          (1:1 — login ⇄ profile)
  UQ Employee_Number
  IX Department_Id FK · Team_Id FK · Location_Id FK · Job_Title_Id FK
     First_Name · Middle_Name · Last_Name · Display_Name · Full_Name_Ar
     Email UQ · Mobile · Profile_Picture_File_Id FK→Doc_Files · Cover_Picture_File_Id FK
     Biography_Ar/En · Joining_Date · Birth_Date · Preferred_Language · Status · Is_Active
  ├─1──n Org_EmployeeManagers   PK Id · Employee_Id FK · Manager_Id FK→Org_Employees
  │                             Is_Primary · Effective_From · Effective_To
  │                             UQ(Employee_Id, Manager_Id, Effective_From)
  ├─n──n Org_Skills      via Org_EmployeeSkills    (Employee_Id, Skill_Id, Endorsement_Count)
  ├─n──n Org_Interests   via Org_EmployeeInterests (Employee_Id, Interest_Id)
  └─n──n Org_Employees   via Org_EmployeeFollowers (Follower_Id, Followee_Id, Followed_At)
                                                    PK(Follower_Id, Followee_Id)
```

### 3.3 Feed

```
Feed_Posts ‡† ®
  PK Id (Guid)
  IX Author_Employee_Id FK→Org_Employees
  IX Community_Id FK→Comm_Communities (nullable — null = company feed)
     Post_Type (enum) · Content_Text · Content_Html (sanitised) · Title_Ar/En
     Visibility (enum) · Status (enum: Draft|Scheduled|Published|Archived)
     Is_Pinned · Is_Featured · Is_Official · Comments_Enabled
     Published_Date · Scheduled_Publish_Date
     Reactions_Count · Comments_Count · Views_Count      ← denormalised counters
  IX_Feed_Posts_Timeline (Status, Is_Deleted, Published_Date DESC, Id DESC)
  IX_Feed_Posts_Pinned   (Status, Is_Pinned, Published_Date DESC) WHERE Is_Pinned = 1
  │
  ├─1──n Feed_PostAttachments  PK Id · Post_Id FK · File_Id FK→Doc_Files
  │                            Attachment_Type (Image|Video|Document|Link)
  │                            Link_Url · Link_Title · Link_Image_Url · Sort_Order
  ├─1──n Feed_PostAudiences    PK Id · Post_Id FK
  │                            Audience_Type (enum) · Target_Id (Guid, nullable)
  │                            Audience_Key AS computed persisted  e.g. 'DEPT:{guid}', 'ALL'
  │                            UQ(Post_Id, Audience_Key) · IX(Audience_Key) INCLUDE(Post_Id)
  ├─1──n Feed_PostReactions    PK Id · Post_Id FK · Employee_Id FK · Reaction_Type (enum)
  │                            UQ(Post_Id, Employee_Id)     ← one reaction per person per post
  │                            IX(Post_Id, Reaction_Type)
  ├─1──n Feed_PostMentions     PK Id · Post_Id FK · Mentioned_Employee_Id FK · Offset · Length
  │                            UQ(Post_Id, Mentioned_Employee_Id)
  ├─1──n Feed_PostViews        PK Id · Post_Id FK · Employee_Id FK · Viewed_At
  │                            UQ(Post_Id, Employee_Id)     ← "announcement views" analytics
  └─1──n Feed_Comments ‡†
           PK Id · Post_Id FK · Author_Employee_Id FK
           Parent_Comment_Id FK→self (nullable) · Depth (0|1|2, CHECK ≤ 2)
           Content_Text · Content_Html · Reactions_Count · Replies_Count
           IX(Post_Id, Parent_Comment_Id, Create_Date)
           ├─1──n Feed_CommentReactions  UQ(Comment_Id, Employee_Id)
           └─1──n Feed_CommentMentions   UQ(Comment_Id, Mentioned_Employee_Id)
```

### 3.4 Communities

```
Comm_Communities ‡† ®
  PK Id · UQ Slug · Name_Ar/En · Description_Ar/En
  Logo_File_Id FK · Cover_File_Id FK · Privacy (Public|Private|Restricted)
  Owner_Employee_Id FK→Org_Employees · Members_Count · Posts_Count · Is_Active
  ├─1──n Comm_Members      PK Id · Community_Id FK · Employee_Id FK
  │                        Member_Role (Member|Moderator|Owner)
  │                        Membership_Status (Pending|Approved|Rejected|Banned)
  │                        Joined_At · Approved_By FK
  │                        UQ(Community_Id, Employee_Id) · IX(Employee_Id, Membership_Status)
  └─1──n Comm_Invitations  PK Id · Community_Id FK · Invited_Employee_Id FK · Invited_By FK
                           Status · Expires_At · UQ(Community_Id, Invited_Employee_Id)
```
> `Comm_Moderators` from the brief is folded into `Comm_Members.Member_Role`. A separate table
> would allow a moderator who is not a member — a state with no meaning here.

### 3.5 Recognition

```
Rec_RecognitionTypes ‡†   PK Id · Code UQ · Name_Ar/En · Description_Ar/En
                          Badge_Icon · Badge_Color · Default_Points · Is_Active · Sort_Order
Rec_Recognitions ‡†
  PK Id · IX Sender_Employee_Id FK · IX Recipient_Employee_Id FK
  Recognition_Type_Id FK · Message · Points · Visibility (enum)
  Post_Id FK→Feed_Posts (nullable — a public recognition also appears in the feed)
  Recognised_On  IX(Recipient_Employee_Id, Recognised_On DESC)
  CHECK Sender_Employee_Id <> Recipient_Employee_Id
Rec_LeaderboardSnapshots  PK Id · Employee_Id FK · Period (Month|Year) · Period_Start
                          Points · Recognition_Count · Rank
                          UQ(Employee_Id, Period, Period_Start)   ← rebuilt nightly by Hangfire
```

### 3.6 Polls and surveys

```
Poll_Polls ‡†     PK Id · Post_Id FK→Feed_Posts (nullable) · Question_Ar/En
                  Is_Multiple_Choice · Is_Anonymous · Start_Date · Expiry_Date
                  Total_Votes · Status
  ├─1──n Poll_Options  PK Id · Poll_Id FK · Text_Ar/En · Sort_Order · Votes_Count
  ├─1──n Poll_Votes    PK Id · Poll_Id FK · Option_Id FK · Employee_Id FK (nullable if anonymous)
  │                    Voter_Hash (for anonymous duplicate prevention) · Voted_At
  │                    UQ(Poll_Id, Option_Id, Employee_Id)
  └─1──n Poll_Audiences   same shape as Feed_PostAudiences

Srv_Surveys ‡†    PK Id · Title_Ar/En · Description_Ar/En · Status (Draft|Scheduled|Published|Closed)
                  Is_Anonymous · Start_Date · End_Date · Allow_Multiple_Responses · Response_Count
  ├─1──n Srv_Questions      PK Id · Survey_Id FK · Question_Type (enum) · Text_Ar/En
  │                         Is_Required · Sort_Order · Min_Value · Max_Value (rating / scale / NPS)
  │        └─1──n Srv_QuestionOptions  PK Id · Question_Id FK · Text_Ar/En · Sort_Order
  ├─1──n Srv_Responses      PK Id · Survey_Id FK · Employee_Id FK (null if anonymous)
  │                         Submitted_At · Is_Complete
  │                         UQ(Survey_Id, Employee_Id) WHERE Allow_Multiple_Responses = 0
  │        └─1──n Srv_Answers  PK Id · Response_Id FK · Question_Id FK
  │                            Option_Id FK (nullable) · Text_Value · Numeric_Value
  └─1──n Srv_Audiences      same shape as Feed_PostAudiences
```

### 3.7 Events

```
Evt_Events ‡† ®
  PK Id · Title_Ar/En · Description_Ar/En
  Event_Type (Company|Team|Online|Physical) · Start_At · End_At · TimeZone_Id
  Location_Id FK→Org_Locations (nullable) · Address · Meeting_Url
  Banner_File_Id FK · Organizer_Employee_Id FK · Community_Id FK (nullable)
  Capacity · Attendees_Count · Status · Reminder_Sent_At
  IX(Start_At, Status)
  ├─1──n Evt_Attendees   PK Id · Event_Id FK · Employee_Id FK
  │                      Response (Attending|Maybe|Declined) · Responded_At · Checked_In_At
  │                      UQ(Event_Id, Employee_Id)
  └─1──n Evt_Audiences   same shape as Feed_PostAudiences
```

### 3.8 Documents and files

```
Doc_Files  ‡                        ← the single physical-file registry, shared by every module
  PK Id · Storage_Provider ('Local'|'AzureBlob'|'S3') · Storage_Key (opaque path/blob name)
  Original_File_Name · Content_Type (sniffed, not trusted) · Extension · Size_Bytes
  Checksum_Sha256 · Uploaded_By FK · Uploaded_At · Scan_Status
  IX(Checksum_Sha256)    ← de-duplication

Doc_Categories ‡†   PK Id · Parent_Category_Id FK→self · Name_Ar/En · Icon · Sort_Order
Doc_Documents  ‡† ® PK Id · Category_Id FK · Title_Ar/En · Description_Ar/En
                    Current_Version_Id FK→Doc_Versions · Owner_Employee_Id FK
                    Download_Count · Is_Published · Published_At
  ├─1──n Doc_Versions   PK Id · Document_Id FK · File_Id FK→Doc_Files
  │                     Version_Number · Change_Note · Created_By · Create_Date
  │                     UQ(Document_Id, Version_Number)
  ├─1──n Doc_Audiences  same shape as Feed_PostAudiences  ← who may read it
  └─1──n Doc_DownloadLogs  PK Id · Document_Id FK · Version_Id FK · Employee_Id FK
                           Downloaded_At · Ip_Address · User_Agent   IX(Document_Id, Downloaded_At)
```
> File **binaries never go in SQL Server** — only metadata rows in `Doc_Files`. The bytes live
> behind `IFileStorageService`.

### 3.9 Notifications

```
Notifications ‡  (existing)   PK Id · Header_Ar/En · Content_Ar/En · RedirectUrl
                              Notification_Type · Notification_Status
NotificationUsers (existing)  PK Id · Notification_Id FK · Reciever_Id FK · IS_Seen
                              IX(Reciever_Id, IS_Seen, Id)   ← added: drives the unread badge
Notif_Preferences ‡           PK Id · Employee_Id FK · Notification_Type (enum)
                              In_App_Enabled · Email_Enabled · Push_Enabled (future)
                              UQ(Employee_Id, Notification_Type)
```

### 3.10 Auditing and settings

```
Audit_Logs                    PK Id (bigint identity)
  IX User_Id · Username · Action (enum: Login|Logout|Create|Update|Delete|Publish|
                                  PermissionChange|Activate|Deactivate|Download|AdminAction)
  Entity_Name · Entity_Id · Old_Values (nvarchar(max) JSON) · New_Values (JSON)
  Affected_Columns · Ip_Address · User_Agent · Correlation_Id · Timestamp
  IX(Entity_Name, Entity_Id) · IX(Timestamp DESC) · IX(User_Id, Timestamp DESC)
  → partitioned / archived monthly by a Hangfire job

Security_AccessLogs (existing) — screen-access log, kept as-is, distinct from Audit_Logs
SysSetting (existing) — key/value application settings, extended for feature flags
Common_Lookups / Security_MasterData (existing) — bilingual enumerations
```

### 3.11 Audience targeting — the shared shape

Every targetable module (`Feed_PostAudiences`, `Evt_Audiences`, `Srv_Audiences`,
`Poll_Audiences`, `Doc_Audiences`) uses the **same column shape** with **real foreign keys per
table**, rather than one polymorphic table:

```
PK Id · <Owner>_Id FK   Audience_Type   Target_Id   Audience_Key (computed, persisted)
                        ─────────────   ─────────   ──────────────────────────────────
                        AllEmployees    NULL        'ALL'
                        Department      {guid}      'DEPT:{guid}'
                        Team            {guid}      'TEAM:{guid}'
                        Location        {guid}      'LOC:{guid}'
                        JobTitle        {guid}      'JOB:{guid}'
                        Role            {guid}      'ROLE:{guid}'
                        Community       {guid}      'COMM:{guid}'
                        Employee        {guid}      'EMP:{guid}'
```

Referential integrity is preserved (a real FK on each owner column), and the duplication is
only in schema, not in logic: **one** `IAudienceResolver` builds the key set and **one** generic
predicate is reused by every module. See §9.

---

## 4. Main tables and relationships (summary)

| Area          | Tables | Key relationships |
| ------------- | ------ | ----------------- |
| Security      | `Security_Users`, `Security_UserGroups`, `Security_UserGroupsLink`, `Security_GroupPermissions`, `Security_UserPermissions`, `Security_RefreshTokens`, `Common_MainModules`, `Common_Screens`, `Common_ScreenActions`, `Common_LinkScreenActions` | user *n–n* group; group *n–n* permission; user *1–n* refresh token |
| Organisation  | `Org_Organizations`, `Org_Departments`, `Org_Teams`, `Org_Locations`, `Org_JobTitles`, `Org_Employees`, `Org_EmployeeManagers`, `Org_Skills`, `Org_EmployeeSkills`, `Org_Interests`, `Org_EmployeeInterests`, `Org_EmployeeFollowers` | employee *1–1* user; department self-hierarchy; employee *n–n* manager (dated); employee *n–n* employee (follow) |
| Feed          | `Feed_Posts`, `Feed_PostAttachments`, `Feed_PostAudiences`, `Feed_PostReactions`, `Feed_PostMentions`, `Feed_PostViews`, `Feed_Comments`, `Feed_CommentReactions`, `Feed_CommentMentions` | post *1–n* everything; comment self-hierarchy capped at depth 2 |
| Communities   | `Comm_Communities`, `Comm_Members`, `Comm_Invitations` | community *1–n* member; post *n–1* community |
| Recognition   | `Rec_RecognitionTypes`, `Rec_Recognitions`, `Rec_LeaderboardSnapshots` | recognition *n–1* sender/recipient/type, optional *1–1* post |
| Polls         | `Poll_Polls`, `Poll_Options`, `Poll_Votes`, `Poll_Audiences` | poll *1–n* option *1–n* vote |
| Surveys       | `Srv_Surveys`, `Srv_Questions`, `Srv_QuestionOptions`, `Srv_Responses`, `Srv_Answers`, `Srv_Audiences` | survey *1–n* question *1–n* option; response *1–n* answer |
| Events        | `Evt_Events`, `Evt_Attendees`, `Evt_Audiences` | event *1–n* attendee |
| Documents     | `Doc_Files`, `Doc_Categories`, `Doc_Documents`, `Doc_Versions`, `Doc_Audiences`, `Doc_DownloadLogs` | document *1–n* version *n–1* file; category self-hierarchy |
| Notifications | `Notifications`, `NotificationUsers`, `Notif_Preferences` | notification *1–n* recipient |
| Platform      | `Audit_Logs`, `Security_AccessLogs`, `UserLoginLogs`, `SysSetting`, `Security_MasterData`, `Lookups`, `GlobalAttachments`, `EmailSMSTemplates`, `EmailSMSHistory` | — |

**Concurrency.** `RowVersion` on entities that two people realistically edit at once: `Org_Employees`,
`Feed_Posts`, `Comm_Communities`, `Doc_Documents`, `Evt_Events`.
Counter columns are **not** guarded by `RowVersion` — they are updated with atomic
`ExecuteUpdateAsync` increments so concurrent reactions never collide.

**Soft delete.** `Is_Deleted` + a global query filter on every `†` entity. Join tables
(reactions, votes, attendees, followers) are hard-deleted — a retracted "like" carries no history worth keeping.

---

## 5. Backend module structure

Each feature module is a folder under `Workvivo.Application/Features/`:

```
Features/Posts/
├── Commands/
│   ├── CreatePost/            CreatePostCommand · Handler · Validator
│   ├── UpdatePost/  PublishPost/  SchedulePost/  PinPost/  ArchivePost/  DeletePost/
├── Queries/
│   ├── GetFeed/               GetFeedQuery · Handler (projection, keyset paging)
│   ├── GetPostById/  GetPostReactions/  GetDrafts/
├── Dtos/                      FeedItemDto · PostDetailDto · ReactionSummaryDto
├── Mappings/                  PostProfile : MappingProfileBase
└── EventHandlers/             PostPublishedHandler → notifications, audience fan-out
```

Modules: `Auth`, `Employees`, `Organization`, `Posts`, `Comments`, `Reactions`, `Mentions`,
`Communities`, `Recognition`, `Polls`, `Surveys`, `Events`, `Documents`, `Notifications`,
`Search`, `Analytics`, `Admin`, `Files`.

**MediatR pipeline** (order matters):

```
Request
  → RequestLoggingBehavior      correlation id, user, elapsed ms (Serilog)
  → ValidationBehavior          FluentValidation; throws ValidationException → 400 ProblemDetails
  → AuthorizationBehavior       [RequiresPermission] on the request type
  → TransactionBehavior         commands only — one transaction per command
  → CachingBehavior             queries marked ICacheableQuery
  → PerformanceBehavior         warns above a configured threshold
  → Handler
  → DomainEventDispatchBehavior after a successful SaveChanges, never inside the transaction
```

Controllers stay thin — bind, `ISender.Send`, wrap in `ApiResponse<T>`:

```csharp
[HttpPost, Route(RouteClass.Posts.Create), HasPermission(Permissions.Post.Create)]
public async Task<IActionResult> Create(CreatePostCommand command, CancellationToken ct)
    => Ok(ApiResponse<Guid>.Ok(await sender.Send(command, ct)));
```

---

## 6. Angular feature structure

```
src/app/
├── core/
│   ├── auth/          auth.service · token.store · permission.service · sso-ready hooks
│   ├── guards/        auth · guest · permission · role · unsaved-changes
│   ├── interceptors/  auth · refresh-token (401 retry queue) · error · correlation-id · loading
│   ├── services/      api.client · locale · theme · realtime(SignalR) · toast · confirm ·
│   │                  breakpoint · upload · title
│   ├── models/        strongly typed contracts mirroring the API
│   └── i18n/          messages/*.ts — one typed catalogue per feature
├── shared/
│   ├── components/    avatar · user-chip · card · skeleton · empty-state · error-state ·
│   │                  paginator · confirm-dialog · toast-host · file-drop · rich-editor ·
│   │                  mention-textarea · reaction-bar · chart · date-range · icon · brand-mark
│   ├── directives/    hasPermission · autofocus · infiniteScroll · clickOutside · dir-aware
│   └── pipes/         localizedDate · relativeTime · localizedName · fileSize · highlight · sanitizeHtml
├── layout/
│   ├── shell/         sidebar + header + mobile nav (responsive)
│   ├── header/        global search · notification bell · profile menu · language toggle
│   └── sidebar/       navigation built from the user's permitted screens
└── features/          (all lazy-loaded standalone routes)
    auth · home · feed · employees · profile · communities · recognition · polls ·
    surveys · events · documents · notifications · search ·
    admin/{dashboard,employees,departments,teams,locations,roles,permissions,
           moderation,communities,recognition,surveys,events,documents,audit,settings}
    analytics · settings
```

**State.** Signals by default — `signal`, `computed`, `linkedSignal`, `resource()` for
server data, `toSignal` at RxJS boundaries. **NgRx is not adopted**; no requirement here
involves multi-screen shared mutable state complex enough to earn its ceremony. The two
genuinely global pieces of state — session and unread-notification count — are signals in
root-provided services, which is simpler and fully reactive. Revisit only if a concrete
feature demands time-travel debugging or cross-feature transactional state.

**Feed list.** Virtual scroll + keyset paging + `@defer` for below-the-fold cards, so a
10 000-post feed never materialises more than a screenful of DOM.

---

## 7. Authentication architecture

```
  Angular                          API                              Store
  ───────                          ───                              ─────
  POST /api/auth/login  ─────────► AuthService
   {userName, password}            ├ lockout check (configurable N failures / window)
                                   ├ PasswordSignInAsync (Identity PBKDF2-HMAC-SHA512)
                                   ├ Is_Active / Email_Confirmed check
                                   ├ issue access JWT   (15 min, in-memory only)
                                   └ issue refresh token (14 d, random 256-bit)
                                        · SHA-256 hash stored, raw value never persisted
                                        · Set-Cookie: __Host-wv_rt;
                                          HttpOnly; Secure; SameSite=Strict; Path=/api/auth
  ◄──────── {accessToken, expiresAt, permissions[], profile}

  access token expires
  POST /api/auth/refresh ────────► rotate:
   (cookie sent automatically)     ├ hash → look up → validate not expired / not revoked
                                   ├ REUSE DETECTED (token already revoked)?
                                   │    → revoke the whole Family_Id, force re-login, audit
                                   ├ revoke old, issue new (same Family_Id)
                                   └ new cookie + new access token
  POST /api/auth/logout ─────────► revoke family · clear cookie · audit
```

**Why the refresh token is a cookie, not `localStorage`.** A single XSS on the SPA reads
`localStorage` and walks away with a 14-day credential. `HttpOnly` puts the refresh token out
of JavaScript's reach; the access token lives in memory and dies with the tab. `SameSite=Strict`
plus a `Path=/api/auth` scope plus an `Origin` check on the refresh endpoint covers CSRF.
This changes the template's current `localStorage` session — deliberately, and noted.

**Also delivered:** logout (single and all-devices) · password reset (single-use, hashed,
expiring token) · email verification · account lockout (configurable threshold and window,
already partly present in `AuthService`) · password policy from configuration.

**SSO readiness.** Nothing in the application layer knows how a principal was authenticated.

```csharp
public interface IExternalIdentityProvider {          // Entra ID / Azure AD / any OIDC
    string Scheme { get; }
    Task<ExternalPrincipal> ValidateAsync(string token, CancellationToken ct);
}
public interface IUserProvisioner {                   // just-in-time account creation
    Task<ApplicationUser> ProvisionAsync(ExternalPrincipal principal, CancellationToken ct);
}
```

Adding Entra ID later = register the OIDC scheme in `ConfigureAuthentication`, implement one
`IExternalIdentityProvider`, and link the external identity to `AspNetUserLogins` (already in
the schema via `IdentityUserLogin<Guid>`). **No change to controllers, handlers, or the SPA's
session model.**

---

## 8. Authorization / permission architecture

The template already has a DB-driven permission engine
(`MainModule → Screen → ScreenAction → LinkScreenAction → GroupPermissions / UserPermissions`,
flattened by `VW_UserActions`). It is **reused, not replaced.** One column is added:

```
Common_LinkScreenActions.Permission_Key   nvarchar(100)  UNIQUE   -- "Post.Create"
```

so the same row simultaneously drives the navigation menu (its `Screen`) and named permission
checks. One source of truth, no parallel system.

```
Roles (Security_UserGroups, seeded)          Permissions (Permission_Key, seeded)
  SuperAdmin                                   Employee.{View,Create,Edit,Delete}
  Admin                                        Post.{View,Create,Edit,Delete,Moderate}
  HR                                           Announcement.{Create,Publish}
  CommunicationsManager                        Community.Manage      Survey.Manage
  DepartmentManager                            Event.Manage          Recognition.Manage
  Moderator                                    Analytics.View        Settings.Manage
  Employee                                     AuditLog.View         Document.Manage
                                               Organization.Manage   Role.Manage
```

**Resolution order** for "may this user do X":

1. `Security_UserPermissions` explicit **deny** for this user → **no** (deny always wins).
2. `Security_UserPermissions` explicit **grant** → yes.
3. Any of the user's groups grants it via `Security_GroupPermissions` → yes.
4. `SuperAdmin` → yes (bypass, and every bypass is audited).
5. Otherwise no.

**Enforcement layers** (defence in depth — a missing check at one layer is caught by another):

| Layer | Mechanism |
| ----- | --------- |
| Endpoint | `[HasPermission(Permissions.Post.Create)]` → `PermissionAuthorizationHandler` via a dynamic `IAuthorizationPolicyProvider` |
| Use case | `AuthorizationBehavior` in the MediatR pipeline, from `[RequiresPermission]` on the request |
| Resource | `IResourceAuthorizer<T>` — *may this user edit **this** post?* — closes the IDOR hole that a permission check alone leaves open |
| Data | Audience filter applied inside every feed / document / event query, so unauthorised rows are never even selected |
| UI | `*hasPermission` structural directive + `permissionGuard` on routes — **presentation only, never the real check** |

The permission set is cached per user (`ICacheService`, 5-minute sliding, invalidated on any
role or permission change) so the hot path does not hit `VW_UserActions` on every request.

---

## 9. Feed architecture

The hard part: for 100 000 employees and millions of posts, "show me what I'm allowed to see,
newest first" must stay a couple of index seeks.

**Rejected — fan-out on write.** A company-wide announcement would write 100 000 timeline rows.

**Chosen — audience-key intersection, computed at read time from a tiny cached key set.**

```
1.  Resolve the viewer's audience keys  (IAudienceResolver, cached 10 min per employee):

      ['ALL', 'DEPT:{dept}', 'DEPT:{parent-dept}', …, 'TEAM:{team}', 'LOC:{loc}',
       'JOB:{job}', 'ROLE:{r1}', 'ROLE:{r2}', 'COMM:{c1}', …, 'EMP:{me}']

    Typically 10–30 short strings, even in a large org.

2.  Feed query — one EXISTS against a covering index, no joins fanning out rows:

      SELECT <projection>
      FROM   Feed_Posts p
      WHERE  p.Is_Deleted = 0
        AND  p.Status = 'Published'
        AND  p.Published_Date <= @now
        AND (@communityId IS NULL OR p.Community_Id = @communityId)
        AND  EXISTS (SELECT 1 FROM Feed_PostAudiences a
                     WHERE a.Post_Id = p.Id AND a.Audience_Key IN (@keys))
        AND (p.Published_Date, p.Id) < (@cursorDate, @cursorId)   -- keyset, not OFFSET
      ORDER BY p.Published_Date DESC, p.Id DESC
      OFFSET 0 ROWS FETCH NEXT @size ROWS ONLY;

    Indexes: IX_Feed_PostAudiences_Key (Audience_Key) INCLUDE (Post_Id)
             IX_Feed_Posts_Timeline (Status, Is_Deleted, Published_Date DESC, Id DESC)
                                    INCLUDE (Author_Employee_Id, Community_Id, Post_Type, …)

3.  Pinned posts are a separate small query (filtered index) unioned at the top of page 1 only.

4.  Enrichment — one batched round trip each, never per row (no N+1):
      · authors            → single WHERE Id IN (…) projection
      · attachments        → single WHERE Post_Id IN (…)
      · reaction summary   → counters already denormalised on Feed_Posts
      · the viewer's own reaction → single WHERE Post_Id IN (…) AND Employee_Id = @me
      · top 2 comments     → one windowed query with ROW_NUMBER()

5.  Counters (Reactions_Count, Comments_Count, Views_Count) are updated by atomic
    ExecuteUpdateAsync increments in the same transaction as the reaction/comment,
    and reconciled nightly by a Hangfire job in case anything drifts.
```

**Why keyset, not `OFFSET`.** `OFFSET 40000` makes SQL Server read and discard 40 000 rows.
A cursor on `(Published_Date, Id)` is a constant-cost seek at any depth. The feed and comment
threads use keyset; admin grids, where users jump to page 37, use `PagedResult<T>` with `OFFSET`.

**Scheduled posts.** A Hangfire recurring job flips `Scheduled → Published` and raises
`PostPublishedEvent`; no polling from the client.

---

## 10. Notification architecture

```
Handler raises a domain event (PostCommentedEvent, EmployeeMentionedEvent, RecognitionSentEvent, …)
        │  after SaveChanges succeeds — never inside the transaction
        ▼
DomainEventDispatcher (MediatR INotification)
        ▼
NotificationDispatcher
   ├ resolve recipients (author, thread participants, mentioned, audience, followers)
   ├ drop the actor (nobody is notified about their own action)
   ├ filter by Notif_Preferences per channel and per notification type
   ├ INSERT 1 × Notifications + N × NotificationUsers  (single bulk insert, batched at 500)
   ├ IN-APP / REAL TIME  → IRealtimeNotifier
   │        SignalR NotificationHub, group "user:{userId}"
   │        · JWT-authenticated (query-string token for the WS handshake)
   │        · Redis backplane when scaled out; single-node without it
   │        · client receives {notification, unreadCount} and updates a signal
   └ EMAIL → INotificationEmailSender (abstraction)
            enqueued to Hangfire, rendered from EmailSMSTemplates (ar/en),
            digestable (immediate | hourly | daily), retried with backoff,
            logged to EmailSMSHistory
```

**Unread count.** Cached per user, incremented in place on push, invalidated on
mark-read/mark-all/delete — never a `COUNT(*)` on the hot path.
Backing index: `IX_NotificationUsers_Unread (Reciever_Id, IS_Seen, Id DESC)`.

**Types:** comment · reply · mention · reaction · recognition · announcement · event reminder ·
survey invitation · community invitation · membership approval · post moderation.

**Push notifications later:** add an `IPushSender` implementation next to the email sender;
`NotificationDispatcher` gains one channel and nothing else changes.

---

## 11. File-storage architecture

```csharp
public interface IFileStorageService {
    string  Provider { get; }
    Task<StoredFile> SaveAsync(FileUploadRequest request, CancellationToken ct);
    Task<Stream>     OpenReadAsync(string storageKey, CancellationToken ct);
    Task             DeleteAsync(string storageKey, CancellationToken ct);
    Task<bool>       ExistsAsync(string storageKey, CancellationToken ct);
    Task<Uri?>       GetPresignedUrlAsync(string storageKey, TimeSpan ttl, CancellationToken ct);
}
```

| Implementation                | Status | Notes |
| ----------------------------- | ------ | ----- |
| `LocalFileStorageService`     | built in Phase 1 | date-sharded paths, wraps the template's `FileManager`; presigned URL = short-lived HMAC token |
| `AzureBlobFileStorageService` | contract + registration ready | real SAS URLs |
| `S3FileStorageService`        | contract + registration ready | real presigned URLs |

Selected by configuration (`Storage:Provider`), never by a compile-time reference.

**Upload pipeline — every upload passes all of it:**

```
size limit (per type, from config)
  → extension allow-list (never a deny-list)
  → declared Content-Type must match the allow-list
  → MAGIC-BYTE SNIFF: the real content type must match the declared one   ← the check that matters
  → file name sanitised; the stored name is a generated Guid, never user input
  → images re-encoded (strips EXIF, kills polyglot payloads)
  → SVG rejected outright (it is executable markup)
  → SHA-256 checksum → de-duplication
  → antivirus hook (IFileScanner, no-op by default, ICAP/Defender-ready)
  → metadata row in Doc_Files; bytes to the provider
```

**Downloads are always authorised, never a bare path.** `GET /api/files/{fileId}` resolves the
owning entity, runs `IResourceAuthorizer`, writes a `Doc_DownloadLogs` row, then streams —
or hands back a short-lived presigned URL. The storage container is **never** publicly readable
and is **never** served by static-file middleware.

---

## 12. NuGet packages

Managed centrally in `Directory.Packages.props`.

| Package | Where | Purpose |
| ------- | ----- | ------- |
| `MediatR` | Application | CQRS dispatch + domain events |
| `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` | Application | validation |
| `AutoMapper` *(present)* | Application | projection/mapping |
| `Ganss.Xss` | Application | HTML sanitisation for rich post content |
| `Microsoft.EntityFrameworkCore.SqlServer` / `.Design` / `.Tools` *(present)* | Infrastructure | ORM |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` *(present)* | Infrastructure | identity |
| `Hangfire.AspNetCore`, `Hangfire.SqlServer` | Infrastructure / API | background jobs |
| `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis` | Infrastructure | distributed cache (optional) |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | API | hub backplane when scaled out |
| `Azure.Storage.Blobs` / `AWSSDK.S3` | Infrastructure | cloud storage providers |
| `MailKit` | Infrastructure | SMTP (replaces `System.Net.Mail` + `Microsoft.Exchange.WebServices`) |
| `ClosedXML` | Application | Excel export for analytics and surveys |
| `Serilog.AspNetCore`, `.Sinks.Console`, `.Sinks.File`, `.Sinks.MSSqlServer`, `.Enrichers.*`, `Serilog.Expressions` | API | structured logging |
| `Swashbuckle.AspNetCore` *(present)* + `.Annotations` | API | OpenAPI + JWT auth in Swagger UI |
| `Asp.Versioning.Mvc` + `.ApiExplorer` | API | API versioning |
| `Microsoft.AspNetCore.Authentication.JwtBearer` *(present)* | API | JWT |
| `Microsoft.AspNetCore.OpenIdConnect` | API | *(later)* Entra ID / OIDC |
| `AspNetCoreRateLimit` **or** built-in `RateLimiter` | API | rate limiting — built-in preferred, no dependency |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` | tests | test host |
| `FluentAssertions`, `NSubstitute`, `Bogus` | tests | assertions, doubles, data |
| `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.MsSql`, `Respawn` | integration tests | real SQL Server per run, reset between tests |
| `NetArchTest.Rules` | Application.Tests | enforce the layer rules from §1 |

**Removed:** `Microsoft.Exchange.WebServices` (unmaintained, .NET-Framework-era) and
`System.Data.SqlClient` (superseded by `Microsoft.Data.SqlClient`, which is already present —
having both in `GlobalUsing.cs` is an ambiguity waiting to happen).

## 13. npm packages

| Package | Purpose |
| ------- | ------- |
| `@microsoft/signalr` | real-time notifications |
| `@angular/cdk` | virtual scroll, overlay, a11y, drag-drop — **without** Material's visual language |
| `chart.js` + `ng2-charts` | analytics and poll charts (lighter and more themeable than ECharts here) |
| `date-fns` + `date-fns/locale` | date maths and Arabic/English formatting |
| `dompurify` | client-side sanitisation of rendered rich content (belt and braces with server-side) |
| `@tiptap/core`, `@tiptap/starter-kit`, `@tiptap/extension-mention` | rich-text composer with `@mention` support |
| `ngx-infinite-scroll` *(or a 20-line custom directive)* | feed paging — prefer the custom directive |
| `xlsx` | client-side Excel export where the server round trip is not worth it |
| `jest` / `@angular/build:unit-test` *(vitest is already wired)* | tests — **keep the existing vitest runner**, it is configured and working |

**Not adopted:** Angular Material (see §0), NgRx (see §6), `ngx-translate` (see §0).

---

## 14. Development phases

| # | Phase | Key output |
| - | ----- | ---------- |
| 1 | **Architecture and projects** | central package management, test projects, cross-cutting foundation (exceptions → ProblemDetails, Serilog + correlation id, `ApiResponse`/`PagedResult`, storage/cache/clock abstractions, MediatR + FluentValidation pipeline, Swagger + JWT, security headers, rate limiting, secrets out of source) |
| 2 | Domain entities | all entities from §3, enums, domain events |
| 3 | EF configurations and migrations | one `IEntityTypeConfiguration` per entity, indexes, constraints, filters, seed |
| 4 | Authentication and authorisation | refresh-token rotation, `HasPermission`, resource authorisation, permission cache, lockout, reset, verification |
| 5 | Employees and organisation | org CRUD, employee profiles, hierarchy, follow, org chart |
| 6 | Posts, feed, comments, reactions, mentions | the feed engine of §9 + composer UI |
| 7 | Notifications and SignalR | dispatcher, hub, preferences, bell UI |
| 8 | Communities | privacy, membership workflow, community feed and moderation |
| 9 | Recognition | types, sending, leaderboard, points |
| 10 | Polls and surveys | builder, targeting, submission, results, export |
| 11 | Events | calendar, RSVP, capacity, reminders |
| 12 | Documents | categories, versions, permissions, audited downloads |
| 13 | Search | SQL Server full-text, unified search API and UI |
| 14 | Analytics | KPIs, charts, filters, Excel export |
| 15 | Admin portal | every management screen in §33 of the brief |
| 16 | Testing, security, performance | coverage, arch tests, load profile, remove lazy loading, index review, pen-test checklist |
| 17 | Docker and deployment | Dockerfiles, compose, CI/CD pipeline, runbook |

**Every phase starts with:** analyse existing code → state what will be implemented → identify DB/API/UI
changes → identify security implications → identify what to reuse.
**Every phase ends with:** build backend → build Angular → run tests → fix all errors → report files
created/modified → summarise done → state what remains.

---

## 15. Security checklist

| Threat | Control | Phase |
| ------ | ------- | ----- |
| XSS (stored) | `Ganss.Xss` sanitisation on write with a strict allow-list; `DomPurify` on render; CSP without `unsafe-inline`; Angular's default escaping never bypassed with `bypassSecurityTrustHtml` on user content | 1, 6 |
| CSRF | Refresh cookie is `SameSite=Strict` + `Path=/api/auth` + `Origin` check; the access token is a header, which is not auto-sent cross-site | 4 |
| SQL injection | EF parameterisation everywhere; the template's `ExecuteSqlQueryAsync`/`ExecuteProc` audited for interpolation and forced to `FromSqlInterpolated`/`SqlParameter` | 1, 16 |
| Broken authorisation | Deny-by-default `[Authorize]` on the base controller (already present); permission at endpoint **and** use case; every gap caught by a test | 4 |
| **IDOR** | `IResourceAuthorizer<T>` on every by-id operation — the check nobody remembers and everybody needs | 4 |
| Mass assignment | Commands are explicit DTOs; entities are **never** model-bound; no `TryUpdateModel` | 1 |
| File upload attacks | §11 pipeline: allow-list, magic-byte sniff, re-encode, Guid names, SVG rejected, size caps, AV hook, non-public store | 1, 12 |
| Unrestricted file access | Authorised streaming endpoint + audience check + download audit; storage never publicly readable | 12 |
| JWT theft | 15-minute access token, memory-only; refresh in `HttpOnly` `Secure` `__Host-` cookie | 4 |
| Refresh-token replay | Rotation + hashed-at-rest + **family revocation on reuse detection** + audit | 4 |
| Credential stuffing / brute force | Lockout after N failures in a window; rate limit on `/api/auth/*`; login attempts logged | 4 |
| Enumeration | Login, reset and verification return the same message and comparable timing regardless of whether the account exists | 4 |
| Secrets in source | User-secrets in dev, environment/Key Vault in prod; `appsettings.json` holds no secret; **the committed SMTP password is rotated** | 1 |
| Transport | HTTPS redirect + HSTS (preload in prod) | 1 |
| Headers | CSP, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options: DENY`, `Permissions-Policy`, `Cross-Origin-Opener-Policy` | 1 |
| CORS | Explicit origin allow-list from configuration — never `AllowAnyOrigin` with credentials | 1 |
| DoS | Global + per-endpoint rate limits; request body size caps (the current `MultipartBodyLengthLimit = long.MaxValue` is replaced by a real limit); pagination hard-capped at 100 | 1 |
| Information disclosure | `ProblemDetails` without stack traces outside development; generic 500s; details only in the correlated log | 1 |
| Audit trail | `Audit_Logs` via an EF `SaveChanges` interceptor + explicit events for login, publish, permission change, download | 1, 3 |
| Dependency risk | `dotnet list package --vulnerable` and `npm audit` in CI | 17 |

## 16. Performance considerations

Target: 100 000 employees, millions of posts, comments and reactions.

- **Keyset pagination** on every high-cardinality timeline; `OFFSET` only in admin grids.
- **Projection, not `Include`.** Read paths `Select` into DTOs — never materialise an aggregate to
  read three fields. `AsNoTracking()` (or `AsNoTrackingWithIdentityResolution()`) on all queries.
- **No N+1.** Enrichment is batched `WHERE Id IN (…)`. New read paths never touch lazy-loaded
  navigations; the arch tests and code review enforce it.
- **Denormalised counters** with atomic `ExecuteUpdateAsync` increments, reconciled nightly.
- **Covering indexes** for the feed, notifications, comments, audience keys, and audit lookups;
  filtered indexes for pinned/featured posts and unread notifications.
- **Caching** via `ICacheService`: permission sets (5 min), audience keys (10 min), org tree
  (30 min), lookups/master data (1 h), analytics aggregates (15 min), unread counts (in place).
  Memory-backed by default, Redis when `Redis:Enabled` — the application code cannot tell.
- **Background work** for anything slow: notification fan-out, email, scheduled publishing,
  leaderboard snapshots, analytics rollups, search-index maintenance, audit archival, event reminders.
- **Async all the way** with `CancellationToken` threaded from controller to `SaveChangesAsync`.
- **Response caching / ETags** on genuinely static reads (org tree, master data, document categories).
- **Analytics reads from pre-aggregated rollup tables**, never from `Feed_Posts` at runtime.
- **`SaveChangesAsync` fixed** so it stops issuing an extra `ApplicationUsers` query per save.
- **Connection resiliency**: `EnableRetryOnFailure` with a bounded strategy.
- **Split queries** where a legitimate `Include` would otherwise produce a cartesian explosion.
- **Bulk insert** for notification fan-out, batched at 500 rows.
- **Angular**: lazy routes, `@defer`, `OnPush` everywhere, virtual scroll, `NgOptimizedImage`,
  debounced search (300 ms) with `switchMap`, and a production bundle budget that fails the build.

## 17. Deployment architecture

```
                      ┌──────────────────────────────┐
   Internet  ────────►│  Reverse proxy / ingress     │  TLS termination, HSTS,
                      │  (nginx · App Gateway · ALB) │  WAF, rate limiting
                      └───────┬──────────────┬───────┘
                              │              │
                   /  (static)│              │/api, /hubs, /jobs
                      ┌───────▼──────┐  ┌────▼──────────────────────┐
                      │ workvivo-ui  │  │ workvivo-api (N replicas) │
                      │ nginx:alpine │  │ ASP.NET Core, non-root    │
                      │ built Angular│  │ /health/live /health/ready│
                      └──────────────┘  └────┬──────────────────────┘
                                             │
                    ┌───────────────┬────────┴──────┬──────────────────┐
              ┌─────▼──────┐  ┌─────▼──────┐  ┌─────▼──────┐  ┌────────▼───────┐
              │ SQL Server │  │   Redis    │  │ Blob / S3  │  │ SMTP / Graph   │
              │ app + jobs │  │ cache + WS │  │  files     │  │                │
              │ + full-text│  │ backplane  │  │            │  │                │
              └────────────┘  └────────────┘  └────────────┘  └────────────────┘
```

**`docker-compose.yml` (local development):** `api`, `ui`, `sqlserver` (2022 developer),
`redis`, plus a one-shot `migrator` that applies EF migrations before `api` starts. Named
volumes for database and uploaded files; `.env` for secrets; the API waits on a SQL health check.

**Images.** API: multi-stage `sdk` → `aspnet:10.0-alpine`, non-root user, no SDK in the runtime
layer. UI: multi-stage `node:22-alpine` build → `nginx:alpine`, with gzip/brotli, cache headers,
and an SPA fallback.

**Scale-out requirements** (all designed in from the start): stateless API, Redis SignalR
backplane, distributed cache, Hangfire server electing a single scheduler, Data Protection keys
persisted to a shared store so cookies and tokens survive a restart or a different replica.

**CI/CD structure.** `restore → build → unit tests → integration tests (Testcontainers) →
`dotnet list package --vulnerable` + `npm audit` → publish → build and scan images → push →
migrate → deploy → smoke test`. Migrations run as a separate gated step, never automatically
on application start in production.

**Configuration** comes from environment variables in every deployed environment:
`ConnectionStrings__WorkvivoConnStr`, `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`,
`Storage__Provider`, `Storage__ConnectionString`, `Redis__ConnectionString`,
`MailSettings__Password`, `Cors__AllowedOrigins__0`. **No secret is ever committed.**

---

## Appendix A — standard API contracts

```jsonc
// success
{ "success": true, "message": "", "data": { }, "traceId": "0HN7…" }

// paged
{ "success": true, "message": "", "traceId": "0HN7…",
  "data": { "items": [], "pageNumber": 1, "pageSize": 20,
            "totalCount": 250, "totalPages": 13,
            "hasPrevious": false, "hasNext": true } }

// keyset-paged (feed, comments)
{ "success": true, "traceId": "0HN7…",
  "data": { "items": [], "nextCursor": "MjAyNi0wOS0wOVQxMjowMDowMFo…", "hasMore": true } }

// failure — RFC 7807
{ "type": "https://workvivo/errors/validation", "title": "One or more validation errors occurred.",
  "status": 400, "traceId": "0HN7…",
  "errors": { "content": ["Content is required."], "audiences": ["Select at least one audience."] } }
```

| Exception              | HTTP | `type` slug   |
| ---------------------- | ---- | ------------- |
| `ValidationException`  | 400  | `validation`  |
| `UnauthorizedException`| 401  | `unauthorized`|
| `ForbiddenException`   | 403  | `forbidden`   |
| `NotFoundException`    | 404  | `not-found`   |
| `ConflictException`    | 409  | `conflict`    |
| `BusinessRuleException`| 422  | `business-rule` |
| `RateLimitException`   | 429  | `rate-limit`  |
| anything else          | 500  | `server-error` (no detail outside development) |

## Appendix B — API surface

```
/api/auth        login · refresh · logout · logout-all · forgot-password · reset-password
                 verify-email · resend-verification · me · change-password
/api/employees   list · get · me · update · avatar · cover · skills · interests
                 follow · unfollow · followers · following · directory · org-chart
/api/organization  departments · teams · locations · job-titles · tree
/api/posts       feed · get · create · update · delete · publish · schedule · pin · feature
                 archive · drafts · views · attachments
/api/comments    list (by post, keyset) · create · reply · update · delete
/api/reactions   react · unreact · summary · reactors
/api/communities list · get · create · update · join · leave · invite · members
                 approve · reject · moderators · feed
/api/recognition send · list · received · sent · types · leaderboard
/api/polls       get · create · vote · results · close
/api/surveys     list · get · create · update · publish · submit · results · export
/api/events      list · calendar · get · create · update · rsvp · attendees · cancel
/api/documents   categories · list · get · upload · new-version · versions · download · search
/api/files       upload · {id} (authorised stream) · delete
/api/notifications  list · unread-count · mark-read · mark-all-read · delete · preferences
/api/search      global · employees · posts · communities · documents · events · suggest
/api/analytics   overview · engagement · content · recognition · surveys · events
                 communities · top-contributors · export
/api/admin       roles · permissions · role-permissions · user-permissions · moderation
                 settings · audit-logs · seed-status
/hubs/notifications   SignalR
/jobs                 Hangfire dashboard (SuperAdmin only)
/health/live /health/ready
```


---

## Appendix C — Phase 1 record

Delivered, verified, and the deviations worth knowing about.

### Framework and packaging

- **.NET 9 → .NET 10** across all projects. `dotnet build` is clean.
  - EF Core 10 removed `SetPropertyCalls<T>`; `IBaseRepository.ExecuteUpdateAsync` and its single
    call site were migrated to `Action<UpdateSettersBuilder<T>>`, and the signature gained a
    `CancellationToken`.
  - `Microsoft.OpenApi` v2 (via Swashbuckle 10) moved `Microsoft.OpenApi.Models` to
    `Microsoft.OpenApi`, changed `ISchemaFilter` to take `IOpenApiSchema`, models examples as
    `System.Text.Json` nodes, and takes security requirements through a factory.
- **Central Package Management.** Every version lives in `Directory.Packages.props`; no `.csproj`
  carries one. Transitive pinning is on, which immediately caught a `Microsoft.Data.SqlClient`
  downgrade that Serilog's SQL sink would otherwise have forced silently.
- **`FrameworkReference Microsoft.AspNetCore.App`** replaces the .NET-Framework-era shim packages
  the template pulled in (`Microsoft.AspNetCore.Identity` 2.3.1, `Cryptography.*`, `DataProtection`).
- **`global.json`** pins the SDK band and selects the Microsoft.Testing.Platform test runner.

### Security fixes applied

| Finding | Status |
| ------- | ------ |
| **AutoMapper 15.0.1 carries GHSA-rvv3-g6hj-g44x (high severity)** | Upgraded to 16.2.0 — the only release without the advisory. 15.0.2, 15.1.0 and 16.0.0 all still warn. |
| Hard-coded JWT signing key, duplicated in two files | Moved to `Jwt:SigningKey`. **The API refuses to start** without a key of at least 32 characters. A development key is set in user-secrets. |
| SMTP password committed in `appsettings.json` | Removed from the file. **The old value is still in the working tree's history and must be rotated.** |
| No security headers | `SecurityHeadersMiddleware` — CSP, nosniff, DENY framing, Referrer-Policy, Permissions-Policy, COOP/CORP. `Server` header suppressed at the Kestrel level. |
| CORS origins hard-coded in `Program.cs` | `Cors:AllowedOrigins` from configuration, empty by default, `AllowCredentials` for the refresh cookie. |
| `MultipartBodyLengthLimit = long.MaxValue` | Reduced to 512 MB at the transport, with tighter per-category limits in `Storage:Validation`. |
| No rate limiting | Global limiter plus a tight `auth` policy and a concurrency-bounded `upload` policy. |
| Stack traces reachable by clients | `GlobalExceptionHandlingMiddleware` returns ProblemDetails; detail is included only in Development. |
| `IHttpContextAccessor` never registered, though `UserAccessor` injects it | Registered. This was a latent DI failure waiting for the first request that resolved it. |

### Performance fixes applied

- **`SaveChangesAsync` no longer issues an extra query per save.** It used to run
  `ApplicationUsers.FirstOrDefaultAsync` unconditionally on every write, purely to have an audit-user
  fallback. It is now resolved lazily, at most once, and only when there is no signed-in user. It
  also throws rather than stamping `Guid.Empty` when no actor can be determined — an audit row that
  cannot say who made a change is worse than a failed save.

### Foundation added

```
Workvivo.Domain
  Exceptions/            AppException + NotFound · Validation · Forbidden · Unauthorized ·
                         Conflict · BusinessRule · RateLimit
  Abstractions/          IFileStorageService · IFileScanner · ICacheService · IDateTimeProvider ·
                         ICurrentUser · IContentSanitizer · IBackgroundJobScheduler · IRealtimeNotifier
  Models/Storage/        FileUploadRequest · StoredFile · FileCategory
  IUnitOfWork            + BeginTransaction / Commit / Rollback / HasActiveTransaction,
                           + cancellable SaveChangesAsync

Workvivo.Application
  Bases/ApiResponse      success envelope, superset of ServiceResponse<T>
  Common/Paging/         PagedResult · CursorPagedResult · Cursor · PageRequest · CursorRequest
  Common/Messaging/      ICommand · ICommand<T> · IQuery<T> · ICacheableQuery
  Behaviors/             RequestLogging → Performance → Validation → Caching → Transaction
  DependencyInjection    AddApplicationLayer()

Workvivo.Infrastructure
  Storage/               LocalFileStorageService · FileSignatureValidator · FileBufferingStream ·
                         NullFileScanner · StorageOptions · FileValidationOptions
  Caching/               DistributedCacheService (memory or Redis, chosen by configuration) · CacheKeys
  Common/                CurrentUser · SystemCurrentUser · SystemDateTimeProvider · HtmlSanitizerAdapter
  Jobs/                  HangfireBackgroundJobScheduler · InlineBackgroundJobScheduler
  DependencyInjection    AddInfrastructureLayer()

Workvivo.API
  Middleware/            CorrelationId · GlobalExceptionHandling · SecurityHeaders
  Extensions/            Swagger(+JWT) · Serilog · Cors · RateLimiting · Localization
  Options/               JwtOptions (validated at startup) · CorsOptions
  Program.cs             rewritten; every existing registration preserved
```

### Tests

`tests/` holds four xUnit v3 projects, all building with warnings-as-errors. 49 tests, all passing:

| Project | Tests | Covers |
| ------- | ----: | ------ |
| `Workvivo.Domain.Tests` | 6 | exception → status-code contract; that no failure type escapes the hierarchy |
| `Workvivo.Application.Tests` | 29 | cursor round-trip and hostile input, page clamping, `PagedResult` maths, validation behaviour, **architecture rules** |
| `Workvivo.API.Tests` | 14 | security headers, correlation-id sanitisation and log-injection rejection |
| `Workvivo.IntegrationTests` | 0 | scaffolded; fixture arrives with the authentication phase |

The architecture tests matter more than usual here: because the project references run
`Application → Infrastructure`, the compiler cannot stop application code reaching for a
`DbContext` or a concrete storage provider. `NetArchTest` does instead.

### Known issue: `dotnet test`

`dotnet test` does not work on this combination. SDK 10.0.401 drives xunit.v3 4.0.0's
Microsoft.Testing.Platform host over a handshake the two do not agree on, and every assembly comes
back "Zero tests ran" (exit code 5) — while running the same assembly directly discovers and passes
everything. Downgrading to xunit.v3 3.2.2 is worse: the host prints its help text and exits.

**Run the suite with `./test.ps1`.** It builds each test project and invokes the self-hosting
executable xunit.v3 produces, which is what `dotnet test` would do if the handshake worked, and it
aggregates the results and exit code. Delete the script once a later SDK or xunit release fixes the
integration; nothing about the tests themselves needs to change.

### Third-party licensing — worth a decision before Phase 2

Three widely used libraries changed licence recently, and two are already in this solution:

| Package | Licence | Status here |
| ------- | ------- | ----------- |
| **AutoMapper 16.2.0** | Commercial above a revenue threshold | **Already in use** (inherited from the template, and the upgrade was forced by a CVE). |
| MediatR 13+ | Commercial above a revenue threshold | Avoided — pinned to **12.4.1**, the last Apache-2.0 release, which is fully featured. |
| FluentAssertions 8+ | Paid | Avoided — using **Shouldly** (BSD-3). |

AutoMapper is the one that needs a decision. It may need a paid licence for commercial use, and the
alternative — hand-written mapping extension methods — is more code but faster, trivially debuggable,
and free. Cheapest to change now, before there are dozens of profiles.

### Deferred deliberately

- Azure Blob and S3 storage providers: contracts and registration exist; configuring a provider
  other than `Local` throws at startup rather than silently writing production uploads to a
  container's ephemeral disk.
- Hangfire is registered but **off** (`BackgroundJobs:Enabled: false`) until there are real jobs.
  Queued work meanwhile runs inline and logs a warning every time, so the mode is never silent.
- Domain-event dispatch, `Docker`, and CI/CD land in their own phases.

---

## Appendix D — Phase 2 record

Domain entities and enums. No EF configuration, no migration, no API or UI surface.

### What was added

| Area | Count | Entities |
| ---- | ----: | -------- |
| Organisation | 12 | `Organization` `Department` `Team` `Location` `JobTitle` `Employee` `EmployeeManager` `Skill` `EmployeeSkill` `Interest` `EmployeeInterest` `EmployeeFollower` |
| Feed | 9 | `Post` `PostAttachment` `PostAudience` `PostReaction` `PostMention` `PostView` `Comment` `CommentReaction` `CommentMention` |
| Communities | 3 | `Community` `CommunityMember` `CommunityInvitation` |
| Recognition | 3 | `RecognitionType` `Recognition` `RecognitionLeaderboardSnapshot` |
| Polls | 4 | `Poll` `PollOption` `PollVote` `PollAudience` |
| Surveys | 6 | `Survey` `SurveyQuestion` `SurveyQuestionOption` `SurveyResponse` `SurveyAnswer` `SurveyAudience` |
| Events | 3 | `Event` `EventAttendee` `EventAudience` |
| Documents | 6 | `FileAsset` `DocumentCategory` `Document` `DocumentVersion` `DocumentAudience` `DocumentDownloadLog` |
| Audit | 1 | `AuditLog` |
| Identity | 1 | `RefreshToken` |
| Notifications | 1 | `NotificationPreference` |
| Base types | 2 | `AuditableEntity<TKey>` / `IHasDomainEvents`, `AudienceEntity<TKey>` / `AudienceKey` |

**49 entities**, plus 11 enum files (`PostType`, `PostStatus`, `PostVisibility`, `ReactionType`,
`PostAttachmentType`, `AudienceType`, `EmployeeStatus`, `CommunityPrivacy`, `CommunityMemberRole`,
`MembershipStatus`, `InvitationStatus`, `RecognitionVisibility`, `LeaderboardPeriod`, `PollStatus`,
`SurveyStatus`, `SurveyQuestionType`, `EventType`, `EventFormat`, `EventStatus`, `EventResponse`,
`FileScanStatus`, `NotificationType`, `NotificationChannel`, `NotificationDigestFrequency`,
`AuditAction`) and `LocalizedText`.

### Existing entities extended, never replaced

- **`ApplicationUser`** — gained `Employee` (1:1) and `RefreshTokens`. Nothing else touched.
- **`LinkScreenAction`** — gained `Permission_Key`, `Module`, `Description_Ar/En`, so the
  template's screen/action engine also serves named permissions. **One catalogue, not two.**
- **`GlobalUsing.cs`** in Domain, Infrastructure and Application — nine new entity namespaces,
  matching the template's existing convention.

### Design decisions worth knowing

**`Employee` is separate from `ApplicationUser`, one-to-one.** The user row is a credential;
the employee row is HR and social data. Keeping them apart lets an employee record exist before
the account is provisioned or after it is disabled, and means an SSO migration that replaces how
people authenticate does not touch the org chart, the feed, or any post's authorship.

**`AudienceEntity<TKey>` shares the shape; the tables stay separate.** Each audience table keeps
a real foreign key to its owner rather than a polymorphic id with no referential integrity. What
is shared is one invariant: `Audience_Key` is derived from `Audience_Type` and `Target_Id`, all
three are written together through `Retarget`, and the setters are private. If they could drift,
a post targeted at one department would silently reach the wrong people and nothing would fail.

**Three invariants live in the domain, not in handlers** — so a second code path cannot bypass
them: comment nesting capped at depth 2 (`Comment.ReplyDepth()`), self-recognition rejected
(`Recognition.EnsureNotSelfRecognition`), and the audience consistency above.

**Denormalised counters throughout** (`Reactions_Count`, `Comments_Count`, `Members_Count`,
`Download_Count`, …). A feed page shows twenty posts; counting per post would be forty aggregates
against the two largest tables. Maintained by atomic increments in the write transaction,
reconciled nightly.

**`Department.Path`** is a materialised ancestor path. "Everything under this division" is asked
by audience targeting, the org chart and every departmental analytics filter; a recursive CTE per
request does not hold at 100,000 employees, and a LIKE on an indexed prefix does.

**Anonymity is real, not a flag.** Anonymous polls and surveys store `Employee_Id` as null and
carry a keyed `Voter_Hash` / `Respondent_Hash` instead. That enforces one response per person
without recording who they were — and the flag has to be fixed before the first response, since
flipping it later either exposes people who answered in confidence or discards attribution that
was given knowingly.

**`SurveyResponse` copies department and location at submission time** so analytics keeps meaning
after a reorganisation — and because on an anonymous survey there is no employee row to join to.

**`AuditLog` denormalises `Username`.** Joining to the user table would show today's name, and
nothing at all once the account is removed — exactly when the audit row matters most.

**`RefreshToken` stores only a SHA-256 hash**, rotates on use, and carries a `Family_Id` so
presenting an already-revoked token reveals a replay and revokes the whole family. Rotation alone
cannot detect that; the family is what makes it visible.

**Domain events: infrastructure only.** `IDomainEvent`, `IHasDomainEvents` and `AuditableEntity`
exist so the dispatch pipeline has something to hang off. Concrete events are defined by the phase
that raises them — an event nobody publishes is a guess about a feature that has not been built.

### Verification

- Solution builds: **0 errors**, 417 warnings — all pre-existing template code, **none in the new
  entities** (verified by filtering the warning list to the new folders).
- **107 backend tests pass**, up from 49. The 58 new ones cover the audience-key invariant
  (including the collision case where two dimensions share a target id), comment depth, community
  privacy and role/status separation, post visibility boundaries, event capacity, the bilingual
  fallback rule, and refresh-token activity.
- Angular builds; its 11 tests still pass.

### Not done in this phase, by design

EF configurations, indexes, constraints and the migration are Phase 3 — including the
`Audience_Key` unique and covering indexes, the `CHECK` constraint mirroring the self-recognition
rule, `RowVersion` mapping, soft-delete query filters, and the seed data.
