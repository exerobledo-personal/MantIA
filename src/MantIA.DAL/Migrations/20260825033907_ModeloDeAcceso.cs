using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ModeloDeAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_empresas_dominio",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "dominio",
                table: "empresas");

            migrationBuilder.CreateTable(
                name: "dominios_empresa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dominio = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dominios_empresa", x => x.id);
                    table.ForeignKey(
                        name: "fk_dominios_empresa_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nivel_permiso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fecha_vencimiento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    invitada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_aceptacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_revocacion = table.Column<string>(type: "text", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitaciones_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitaciones_niveles_permiso_nivel_permiso_id",
                        column: x => x.nivel_permiso_id,
                        principalTable: "niveles_permiso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dominios_empresa_dominio",
                table: "dominios_empresa",
                column: "dominio");

            migrationBuilder.CreateIndex(
                name: "ix_dominios_empresa_empresa_id_dominio",
                table: "dominios_empresa",
                columns: new[] { "empresa_id", "dominio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitaciones_email",
                table: "invitaciones",
                column: "email",
                unique: true,
                filter: "estado = 'Pendiente'");

            migrationBuilder.CreateIndex(
                name: "ix_invitaciones_empresa_id_estado",
                table: "invitaciones",
                columns: new[] { "empresa_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_invitaciones_fecha_vencimiento",
                table: "invitaciones",
                column: "fecha_vencimiento");

            migrationBuilder.CreateIndex(
                name: "ix_invitaciones_nivel_permiso_id",
                table: "invitaciones",
                column: "nivel_permiso_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitaciones_usuario_id",
                table: "invitaciones",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dominios_empresa");

            migrationBuilder.DropTable(
                name: "invitaciones");

            migrationBuilder.AddColumn<string>(
                name: "dominio",
                table: "empresas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_empresas_dominio",
                table: "empresas",
                column: "dominio",
                unique: true);
        }
    }
}
