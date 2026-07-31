using Auth.Common.Data;
using Auth.Common.Exceptions;
using Auth.Common.Mappings;
using Auth.Common.Models.Dto;

namespace Auth.Tests.Common;

public class CommonMapperTests
{
    [Fact]
    public void ToQueryStringParameters_CarriesPagingThrough()
    {
        var parameters = new QueryRequestDto { Page = 3, PageSize = 25, SortBy = null }.ToQueryStringParameters();

        Assert.Equal(3, parameters.Page);
        Assert.Equal(25, parameters.PageSize);
    }

    [Fact]
    public void ToQueryStringParameters_LetsTheParametersCapAnOversizedPageSize()
    {
        var parameters = new QueryRequestDto { PageSize = 5000 }.ToQueryStringParameters();

        Assert.Equal(1000, parameters.PageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToQueryStringParameters_TreatsAnAbsentSortAsNoOrdering(string? sortBy)
    {
        var parameters = new QueryRequestDto { SortBy = sortBy }.ToQueryStringParameters();

        Assert.Null(parameters.SortBy);
        Assert.True(parameters.SortAscending);
    }

    [Theory]
    [InlineData("uuid", "Uuid")]
    [InlineData("username", "Username")]
    [InlineData("Username", "Username")]
    public void ToQueryStringParameters_CapitalizesTheFieldForTheClrPropertyLookup(string sortBy, string expected)
    {
        var parameters = new QueryRequestDto { SortBy = sortBy }.ToQueryStringParameters();

        Assert.Equal(expected, parameters.SortBy);
        Assert.True(parameters.SortAscending);
    }

    [Theory]
    [InlineData("-username", "Username")]
    [InlineData("-uuid", "Uuid")]
    public void ToQueryStringParameters_ReadsALeadingMinusAsDescending(string sortBy, string expected)
    {
        var parameters = new QueryRequestDto { SortBy = sortBy }.ToQueryStringParameters();

        Assert.Equal(expected, parameters.SortBy);
        Assert.False(parameters.SortAscending);
    }

    [Theory]
    [InlineData("-")]
    [InlineData("-   ")]
    public void ToQueryStringParameters_RejectsADescendingPrefixWithNoField(string sortBy)
    {
        var exception = Assert.Throws<InvalidFormatException>(() => new QueryRequestDto { SortBy = sortBy }.ToQueryStringParameters());

        Assert.Equal("INVALID_FORMAT", exception.Code);
    }

    [Fact]
    public void ToQueryStringParameters_StripsLineEndingsFromTheRejectionMessage()
    {
        var exception = Assert.Throws<InvalidFormatException>(() => new QueryRequestDto { SortBy = "-\r\n" }.ToQueryStringParameters());

        Assert.DoesNotContain('\n', exception.Message);
        Assert.DoesNotContain('\r', exception.Message);
    }

    [Fact]
    public void ToPagingMetadata_CopiesEveryPagingField()
    {
        var metadata = new PagedList<int>([1, 2], 5, 2, 2).ToPagingMetadata();

        Assert.Equal(2, metadata.CurrentPage);
        Assert.Equal(2, metadata.PageSize);
        Assert.Equal(5, metadata.TotalCount);
        Assert.Equal(3, metadata.TotalPages);
        Assert.True(metadata.HasPrevious);
        Assert.True(metadata.HasNext);
    }
}
