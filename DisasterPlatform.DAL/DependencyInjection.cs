using DisasterLocation.DAL;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.DAL.Contexts;
using Platform.DAL.Messaging;
using Platform.DAL.Repositories.Reconstruction;
using Platform.DAL.Transactions;
using Reconstruction.Application.Ports;

namespace Platform.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformDal(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccess(connectionString);
        return services.AddReconstructionDataAccess(connectionString);
    }

    public static IServiceCollection AddReconstructionDataAccess(this IServiceCollection services, string connectionString)
    {
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<IDisasterAccess, UnrestrictedDisasterAccess>(services);
        services.AddDbContext<ReconstructionDbContext>(options => options.UseNpgsql(
            PlatformDatabase.ResolveConnectionString(connectionString),
            postgres => postgres.MigrationsHistoryTable(PlatformDatabase.ReconstructionHistory, PlatformDatabase.ReconstructionSchema)));
        services.AddScoped<IProposalRepository, ProposalRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IFundingReservationRepository, FundingReservationRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowReplaySource, WorkflowReplaySource>();
        services.AddScoped<IReconstructionHistoryReader, HistoryReader>();
        services.AddScoped<IReconstructionUnitOfWork, ReconstructionUnitOfWork>();
        services.AddScoped<IConsumerInbox, ConsumerInbox>();
        services.AddScoped<ITimelineWriter, TimelineWriter>();
        return services;
    }

    public static async Task InitializePlatformDalAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var disasters = scope.ServiceProvider.GetRequiredService<DisasterDbContext>();
        var reconstruction = scope.ServiceProvider.GetRequiredService<ReconstructionDbContext>();
        await using var connection = new NpgsqlConnection(disasters.Database.GetConnectionString());
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(1761127341)", connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
        await disasters.Database.MigrateAsync(ct);
        await reconstruction.Database.MigrateAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
