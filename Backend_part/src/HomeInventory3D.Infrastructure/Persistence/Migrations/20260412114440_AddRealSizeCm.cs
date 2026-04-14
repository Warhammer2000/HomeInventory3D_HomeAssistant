using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeInventory3D.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRealSizeCm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "real_size_cm",
                table: "inventory_items",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "real_size_cm",
                table: "inventory_items");
        }
    }
}
