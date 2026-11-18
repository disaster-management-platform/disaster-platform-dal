using DisasterLocation.Core;
using Microsoft.EntityFrameworkCore;

namespace Platform.DAL.Contexts;

public sealed class DisasterDbContext(DbContextOptions<DisasterDbContext> options) : DbContext(options)
{
    public DbSet<Platform.DAL.Models.DisasterCommandReceipt> Commands => Set<Platform.DAL.Models.DisasterCommandReceipt>();
    public DbSet<DisasterReport> Reports => Set<DisasterReport>();
    public DbSet<Platform.DAL.Models.DisasterOutboxMessage> Outbox => Set<Platform.DAL.Models.DisasterOutboxMessage>();
    public DbSet<DisasterType> DisasterTypes => Set<DisasterType>();
    public DbSet<Disaster> Disasters => Set<Disaster>();
    public DbSet<Platform.DAL.Models.DisasterPhaseAudit> PhaseAudit => Set<Platform.DAL.Models.DisasterPhaseAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PlatformDatabase.DisasterSchema);
        modelBuilder.Entity<DisasterType>(b =>
        {
            b.ToTable("DisasterTypes"); b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasIdentityOptions(startValue: 7);
            b.Property(x => x.Name).HasMaxLength(120).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(new DisasterType { Id = 1, Name = "Terremoto" }, new DisasterType { Id = 2, Name = "Inundación" },
                new DisasterType { Id = 3, Name = "Incendio" }, new DisasterType { Id = 4, Name = "Deslizamiento" },
                new DisasterType { Id = 5, Name = "Huracán" }, new DisasterType { Id = 6, Name = "Erupción volcánica" });
        });
        modelBuilder.Entity<Platform.DAL.Models.DisasterCommandReceipt>(b =>
        {
            b.ToTable("DisasterCommandReceipts"); b.HasKey(x => x.ClientRequestId);
            b.Property(x => x.CommandJson).HasColumnType("jsonb").Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
            b.Property(x => x.ResultJson).HasColumnType("jsonb");
            b.HasIndex(x => x.ReceivedAt).HasFilter("\"ResultJson\" IS NULL");
        });
        modelBuilder.Entity<DisasterReport>(b =>
        {
            b.ToTable("DisasterReports"); b.HasKey(x => x.Id);
            b.Property(x => x.SourceId).HasMaxLength(200);
            b.Property(x => x.SourceReportId).HasMaxLength(200);
            b.Property(x => x.ReceivedBy).HasMaxLength(300);
            b.Property(x => x.RegisteringOrganizationId).HasMaxLength(200);
            b.Property(x => x.OriginalContent).HasColumnType("text");
            b.Property(x => x.ReceiptSnapshot).HasColumnType("jsonb");
            b.HasIndex(x => new { x.SourceId, x.SourceReportId }).IsUnique();
            b.HasOne<Disaster>().WithMany().HasForeignKey(x => x.DisasterId).OnDelete(DeleteBehavior.Restrict);
            foreach (var property in b.Metadata.GetProperties().Where(x => x.Name != nameof(DisasterReport.DisasterId)))
                property.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        });
        modelBuilder.Entity<Platform.DAL.Models.DisasterOutboxMessage>(b =>
        {
            b.ToTable("DisasterOutboxMessages"); b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(100);
            b.Property(x => x.Payload).HasColumnType("jsonb");
            b.HasIndex(x => new { x.DisasterId, x.Version, x.EventType }).IsUnique();
            b.HasOne<Disaster>().WithMany().HasForeignKey(x => x.DisasterId).OnDelete(DeleteBehavior.Restrict);
        });
        var disaster = modelBuilder.Entity<Disaster>();
        disaster.Property(x => x.RegisteringOrganizationId).HasMaxLength(200);
        disaster.Property(x => x.EmergencyResponsibleId).HasMaxLength(200);
        disaster.Property(x => x.GeographyJson).HasColumnType("jsonb");
        disaster.Property(x => x.OrganizationsJson).HasColumnType("jsonb");
        disaster.HasOne<DisasterType>().WithMany().HasForeignKey(x => x.DisasterTypeId).OnDelete(DeleteBehavior.Restrict);
        disaster.Property(x => x.CreationSnapshot).HasColumnType("jsonb").Metadata
            .SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        disaster.ToTable("Desastres", table =>
        {
            table.HasCheckConstraint("CK_Desastres_AttentionLevel", "\"AttentionLevel\" IS NULL OR \"AttentionLevel\" BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_Desastres_Radio", "\"Radio\" IS NULL OR (\"Radio\" > 0 AND \"Radio\" <= 1000000)");
            table.HasCheckConstraint("CK_Desastres_Severidad", "\"Severidad\" BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_Desastres_Latitud", "\"Latitud\" BETWEEN -90 AND 90");
            table.HasCheckConstraint("CK_Desastres_Longitud", "\"Longitud\" BETWEEN -180 AND 180");
        });
        modelBuilder.Entity<Platform.DAL.Models.DisasterPhaseAudit>(b =>
        {
            b.ToTable("DisasterPhaseAudit"); b.HasKey(x => x.Id);
            b.Property(x => x.Operation).HasDefaultValue("PhaseChanged");
            b.Property(x => x.Result).HasDefaultValue("Success");
            b.Property(x => x.Changes).HasColumnType("jsonb");
            b.HasIndex(x => new { x.DisasterId, x.Version }).IsUnique();
            b.HasOne<Disaster>().WithMany().HasForeignKey(x => x.DisasterId).OnDelete(DeleteBehavior.Restrict);
        });
        disaster.Property(d => d.Phase).HasDefaultValue(DisasterPhase.Registered);
        disaster.Property(d => d.Version).HasDefaultValue(1L).IsConcurrencyToken();
        disaster.Property<long>("SyncSequence").ValueGeneratedOnAddOrUpdate();
        disaster.HasIndex("SyncSequence").IsUnique();
        disaster.HasKey(d => d.Id);
        disaster.HasIndex(d => d.ClientRequestId).IsUnique();
        disaster.Property(d => d.Nombre).IsRequired().HasMaxLength(120);
        disaster.Property(d => d.Ubicacion).IsRequired().HasMaxLength(200);
        disaster.Property(d => d.Descripcion).IsRequired().HasMaxLength(2000);
    }
}
