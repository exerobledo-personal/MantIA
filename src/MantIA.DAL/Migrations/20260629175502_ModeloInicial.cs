using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ModeloInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogos_maquina",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marca = table.Column<string>(type: "text", nullable: false),
                    modelo = table.Column<string>(type: "text", nullable: false),
                    fallas_comunes = table.Column<string>(type: "text", nullable: true),
                    repuestos_sugeridos = table.Column<string>(type: "text", nullable: true),
                    intervalos_mantenimiento = table.Column<string>(type: "text", nullable: true),
                    estado_enriquecimiento = table.Column<string>(type: "text", nullable: false),
                    fecha_ultimo_enriquecimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalogos_maquina", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razon_social = table.Column<string>(type: "text", nullable: false),
                    dominio = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_maquinas_habilitadas = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "maquinas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    planta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    marca = table.Column<string>(type: "text", nullable: false),
                    modelo = table.Column<string>(type: "text", nullable: false),
                    numero_serie = table.Column<string>(type: "text", nullable: true),
                    catalogo_maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maquinas", x => x.id);
                    table.ForeignKey(
                        name: "fk_maquinas_catalogos_maquina_catalogo_maquina_id",
                        column: x => x.catalogo_maquina_id,
                        principalTable: "catalogos_maquina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_catalogo_maquina_id",
                table: "maquinas",
                column: "catalogo_maquina_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "maquinas");

            migrationBuilder.DropTable(
                name: "catalogos_maquina");
        }
    }
}
