using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SellosYDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documentos_maquina",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden_trabajo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    emisor = table.Column<string>(type: "text", nullable: true),
                    numero_documento = table.Column<string>(type: "text", nullable: true),
                    fecha_documento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_vencimiento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nombre_archivo = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    tipo_contenido = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tamanio_bytes = table.Column<long>(type: "bigint", nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documentos_maquina", x => x.id);
                    table.ForeignKey(
                        name: "fk_documentos_maquina_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documentos_maquina_maquinas_maquina_id",
                        column: x => x.maquina_id,
                        principalTable: "maquinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documentos_maquina_ordenes_trabajo_orden_trabajo_id",
                        column: x => x.orden_trabajo_id,
                        principalTable: "ordenes_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sellos_fila",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tabla = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    fila_id = table.Column<Guid>(type: "uuid", nullable: false),
                    digito = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version_llave = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version_formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    calculado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sellos_fila", x => x.id);
                    table.ForeignKey(
                        name: "fk_sellos_fila_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sellos_tabla",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tabla = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    secuencia = table.Column<long>(type: "bigint", nullable: false),
                    filas = table.Column<long>(type: "bigint", nullable: false),
                    filas_con_digito_invalido = table.Column<long>(type: "bigint", nullable: false),
                    digito = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    digito_anterior = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version_llave = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version_formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    calculado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sellos_tabla", x => x.id);
                    table.ForeignKey(
                        name: "fk_sellos_tabla_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documentos_maquina_empresa_id_fecha_vencimiento",
                table: "documentos_maquina",
                columns: new[] { "empresa_id", "fecha_vencimiento" });

            migrationBuilder.CreateIndex(
                name: "ix_documentos_maquina_empresa_id_hash",
                table: "documentos_maquina",
                columns: new[] { "empresa_id", "hash" });

            migrationBuilder.CreateIndex(
                name: "ix_documentos_maquina_maquina_id_tipo",
                table: "documentos_maquina",
                columns: new[] { "maquina_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_documentos_maquina_orden_trabajo_id",
                table: "documentos_maquina",
                column: "orden_trabajo_id");

            migrationBuilder.CreateIndex(
                name: "ix_sellos_fila_empresa_id_tabla_fila_id",
                table: "sellos_fila",
                columns: new[] { "empresa_id", "tabla", "fila_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sellos_fila_tabla_fila_id",
                table: "sellos_fila",
                columns: new[] { "tabla", "fila_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sellos_tabla_empresa_id_tabla_calculado_en",
                table: "sellos_tabla",
                columns: new[] { "empresa_id", "tabla", "calculado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_sellos_tabla_empresa_id_tabla_secuencia",
                table: "sellos_tabla",
                columns: new[] { "empresa_id", "tabla", "secuencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documentos_maquina");

            migrationBuilder.DropTable(
                name: "sellos_fila");

            migrationBuilder.DropTable(
                name: "sellos_tabla");
        }
    }
}
