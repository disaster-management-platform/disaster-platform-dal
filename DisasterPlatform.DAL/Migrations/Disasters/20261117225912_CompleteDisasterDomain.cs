using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class CompleteDisasterDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Changes",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CommitmentsConfirmed",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                type: "text",
                nullable: false,
                defaultValue: "PhaseChanged");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                type: "text",
                nullable: false,
                defaultValue: "Success");

            migrationBuilder.AddColumn<int>(
                name: "AttentionLevel",
                schema: "disasters",
                table: "Desastres",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreationSnapshot",
                schema: "disasters",
                table: "Desastres",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisasterTypeId",
                schema: "disasters",
                table: "Desastres",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegisteredAt",
                schema: "disasters",
                table: "Desastres",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: "disasters",
                table: "Desastres",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "disasters",
                table: "Desastres",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisasterTypes",
                schema: "disasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'7', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "disasters",
                table: "DisasterTypes",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Terremoto" },
                    { 2, true, "Inundación" },
                    { 3, true, "Incendio" },
                    { 4, true, "Deslizamiento" },
                    { 5, true, "Huracán" },
                    { 6, true, "Erupción volcánica" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Desastres_DisasterTypeId",
                schema: "disasters",
                table: "Desastres",
                column: "DisasterTypeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Desastres_AttentionLevel",
                schema: "disasters",
                table: "Desastres",
                sql: "\"AttentionLevel\" IS NULL OR \"AttentionLevel\" BETWEEN 1 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Desastres_Radio",
                schema: "disasters",
                table: "Desastres",
                sql: "\"Radio\" IS NULL OR (\"Radio\" > 0 AND \"Radio\" <= 1000000)");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterTypes_Name",
                schema: "disasters",
                table: "DisasterTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Desastres_DisasterTypes_DisasterTypeId",
                schema: "disasters",
                table: "Desastres",
                column: "DisasterTypeId",
                principalSchema: "disasters",
                principalTable: "DisasterTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                UPDATE disasters."Desastres" d SET "CreationSnapshot" = jsonb_build_object(
                    'Nombre', d."Nombre", 'Ubicacion', d."Ubicacion", 'Severidad', d."Severidad",
                    'Descripcion', d."Descripcion", 'Latitud', d."Latitud", 'Longitud', d."Longitud", 'Radio', d."Radio",
                    'DisasterTypeId', NULL, 'StartedAt', NULL, 'RegisteredAt', NULL, 'UpdatedAt', NULL, 'AttentionLevel', NULL,
                    'Version', 1, 'Phase', CASE WHEN d."Version" = 1 THEN d."Phase" ELSE
                        (SELECT a."Before" FROM disasters."DisasterPhaseAudit" a WHERE a."DisasterId" = d."Id" AND a."Version" = 2) END)
                WHERE d."ClientRequestId" IS NOT NULL AND (d."Version" = 1 OR EXISTS
                    (SELECT 1 FROM disasters."DisasterPhaseAudit" a WHERE a."DisasterId" = d."Id" AND a."Version" = 2));
                UPDATE disasters."Desastres" d SET "UpdatedAt" = a."OccurredAt"
                FROM disasters."DisasterPhaseAudit" a WHERE a."DisasterId" = d."Id" AND a."Version" = d."Version";
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Desastres_DisasterTypes_DisasterTypeId",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropTable(
                name: "DisasterTypes",
                schema: "disasters");

            migrationBuilder.DropIndex(
                name: "IX_Desastres_DisasterTypeId",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Desastres_AttentionLevel",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Desastres_Radio",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "Changes",
                schema: "disasters",
                table: "DisasterPhaseAudit");

            migrationBuilder.DropColumn(
                name: "CommitmentsConfirmed",
                schema: "disasters",
                table: "DisasterPhaseAudit");

            migrationBuilder.DropColumn(
                name: "Operation",
                schema: "disasters",
                table: "DisasterPhaseAudit");

            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "disasters",
                table: "DisasterPhaseAudit");

            migrationBuilder.DropColumn(
                name: "Result",
                schema: "disasters",
                table: "DisasterPhaseAudit");

            migrationBuilder.DropColumn(
                name: "AttentionLevel",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "CreationSnapshot",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "DisasterTypeId",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "disasters",
                table: "Desastres");
        }
    }
}
