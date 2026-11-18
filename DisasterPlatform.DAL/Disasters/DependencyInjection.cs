using DisasterLocation.Core;
using Platform.DAL;
using Platform.DAL.Contexts;
using Platform.DAL.Repositories.Disasters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DisasterLocation.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DisasterDbContext>(options => options.UseNpgsql(
            PlatformDatabase.ResolveConnectionString(connectionString),
            postgres => postgres.MigrationsHistoryTable(PlatformDatabase.DisasterHistory, PlatformDatabase.DisasterSchema)));
        services.AddScoped<DisasterCommandStore>();
        services.AddScoped<IDisasterRepository, DisasterRepository>();
        return services;
    }
}
