using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.DAL;

public static class PlatformDatabase
{
    public const string DisasterSchema = "disasters";
    public const string ReconstructionSchema = "reconstruction";
    public const string DisasterHistory = "__EFMigrationsHistory_Disaster";
    public const string ReconstructionHistory = "__EFMigrationsHistory_Reconstruction";

    public static string ResolveConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Configure ConnectionStrings:Disasters para PostgreSQL.", nameof(connectionString));
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host) || string.IsNullOrWhiteSpace(builder.Database))
            throw new ArgumentException("Configure el host y la base PostgreSQL.", nameof(connectionString));
        if (!builder.Host.StartsWith('/'))
        {
            if (builder.ContainsKey("SSL Mode") && builder.SslMode != SslMode.VerifyFull)
                throw new ArgumentException("La conexión PostgreSQL requiere SSL Mode=VerifyFull.", nameof(connectionString));
            builder.SslMode = SslMode.VerifyFull;
        }
        builder.IncludeErrorDetail = false;
        builder.PersistSecurityInfo = false;
        builder.Timezone = "UTC";
        return builder.ToString();
    }

    public static async Task MigrateAsync(string connectionString, CancellationToken ct = default)
    {
        await using var services = new ServiceCollection().AddPlatformDal(connectionString).BuildServiceProvider();
        await services.InitializePlatformDalAsync(ct);
    }

    internal static DateTimeOffset Timestamp(DateTimeOffset value)
    {
        value = value.ToUniversalTime();
        return new DateTimeOffset(value.Ticks - value.Ticks % 10, TimeSpan.Zero);
    }

    public static bool IsPersistenceFailure(Exception exception) =>
        exception is DbUpdateException or NpgsqlException or TimeoutException;

    public static bool IsConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation,
            SchemaName: ReconstructionSchema } error && error.ConstraintName is
            "IX_ReconstructionProposals_DisasterId_Zone_AssetType" or "IX_ReconstructionProjects_ProposalId"
            or "IX_ReconstructionReports_RequestId" or "PK_ReconstructionFundingReservations"
            or "PK_InboxMessages" or "IX_ReconstructionAudit_EventId" or "IX_ReconstructionTimeline_EventId";
}
