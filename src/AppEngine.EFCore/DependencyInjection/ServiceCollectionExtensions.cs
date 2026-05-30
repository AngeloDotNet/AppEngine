using AppEngine.EFCore.Entities.Interfaces;
using AppEngine.EFCore.Interceptors;
using AppEngine.EFCore.Repositories;
using AppEngine.EFCore.Repositories.Interfaces;
using AppEngine.EFCore.Services;
using AppEngine.EFCore.Services.Interfaces;
using AppEngine.EFCore.UoW;
using AppEngine.EFCore.UoW.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppEngine.EFCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPersistence<TEntity, TKey, TDbContext>()
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
        where TDbContext : DbContext
        {
            services.AddScoped<IRepository<TEntity, TKey>, Repository<TEntity, TKey, TDbContext>>();

            return services;
        }

        public IServiceCollection AddPersistenceUoW<TEntity, TKey, TDbContext>()
            where TEntity : class, IEntity<TKey>
            where TKey : IEquatable<TKey>
            where TDbContext : DbContext
        {
            services.AddScoped<IRepository<TEntity, TKey>, Repository<TEntity, TKey, TDbContext>>();
            services.AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();

            return services;
        }

        public IServiceCollection AddInterceptors()
        {
            services.AddScoped<AuditSaveChangesInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }

    extension(IHost app)
    {
        public void ApplyMigrations<TDbContext>() where TDbContext : DbContext
        {
            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            dbContext.Database.Migrate();
        }
    }
}