using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "niveles_permiso",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_niveles_permiso", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permisos_por_rol_y_nivel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    nivel_permiso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recurso = table.Column<string>(type: "text", nullable: false),
                    accion_permitida = table.Column<string>(type: "text", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permisos_por_rol_y_nivel", x => x.id);
                    table.ForeignKey(
                        name: "fk_permisos_por_rol_y_nivel_niveles_permiso_nivel_permiso_id",
                        column: x => x.nivel_permiso_id,
                        principalTable: "niveles_permiso",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_permisos_por_rol_y_nivel_nivel_permiso_id",
                table: "permisos_por_rol_y_nivel",
                column: "nivel_permiso_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permisos_por_rol_y_nivel");

            migrationBuilder.DropTable(
                name: "niveles_permiso");
        }
    }
}
