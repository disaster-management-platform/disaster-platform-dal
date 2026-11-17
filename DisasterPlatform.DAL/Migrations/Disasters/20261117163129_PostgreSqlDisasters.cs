using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class PostgreSqlDisasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "disasters");

            migrationBuilder.CreateTable(
                name: "Desastres",
                schema: "disasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severidad = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: false),
                    Longitud = table.Column<double>(type: "double precision", nullable: false),
                    Radio = table.Column<double>(type: "double precision", nullable: true),
                    Phase = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Desastres", x => x.Id);
                    table.CheckConstraint("CK_Desastres_Latitud", "\"Latitud\" BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_Desastres_Longitud", "\"Longitud\" BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_Desastres_Severidad", "\"Severidad\" BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "DisasterPhaseAudit",
                schema: "disasters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterId = table.Column<int>(type: "integer", nullable: false),
                    Before = table.Column<int>(type: "integer", nullable: false),
                    After = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterPhaseAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Desastres_ClientRequestId",
                schema: "disasters",
                table: "Desastres",
                column: "ClientRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisasterPhaseAudit_DisasterId_Version",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                columns: new[] { "DisasterId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Desastres",
                schema: "disasters");

            migrationBuilder.DropTable(
                name: "DisasterPhaseAudit",
                schema: "disasters");
        }
    }
}
