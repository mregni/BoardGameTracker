using System;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Datastore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameTracker.Tests.Support;

public sealed class IdentityTestScope : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public IdentityTestScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<MainDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MainDbContext>();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    public UserManager<ApplicationUser> UserManager => _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    public RoleManager<IdentityRole> RoleManager => _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
