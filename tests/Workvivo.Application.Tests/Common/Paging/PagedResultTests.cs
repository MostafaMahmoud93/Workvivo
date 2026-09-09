using Shouldly;
using Workvivo.Application.Common.Paging;
using Xunit;

namespace Workvivo.Application.Tests.Common.Paging;

public class PagedResultTests
{
    [Fact]
    public void Total_pages_rounds_up_for_a_partial_last_page()
    {
        // 250 items at 20 per page is 13 pages, not 12 - the example in the brief.
        new PagedResult<int>([], 1, 20, 250).TotalPages.ShouldBe(13);
    }

    [Fact]
    public void Navigation_flags_reflect_position_in_the_set()
    {
        var first = new PagedResult<int>([1], 1, 20, 250);
        var last = new PagedResult<int>([1], 13, 20, 250);

        first.HasPrevious.ShouldBeFalse();
        first.HasNext.ShouldBeTrue();
        last.HasPrevious.ShouldBeTrue();
        last.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void An_empty_result_reports_no_pages_and_no_navigation()
    {
        var empty = PagedResult<int>.Empty(1, 20);

        empty.TotalPages.ShouldBe(0);
        empty.HasNext.ShouldBeFalse();
        empty.HasPrevious.ShouldBeFalse();
    }

    [Fact]
    public void Zero_page_size_does_not_divide_by_zero()
    {
        new PagedResult<int>([], 1, 0, 10).TotalPages.ShouldBe(0);
    }

    [Fact]
    public void Map_projects_items_and_keeps_the_paging_metadata()
    {
        var mapped = new PagedResult<int>([1, 2, 3], 2, 3, 9).Map(i => i.ToString());

        mapped.Items.ShouldBe(["1", "2", "3"]);
        mapped.PageNumber.ShouldBe(2);
        mapped.TotalCount.ShouldBe(9);
    }
}
