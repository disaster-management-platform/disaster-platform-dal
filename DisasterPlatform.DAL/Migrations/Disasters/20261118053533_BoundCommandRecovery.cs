using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class BoundCommandRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRecoveryAt",
                schema: "disasters",
                table: "DisasterCommandReceipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoveryCount",
                schema: "disasters",
                table: "DisasterCommandReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextRecoveryAt",
                schema: "disasters",
                table: "DisasterCommandReceipts");

            migrationBuilder.DropColumn(
                name: "RecoveryCount",
                schema: "disasters",
                table: "DisasterCommandReceipts");
        }
    }
}
