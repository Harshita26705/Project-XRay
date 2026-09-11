using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XRay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchAwareGraphAndProjectIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_OrganizationId_Name",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_GraphSnapshots_ProjectId",
                table: "GraphSnapshots");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "GraphSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Analyses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId_Name",
                table: "Projects",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_GraphSnapshots_BranchId",
                table: "GraphSnapshots",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphSnapshots_ProjectId_BranchId",
                table: "GraphSnapshots",
                columns: new[] { "ProjectId", "BranchId" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_BranchId",
                table: "Analyses",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_Branches_BranchId",
                table: "Analyses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphSnapshots_Branches_BranchId",
                table: "GraphSnapshots",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analyses_Branches_BranchId",
                table: "Analyses");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphSnapshots_Branches_BranchId",
                table: "GraphSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OrganizationId_Name",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_GraphSnapshots_BranchId",
                table: "GraphSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_GraphSnapshots_ProjectId_BranchId",
                table: "GraphSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Analyses_BranchId",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "GraphSnapshots");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Analyses");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId_Name",
                table: "Projects",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GraphSnapshots_ProjectId",
                table: "GraphSnapshots",
                column: "ProjectId");
        }
    }
}
