using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Search.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Events;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Application.Features.Search.Queries.Search;

/// <summary>
/// Search across the product.
///
/// Every branch is filtered by the same audience key set the owning feature uses.
/// That is the whole difficulty of cross-entity search in a product like this: search
/// is where content leaks, because it is the one place that reaches into every table
/// at once, and a branch that forgets the filter discloses exactly the things the
/// audience rules exist to protect.
///
/// Deliberately LIKE against indexed columns rather than a full-text index. It is
/// honest about what it is - prefix and substring matching over titles and names -
/// and it needs no separate catalogue to provision or keep in sync. Full-text or an
/// external index is the right answer at a scale this does not have yet, and the
/// seam for it is this one class.
/// </summary>
public sealed class SearchQuery : IQuery<SearchResultsDto>
{
    public string Term { get; set; } = string.Empty;

    /// <summary>Results per category. Small on purpose - this is a jump-to, not a report.</summary>
    public int Take { get; set; } = 5;
}

public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResultsDto>
{
    /// <summary>Below this a term matches most of the company and is never a real search.</summary>
    public const int MinimumTermLength = 2;

    private const int MaxTake = 20;

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IAudienceResolver _audience;

    public SearchQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IAudienceResolver audience)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _audience = audience;
    }

    public async Task<SearchResultsDto> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        var term = request.Term?.Trim() ?? string.Empty;

        if (term.Length < MinimumTermLength)
        {
            return new SearchResultsDto { Term = term };
        }

        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);
        var keys = await _audience.ResolveKeysAsync(employeeId, cancellationToken);
        var take = Math.Clamp(request.Take, 1, MaxTake);

        // Escaped before it reaches LIKE. Without this a term containing % matches
        // everything and one containing _ matches any character - both from ordinary
        // user input, and both silently wrong rather than loud.
        var pattern = LikePattern.Contains(term);

        return new SearchResultsDto
        {
            Term = term,
            Employees = await SearchEmployeesAsync(pattern, take, cancellationToken),
            Posts = await SearchPostsAsync(pattern, keys, take, cancellationToken),
            Communities = await SearchCommunitiesAsync(pattern, employeeId, take, cancellationToken),
            Documents = await SearchDocumentsAsync(pattern, keys, take, cancellationToken),
            Events = await SearchEventsAsync(pattern, keys, take, cancellationToken),
        };
    }

    private async Task<IReadOnlyList<SearchResultDto>> SearchEmployeesAsync(
        string pattern,
        int take,
        CancellationToken cancellationToken)
    {
        // The staff directory is open to everybody who can read it at all, so there
        // is no audience filter here - only the active check.
        var rows = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Is_Active
                && (EF.Functions.Like(employee.Display_Name, pattern, LikePattern.EscapeCharacter)
                    || EF.Functions.Like(employee.Email, pattern, LikePattern.EscapeCharacter)
                    || (employee.Full_Name_Ar != null
                        && EF.Functions.Like(employee.Full_Name_Ar, pattern, LikePattern.EscapeCharacter))))
            .OrderBy(employee => employee.Display_Name)
            .Take(take)
            .Select(employee => new
            {
                employee.Id,
                employee.Display_Name,
                JobTitle = employee.JobTitle == null
                    ? null
                    : employee.JobTitle.Name_En ?? employee.JobTitle.Name_Ar,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new SearchResultDto
            {
                Kind = SearchResultKind.Employee,
                Id = row.Id,
                Title = row.Display_Name,
                Subtitle = row.JobTitle,
                Url = $"/employees/{row.Id:D}",
            }),
        ];
    }

    private async Task<IReadOnlyList<SearchResultDto>> SearchPostsAsync(
        string pattern,
        IReadOnlyList<string> keys,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<Post, Guid>()
            .GetAllQ()
            .Where(post => post.Status == PostStatus.Published)

            // Content_Text, not Content_Html: searching the markup matches on tag
            // names and attribute values, so a term like "span" would return the
            // whole feed.
            .Where(post =>
                (post.Title_En != null && EF.Functions.Like(post.Title_En, pattern, LikePattern.EscapeCharacter))
                || (post.Title_Ar != null && EF.Functions.Like(post.Title_Ar, pattern, LikePattern.EscapeCharacter))
                || (post.Content_Text != null && EF.Functions.Like(post.Content_Text, pattern, LikePattern.EscapeCharacter)))

            // The same audience filter as the feed. Without it, search becomes a way
            // to read every targeted announcement in the company.
            .Where(post => _unitOfWork.Repository<PostAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Post_Id == post.Id && keys.Contains(audience.Audience_Key)))
            .OrderByDescending(post => post.Published_Date)
            .Take(take)
            .Select(post => new
            {
                post.Id,
                post.Title_Ar,
                post.Title_En,
                post.Content_Text,
                post.Published_Date,
                Author = post.Author == null ? null : post.Author.Display_Name,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new SearchResultDto
            {
                Kind = SearchResultKind.Post,
                Id = row.Id,
                Title = LocalizedText.Pick(row.Title_Ar, row.Title_En)
                    ?? Snippet(row.Content_Text, 60),
                Subtitle = row.Author,
                Snippet = Snippet(row.Content_Text, 160),
                Url = $"/feed/{row.Id:D}",
                Date = row.Published_Date,
            }),
        ];
    }

    private async Task<IReadOnlyList<SearchResultDto>> SearchCommunitiesAsync(
        string pattern,
        Guid employeeId,
        int take,
        CancellationToken cancellationToken)
    {
        var joined = _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .Where(member => member.Employee_Id == employeeId
                && member.Membership_Status == MembershipStatus.Approved)
            .Select(member => member.Community_Id);

        var rows = await _unitOfWork.Repository<Community, Guid>()
            .GetAllQ()
            .Where(community => community.Is_Active)

            // A Private community must not surface in search for a non-member. This
            // is the same rule as the directory, and search is where it is easiest to
            // forget.
            .Where(community => community.Privacy != CommunityPrivacy.Private
                || joined.Contains(community.Id))
            .Where(community =>
                EF.Functions.Like(community.Name_Ar, pattern, LikePattern.EscapeCharacter)
                || (community.Name_En != null && EF.Functions.Like(community.Name_En, pattern, LikePattern.EscapeCharacter)))
            .OrderByDescending(community => community.Members_Count)
            .Take(take)
            .Select(community => new
            {
                community.Id,
                community.Name_Ar,
                community.Name_En,
                community.Members_Count,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new SearchResultDto
            {
                Kind = SearchResultKind.Community,
                Id = row.Id,
                Title = LocalizedText.Pick(row.Name_Ar, row.Name_En) ?? string.Empty,
                Subtitle = $"{row.Members_Count}",
                Url = $"/communities/{row.Id:D}",
            }),
        ];
    }

    private async Task<IReadOnlyList<SearchResultDto>> SearchDocumentsAsync(
        string pattern,
        IReadOnlyList<string> keys,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<Document, Guid>()
            .GetAllQ()
            .Where(document => document.Is_Published)
            .Where(document =>
                EF.Functions.Like(document.Title_Ar, pattern, LikePattern.EscapeCharacter)
                || (document.Title_En != null && EF.Functions.Like(document.Title_En, pattern, LikePattern.EscapeCharacter)))
            .Where(document => _unitOfWork.Repository<DocumentAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Document_Id == document.Id
                    && keys.Contains(audience.Audience_Key)))
            .OrderByDescending(document => document.Published_At)
            .Take(take)
            .Select(document => new
            {
                document.Id,
                document.Title_Ar,
                document.Title_En,
                document.Published_At,
                Category = document.Category == null
                    ? null
                    : document.Category.Name_En ?? document.Category.Name_Ar,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new SearchResultDto
            {
                Kind = SearchResultKind.Document,
                Id = row.Id,
                Title = LocalizedText.Pick(row.Title_Ar, row.Title_En) ?? string.Empty,
                Subtitle = row.Category,
                Url = "/documents",
                Date = row.Published_At,
            }),
        ];
    }

    private async Task<IReadOnlyList<SearchResultDto>> SearchEventsAsync(
        string pattern,
        IReadOnlyList<string> keys,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<Event, Guid>()
            .GetAllQ()
            .Where(candidate => candidate.Status == EventStatus.Published)
            .Where(candidate =>
                EF.Functions.Like(candidate.Title_Ar, pattern, LikePattern.EscapeCharacter)
                || (candidate.Title_En != null && EF.Functions.Like(candidate.Title_En, pattern, LikePattern.EscapeCharacter)))
            .Where(candidate => _unitOfWork.Repository<EventAudience, Guid>()
                .GetAllQ()
                .Any(audience => audience.Event_Id == candidate.Id
                    && keys.Contains(audience.Audience_Key)))
            .OrderBy(candidate => candidate.Start_At)
            .Take(take)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Title_Ar,
                candidate.Title_En,
                candidate.Start_At,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new SearchResultDto
            {
                Kind = SearchResultKind.Event,
                Id = row.Id,
                Title = LocalizedText.Pick(row.Title_Ar, row.Title_En) ?? string.Empty,
                Url = "/events",
                Date = row.Start_At,
            }),
        ];
    }

    private static string Snippet(string? text, int length)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();

        return trimmed.Length <= length
            ? trimmed
            : string.Concat(trimmed.AsSpan(0, length).TrimEnd(), "\u2026");
    }
}
