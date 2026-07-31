using Auth.Common.Data;

namespace Auth.Tests.Common;

public class QueryStringParametersTests
{
    [Fact]
    public void Defaults_StartOnTheFirstPageWithTenItems()
    {
        var parameters = new QueryStringParameters();

        Assert.Equal(1, parameters.Page);
        Assert.Equal(10, parameters.PageSize);
        Assert.Null(parameters.SortBy);
        Assert.False(parameters.SortAscending);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(999, 999)]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    [InlineData(int.MaxValue, 1000)]
    public void PageSize_IsCappedAtOneThousand(int requested, int expected)
    {
        var parameters = new QueryStringParameters { PageSize = requested };

        Assert.Equal(expected, parameters.PageSize);
    }
}
