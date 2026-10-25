using AppEngine.EFCore.DependencyInjection;
using AppEngine.EFCore.Interceptors;
using AppEngine.EFCore.Repositories;
using AppEngine.EFCore.Repositories.Interfaces;
using AppEngine.EFCore.Services.Interfaces;
using AppEngine.EFCore.Tests.TestSupport;
using AppEngine.EFCore.UoW;
using AppEngine.EFCore.UoW.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.EFCore.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    private static ServiceCollection WithDbContext()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseSqlite("DataSource=:memory:"));
        return services;
    }

    [Fact]
    public void AddPersistence_RegistersScopedRepository()
    {
        var services = WithDbContext();
        services.AddPersistence<Tag, int, TestDbContext>();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IRepository<Tag, int>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(Repository<Tag, int, TestDbContext>), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPersistenceUoW_RegistersRepositoryAndUnitOfWork()
    {
        var services = WithDbContext();
        services.AddPersistenceUoW<Tag, int, TestDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<Repository<Tag, int, TestDbContext>>(scope.ServiceProvider.GetRequiredService<IRepository<Tag, int>>());
        Assert.IsType<UnitOfWork<TestDbContext>>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void Current_AddPersistenceUoW_CalledTwice_RegistersUnitOfWorkTwice()
    {
        var services = WithDbContext();
        services.AddPersistenceUoW<Tag, int, TestDbContext>();
        services.AddPersistenceUoW<Product, int, TestDbContext>();

        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IUnitOfWork)));
    }

    [Fact]
    public void AddInterceptors_RegistersInterceptorsAndCurrentUser()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddInterceptors();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SoftDeleteInterceptor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserService>());
    }
}
