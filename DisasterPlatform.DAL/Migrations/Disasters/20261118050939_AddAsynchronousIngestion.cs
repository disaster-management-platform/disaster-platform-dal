using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class AddAsynchronousIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "disasters",
                table: "DisasterOutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseId",
                schema: "disasters",
                table: "DisasterOutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseUntil",
                schema: "disasters",
                table: "DisasterOutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                schema: "disasters",
                table: "DisasterOutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisasterCommandReceipts",
                schema: "disasters",
                columns: table => new
                {
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterCommandReceipts", x => x.ClientRequestId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisasterCommandReceipts_ReceivedAt",
                schema: "disasters",
                table: "DisasterCommandReceipts",
                column: "ReceivedAt",
                filter: "\"ResultJson\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisasterCommandReceipts",
                schema: "disasters");

            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "disasters",
                table: "DisasterOutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseId",
                schema: "disasters",
                table: "DisasterOutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseUntil",
                schema: "disasters",
                table: "DisasterOutboxMessages");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                schema: "disasters",
                table: "DisasterOutboxMessages");
        }
    }
}
