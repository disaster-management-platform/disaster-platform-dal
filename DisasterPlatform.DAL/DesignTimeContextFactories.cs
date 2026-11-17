using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Platform.DAL.Contexts;

namespace Platform.DAL;

// No API or broker is started by EF tools.
public sealed class DisasterContextFactory : IDesignTimeDbContextFactory<DisasterDbContext>
{
    public DisasterDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<DisasterDbContext>().UseNpgsql(DesignTimeDatabase.ConnectionString(),
            postgres => postgres.MigrationsHistoryTable(PlatformDatabase.DisasterHistory, PlatformDatabase.DisasterSchema)).Options);
}

public sealed class ReconstructionContextFactory : IDesignTimeDbContextFactory<ReconstructionDbContext>
{
    public ReconstructionDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<ReconstructionDbContext>().UseNpgsql(DesignTimeDatabase.ConnectionString(),
            postgres => postgres.MigrationsHistoryTable(PlatformDatabase.ReconstructionHistory, PlatformDatabase.ReconstructionSchema)).Options);
}

internal static class DesignTimeDatabase
{
    internal static string ConnectionString() => PlatformDatabase.ResolveConnectionString(
        Environment.GetEnvironmentVariable("ConnectionStrings__Disasters")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__Disasters para las herramientas de migración."));
}
