using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeInventory3D.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "containers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nfc_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    qr_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    width_mm = table.Column<float>(type: "real", nullable: true),
                    height_mm = table.Column<float>(type: "real", nullable: true),
                    depth_mm = table.Column<float>(type: "real", nullable: true),
                    mesh_file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    thumbnail_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_containers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    description = table.Column<string>(type: "text", nullable: true),
                    position_x = table.Column<float>(type: "real", nullable: true),
                    position_y = table.Column<float>(type: "real", nullable: true),
                    position_z = table.Column<float>(type: "real", nullable: true),
                    bbox_min_x = table.Column<float>(type: "real", nullable: true),
                    bbox_min_y = table.Column<float>(type: "real", nullable: true),
                    bbox_min_z = table.Column<float>(type: "real", nullable: true),
                    bbox_max_x = table.Column<float>(type: "real", nullable: true),
                    bbox_max_y = table.Column<float>(type: "real", nullable: true),
                    bbox_max_z = table.Column<float>(type: "real", nullable: true),
                    rotation_x = table.Column<float>(type: "real", nullable: true),
                    rotation_y = table.Column<float>(type: "real", nullable: true),
                    rotation_z = table.Column<float>(type: "real", nullable: true),
                    photo_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    mesh_file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    thumbnail_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    confidence = table.Column<float>(type: "real", nullable: true),
                    recognition_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Present"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_items_containers_container_id",
                        column: x => x.container_id,
                        principalTable: "containers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scan_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scan_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    point_cloud_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    depth_map_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rgb_photo_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    items_detected = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items_added = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items_removed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_sessions_containers_container_id",
                        column: x => x.container_id,
                        principalTable: "containers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_containers_nfc_id",
                table: "containers",
                column: "nfc_id",
                unique: true,
                filter: "nfc_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_containers_qr_code",
                table: "containers",
                column: "qr_code",
                unique: true,
                filter: "qr_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_items_container",
                table: "inventory_items",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_name_trgm",
                table: "inventory_items",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_items_status",
                table: "inventory_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_items_tags",
                table: "inventory_items",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_container_id",
                table: "scan_sessions",
                column: "container_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "scan_sessions");

            migrationBuilder.DropTable(
                name: "containers");
        }
    }
}
