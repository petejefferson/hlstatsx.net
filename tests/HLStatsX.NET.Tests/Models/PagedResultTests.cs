using HLStatsX.NET.Core.Models;

namespace HLStatsX.NET.Tests.Models;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUp_WhenItemsDoNotDivideEvenly()
    {
        var result = PagedResult<string>.Create(["a"], 51, 1, 50);

        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void TotalPages_IsOne_WhenCountEqualsPageSize()
    {
        var result = PagedResult<string>.Create([], 50, 1, 50);

        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public void TotalPages_IsZero_WhenTotalCountIsZero()
    {
        var result = PagedResult<string>.Create([], 0, 1, 50);

        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void HasPreviousPage_IsFalse_OnFirstPage()
    {
        var result = PagedResult<string>.Create([], 100, 1, 10);

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_IsTrue_OnSecondPage()
    {
        var result = PagedResult<string>.Create([], 100, 2, 10);

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_IsFalse_OnLastPage()
    {
        var result = PagedResult<string>.Create([], 20, 2, 10);

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_IsTrue_WhenMorePagesExist()
    {
        var result = PagedResult<string>.Create([], 21, 1, 10);

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void Create_SetsAllProperties()
    {
        var items = new List<string> { "x", "y" };
        var result = PagedResult<string>.Create(items, 42, 3, 15);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(42);
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(15);
    }
}

public class PaginationModelTests
{
    [Fact]
    public void From_MapsPageAndTotalPages()
    {
        var paged = PagedResult<string>.Create([], 75, 2, 25);
        var model = PaginationModel.From(paged, p => $"/items?page={p}");

        model.CurrentPage.Should().Be(2);
        model.TotalPages.Should().Be(3);
    }

    [Fact]
    public void From_BuildUrl_GeneratesCorrectUrl()
    {
        var paged = PagedResult<string>.Create([], 10, 1, 10);
        var model = PaginationModel.From(paged, p => $"/foo?page={p}");

        model.BuildUrl(5).Should().Be("/foo?page=5");
    }
}
