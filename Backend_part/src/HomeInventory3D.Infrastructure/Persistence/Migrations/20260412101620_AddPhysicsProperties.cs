using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeInventory3D.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicsProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "bounciness",
                table: "inventory_items",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "collider_type",
                table: "inventory_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "friction",
                table: "inventory_items",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_fragile",
                table: "inventory_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "mass_kg",
                table: "inventory_items",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "material_type",
                table: "inventory_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bounciness",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "collider_type",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "friction",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "is_fragile",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "mass_kg",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "material_type",
                table: "inventory_items");
        }
    }
}
