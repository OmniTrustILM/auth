using Auth.Common.Data;

namespace Auth.Tests.Common;

public class PagedListTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void TotalPages_RoundsUp(int count, int pageSize, int expectedTotalPages)
    {
        var page = new PagedList<int>([], count, 1, pageSize);

        Assert.Equal(expectedTotalPages, page.TotalPages);
    }

    [Fact]
    public void Constructor_KeepsItemsAndPagingState()
    {
        var page = new PagedList<string>(["a", "b"], 5, 2, 2);

        Assert.Equal(["a", "b"], page);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.CurrentPage);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalPages);
    }

    [Theory]
    [InlineData(1, false, true)]
    [InlineData(2, true, true)]
    [InlineData(3, true, false)]
    public void HasPreviousAndHasNext_FollowCurrentPagePosition(int currentPage, bool hasPrevious, bool hasNext)
    {
        var page = new PagedList<int>([], 5, currentPage, 2);

        Assert.Equal(hasPrevious, page.HasPrevious);
        Assert.Equal(hasNext, page.HasNext);
    }

    [Fact]
    public void CreateFromFullList_TakesTheRequestedWindowAndReportsTheFullCount()
    {
        var page = PagedList<int>.CreateFromFullList([1, 2, 3, 4, 5], 2, 2);

        Assert.Equal([3, 4], page);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.CurrentPage);
    }

    [Fact]
    public void CreateFromFullList_YieldsAnEmptyWindowPastTheEnd()
    {
        var page = PagedList<int>.CreateFromFullList([1, 2, 3], 9, 2);

        Assert.Empty(page);
        Assert.Equal(3, page.TotalCount);
    }
}
