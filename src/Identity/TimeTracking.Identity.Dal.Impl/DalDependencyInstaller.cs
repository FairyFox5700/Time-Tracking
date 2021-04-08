using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Dal.Impl.Repositories;
using TimeTracking.Identity.Entities;

namespace TimeTracking.Identity.Dal.Impl
{
    public static class DalDependencyInstaller
    {
        public static IServiceCollection AddDalServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<TimeTrackingIdentityDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(TimeTrackingIdentityDbContext).Assembly.FullName)));

            services.AddIdentity<User, Role>(options =>
                     options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<TimeTrackingIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.AddTransient<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            return services;
        }
    }
}