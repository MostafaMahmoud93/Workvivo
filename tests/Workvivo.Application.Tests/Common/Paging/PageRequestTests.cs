using Shouldly;
using Workvivo.Application.Common.Paging;
using Xunit;

namespace Workvivo.Application.Tests.Common.Paging;

public class PageRequestTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Page_number_below_one_falls_back_to_the_first_page(int requested)
    {
        new PageRequest { PageNumber = requested }.PageNumber.ShouldBe(1);
    }

    [Fact]
    public void Page_size_is_capped_rather_than_rejected()
    {
        // An unbounded page size is a cheap denial-of-service: one request asking for
        // a million rows. Clamping serves a sane page instead of a 400 that only tells
        // the caller which number to try next.
        new PageRequest { PageSize = 100_000 }.PageSize.ShouldBe(PageRequest.MaxPageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Nonsense_page_sizes_fall_back_to_the_default(int requested)
    {
        new PageRequest { PageSize = requested }.PageSize.ShouldBe(PageRequest.DefaultPageSize);
    }

    [Fact]
    public void Skip_is_derived_from_the_clamped_values()
    {
        var request = new PageRequest { PageNumber = 4, PageSize = 25 };

        request.Skip.ShouldBe(75);
    }
}
