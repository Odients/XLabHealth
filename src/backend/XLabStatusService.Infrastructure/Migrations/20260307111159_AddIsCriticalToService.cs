using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XLabStatusService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCriticalToService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "Services",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Services_IsCritical",
                table: "Services",
                column: "IsCritical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_IsCritical",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsCritical",
                table: "Services");
        }
    }
}
