using AppEngine.EFCore.Interceptors;
using AppEngine.EFCore.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AppEngine.EFCore.Tests.TestSupport;

public sealed class FakeCurrentUser(string? userName) : ICurrentUserService
{
    public string? UserName { get; set; } = userName;
}

/// <summary>SQLite in-memory database kept alive by an open connection.</summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public FakeCurrentUser User { get; }

    public DbContextOptions<TestDbContext> Options { get; }

    public TestDatabase(string? userName = "tester", bool withInterceptors = true)
    {
        connection.Open();
        User = new FakeCurrentUser(userName);

        var builder = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection);
        if (withInterceptors)
        {
            builder.AddInterceptors(new AuditSaveChangesInterceptor(User), new SoftDeleteInterceptor(User));
        }

        Options = builder.Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TestDbContext CreateContext() => new(Options);

    public void Dispose() => connection.Dispose();
}
