using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XRay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationConnectionPersonalAccessToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonalAccessToken",
                table: "IntegrationConnections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalAccessToken",
                table: "IntegrationConnections");
        }
    }
}
