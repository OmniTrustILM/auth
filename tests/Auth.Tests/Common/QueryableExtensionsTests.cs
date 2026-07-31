using Auth.Common.Extensions;

namespace Auth.Tests.Common;

public class QueryableExtensionsTests
{
    private sealed record Row(string Name, int Rank);

    private static readonly IQueryable<Row> Rows = new[]
    {
        new Row("beta", 2),
        new Row("alpha", 2),
        new Row("gamma", 1),
    }.AsQueryable();

    [Fact]
    public void WhereIf_AppliesTheBranchOnlyWhenTheConditionHolds()
    {
        Assert.Single(Rows.WhereIf(true, q => q.Where(r => r.Rank == 1)));
        Assert.Equal(3, Rows.WhereIf(false, q => q.Where(r => r.Rank == 1)).Count());
    }

    [Fact]
    public void IncludeIf_AppliesTheBranchOnlyWhenTheConditionHolds()
    {
        Assert.Single(Rows.IncludeIf(true, q => q.Where(r => r.Rank == 1)));
        Assert.Equal(3, Rows.IncludeIf(false, q => q.Where(r => r.Rank == 1)).Count());
    }

    [Fact]
    public void OrderBy_SortsByThePropertyNamedAtRuntime()
    {
        Assert.Equal(["alpha", "beta", "gamma"], Rows.OrderBy(nameof(Row.Name)).Select(r => r.Name));
        Assert.Equal(["gamma", "beta", "alpha"], Rows.OrderBy(nameof(Row.Name), ascending: false).Select(r => r.Name));
    }

    [Fact]
    public void OrderByDescending_SortsDescendingOnlyWhenItsAscendingFlagIsCleared()
    {
        // The flag, not the method name, decides the direction - both OrderBy overloads read it the same way.
        Assert.Equal(["alpha", "beta", "gamma"], Rows.OrderByDescending(nameof(Row.Name)).Select(r => r.Name));
        Assert.Equal(["gamma", "beta", "alpha"], Rows.OrderByDescending(nameof(Row.Name), ascending: false).Select(r => r.Name));
    }

    [Fact]
    public void ThenBy_BreaksTiesOfThePrimarySort()
    {
        var ordered = Rows.OrderBy(nameof(Row.Rank)).ThenBy(nameof(Row.Name));

        Assert.Equal(["gamma", "alpha", "beta"], ordered.Select(r => r.Name));
    }

    [Fact]
    public void ThenBy_BreaksTiesDescending_WhenItsAscendingFlagIsCleared()
    {
        var ordered = Rows.OrderBy(nameof(Row.Rank)).ThenBy(nameof(Row.Name), ascending: false);

        Assert.Equal(["gamma", "beta", "alpha"], ordered.Select(r => r.Name));
    }

    [Fact]
    public void ThenByDescending_ReadsItsAscendingFlagTheSameWay()
    {
        Assert.Equal(["gamma", "alpha", "beta"], Rows.OrderBy(nameof(Row.Rank)).ThenByDescending(nameof(Row.Name)).Select(r => r.Name));
        Assert.Equal(["gamma", "beta", "alpha"], Rows.OrderBy(nameof(Row.Rank)).ThenByDescending(nameof(Row.Name), ascending: false).Select(r => r.Name));
    }

    [Fact]
    public void OrderBy_ThrowsWhenTheNameIsNotAProperty()
    {
        Assert.Throws<ArgumentException>(() => Rows.OrderBy("NoSuchProperty").ToList());
    }
}
