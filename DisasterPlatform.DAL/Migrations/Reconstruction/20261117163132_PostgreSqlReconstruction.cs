using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Reconstruction
{
    /// <inheritdoc />
    public partial class PostgreSqlReconstruction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reconstruction");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "reconstruction",
                columns: table => new
                {
                    ConsumerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: "reconstruction.timeline"),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OutcomeJson = table.Column<string>(type: "text", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.ConsumerName, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedUnixMs = table.Column<long>(type: "bigint", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionAudit",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Detail = table.Column<string>(type: "text", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionFundingReservations",
                schema: "reconstruction",
                columns: table => new
                {
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionFundingReservations", x => x.DisasterId);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionProjects",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FinalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    FundingSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FundingSourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "donations"),
                    Responsible = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Contractor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormalizationSavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SignedContractReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    ContractSignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ContractRecordedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    BudgetLockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionProposals",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    Zone = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    AssetType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    AssessmentCount = table.Column<int>(type: "integer", nullable: false),
                    ProposedBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    EvaluationsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "'[]'"),
                    ReviewFingerprint = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSelectedForReconstruction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SelectedForProjectId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionTimeline",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionTimeline", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionWorkflow",
                schema: "reconstruction",
                columns: table => new
                {
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposalId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionWorkflow", x => new { x.Kind, x.SubjectId });
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionActivities",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Responsible = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconstructionActivities_ReconstructionProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "reconstruction",
                        principalTable: "ReconstructionProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReconstructionReports",
                schema: "reconstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActualCost = table.Column<decimal>(type: "numeric", nullable: false),
                    EvidenceJson = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "'[]'"),
                    DocumentsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "'[]'"),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconstructionReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconstructionReports_ReconstructionActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "reconstruction",
                        principalTable: "ReconstructionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SentAt_CreatedUnixMs",
                schema: "reconstruction",
                table: "OutboxMessages",
                columns: new[] { "SentAt", "CreatedUnixMs" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionActivities_ProjectId",
                schema: "reconstruction",
                table: "ReconstructionActivities",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionAudit_EventId",
                schema: "reconstruction",
                table: "ReconstructionAudit",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionAudit_ProjectId",
                schema: "reconstruction",
                table: "ReconstructionAudit",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionProjects_ProposalId",
                schema: "reconstruction",
                table: "ReconstructionProjects",
                column: "ProposalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionProposals_DisasterId_Zone_AssetType",
                schema: "reconstruction",
                table: "ReconstructionProposals",
                columns: new[] { "DisasterId", "Zone", "AssetType" },
                unique: true,
                filter: "\"Status\" = 0 AND \"IsSelectedForReconstruction\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionReports_ActivityId",
                schema: "reconstruction",
                table: "ReconstructionReports",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionReports_RequestId",
                schema: "reconstruction",
                table: "ReconstructionReports",
                column: "RequestId",
                unique: true,
                filter: "\"RequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionTimeline_EventId",
                schema: "reconstruction",
                table: "ReconstructionTimeline",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionTimeline_ProjectId",
                schema: "reconstruction",
                table: "ReconstructionTimeline",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionWorkflow_DisasterId_IsActive",
                schema: "reconstruction",
                table: "ReconstructionWorkflow",
                columns: new[] { "DisasterId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconstructionWorkflow_ProjectId",
                schema: "reconstruction",
                table: "ReconstructionWorkflow",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionAudit",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionFundingReservations",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionProposals",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionReports",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionTimeline",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionWorkflow",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionActivities",
                schema: "reconstruction");

            migrationBuilder.DropTable(
                name: "ReconstructionProjects",
                schema: "reconstruction");
        }
    }
}
