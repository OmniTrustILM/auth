using Auth.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.TestSupport;

/// <summary>
/// A real <see cref="AuthDbContext"/> over a private in-memory SQLite database. The connection is held open for the
/// lifetime of the instance because SQLite drops an in-memory database as soon as its last connection closes, and it is
/// shared by every context handed out so a test can write through one context and read through another.
/// </summary>
public sealed class SqliteAuthDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteAuthDb()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    public AuthDbContext NewContext()
        => new(new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
