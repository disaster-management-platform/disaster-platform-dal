using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class ProtectDisastersReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_DisasterPhaseAudit_Desastres_DisasterId",
                schema: "disasters",
                table: "DisasterPhaseAudit",
                column: "DisasterId",
                principalSchema: "disasters",
                principalTable: "Desastres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisasterPhaseAudit_Desastres_DisasterId",
                schema: "disasters",
                table: "DisasterPhaseAudit");
        }
    }
}
