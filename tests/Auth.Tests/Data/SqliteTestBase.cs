using Auth.Data;
using Auth.Tests.TestSupport;

namespace Auth.Tests.Data;

public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteAuthDb _db = new();

    protected AuthDbContext NewContext() => _db.NewContext();

    protected async Task Seed(Func<AuthDbContext, Task> seed)
    {
        await using var context = NewContext();
        await seed(context);
        await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
