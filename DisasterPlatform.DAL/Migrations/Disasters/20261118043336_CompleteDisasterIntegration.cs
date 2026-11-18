using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class CompleteDisasterIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmergencyResponsibleId",
                schema: "disasters",
                table: "Desastres",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographyJson",
                schema: "disasters",
                table: "Desastres",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationsJson",
                schema: "disasters",
                table: "Desastres",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteringOrganizationId",
                schema: "disasters",
                table: "Desastres",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisasterOutboxMessages",
                schema: "disasters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterOutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisasterOutboxMessages_Desastres_DisasterId",
                        column: x => x.DisasterId,
                        principalSchema: "disasters",
                        principalTable: "Desastres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisasterReports",
                schema: "disasters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKind = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceReportId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RegisteringOrganizationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OriginalContent = table.Column<string>(type: "text", nullable: false),
                    ReceiptSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisasterReports_Desastres_DisasterId",
                        column: x => x.DisasterId,
                        principalSchema: "disasters",
                        principalTable: "Desastres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisasterOutboxMessages_DisasterId_Version_EventType",
                schema: "disasters",
                table: "DisasterOutboxMessages",
                columns: new[] { "DisasterId", "Version", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisasterReports_DisasterId",
                schema: "disasters",
                table: "DisasterReports",
                column: "DisasterId");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterReports_SourceId_SourceReportId",
                schema: "disasters",
                table: "DisasterReports",
                columns: new[] { "SourceId", "SourceReportId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisasterOutboxMessages",
                schema: "disasters");

            migrationBuilder.DropTable(
                name: "DisasterReports",
                schema: "disasters");

            migrationBuilder.DropColumn(
                name: "EmergencyResponsibleId",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "GeographyJson",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "OrganizationsJson",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "RegisteringOrganizationId",
                schema: "disasters",
                table: "Desastres");
        }
    }
}
