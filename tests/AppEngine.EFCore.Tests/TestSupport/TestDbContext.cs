using AppEngine.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppEngine.EFCore.Tests.TestSupport;

public class Product : BaseSoftDelete<int>
{
    public string Name { get; set; } = string.Empty;
}

public class AuditedNote : BaseAuditable<int>
{
    public string Text { get; set; } = string.Empty;
}

public class Tag : BaseEntity<int>
{
    public string Label { get; set; } = string.Empty;
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditedNote> Notes => Set<AuditedNote>();
    public DbSet<Tag> Tags => Set<Tag>();
}
