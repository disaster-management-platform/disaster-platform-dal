using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Reconstruction
{
    /// <inheritdoc />
    public partial class ProtectReconstructionReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ReconstructionProjects_ReconstructionProposals_ProposalId",
                schema: "reconstruction",
                table: "ReconstructionProjects",
                column: "ProposalId",
                principalSchema: "reconstruction",
                principalTable: "ReconstructionProposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReconstructionProjects_ReconstructionProposals_ProposalId",
                schema: "reconstruction",
                table: "ReconstructionProjects");
        }
    }
}
