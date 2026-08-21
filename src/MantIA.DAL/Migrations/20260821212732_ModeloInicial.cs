using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ModeloInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "catalogos_maquina",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marca = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    categoria = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    intervalos_mantenimiento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fecha_ultimo_enriquecimiento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version_ingesta = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ultimo_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalogos_maquina", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "eventos_pendientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cadena = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    contenido = table.Column<string>(type: "jsonb", nullable: false),
                    severidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_evento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    ultimo_intento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultimo_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_pendientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    max_maquinas = table.Column<int>(type: "integer", nullable: false),
                    max_usuarios = table.Column<int>(type: "integer", nullable: false),
                    max_plantas = table.Column<int>(type: "integer", nullable: false),
                    precio_mensual = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalogo_fallas_comunes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalogo_maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    empresas_que_la_corroboraron = table.Column<int>(type: "integer", nullable: false),
                    eventos_registrados = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalogo_fallas_comunes", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_fallas_comunes_catalogos_maquina_catalogo_maquina_",
                        column: x => x.catalogo_maquina_id,
                        principalTable: "catalogos_maquina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalogo_repuestos_sugeridos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalogo_maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    numero_parte_referencia = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    criticidad_sugerida = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalogo_repuestos_sugeridos", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_repuestos_sugeridos_catalogos_maquina_catalogo_maq",
                        column: x => x.catalogo_maquina_id,
                        principalTable: "catalogos_maquina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidencias_modelo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    catalogo_maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden_trabajo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    texto_original = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    modo_falla_normalizado = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    promovida = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_promocion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evidencias_modelo", x => x.id);
                    table.ForeignKey(
                        name: "fk_evidencias_modelo_catalogos_maquina_catalogo_maquina_id",
                        column: x => x.catalogo_maquina_id,
                        principalTable: "catalogos_maquina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razon_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dominio = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_maquinas_habilitadas = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresas", x => x.id);
                    table.ForeignKey(
                        name: "fk_empresas_planes_plan_id",
                        column: x => x.plan_id,
                        principalTable: "planes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contadores_documento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    ultimo = table.Column<long>(type: "bigint", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contadores_documento", x => x.id);
                    table.ForeignKey(
                        name: "fk_contadores_documento_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "niveles_permiso",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    jerarquia = table.Column<int>(type: "integer", nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_niveles_permiso", x => x.id);
                    table.ForeignKey(
                        name: "fk_niveles_permiso_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plantas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    localidad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    latitud = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitud = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantas", x => x.id);
                    table.ForeignKey(
                        name: "fk_plantas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reportes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    filtros_json = table.Column<string>(type: "jsonb", nullable: true),
                    periodo_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    periodo_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reportes", x => x.id);
                    table.ForeignKey(
                        name: "fk_reportes_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repuestos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    numero_parte = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    unidad_medida = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stock_actual = table.Column<int>(type: "integer", nullable: false),
                    stock_minimo = table.Column<int>(type: "integer", nullable: false),
                    criticidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    proveedor = table.Column<string>(type: "text", nullable: true),
                    plazo_reposicion_dias = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repuestos", x => x.id);
                    table.ForeignKey(
                        name: "fk_repuestos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes_rollback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_objetivo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recurso_filtro = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    solicitada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_solicitud = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aprobada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_aprobacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_rechazo = table.Column<string>(type: "text", nullable: true),
                    eventos_alcanzados = table.Column<int>(type: "integer", nullable: false),
                    eventos_revertidos = table.Column<int>(type: "integer", nullable: false),
                    eventos_no_revertidos = table.Column<string>(type: "jsonb", nullable: true),
                    fecha_aplicacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitudes_rollback", x => x.id);
                    table.ForeignKey(
                        name: "fk_solicitudes_rollback_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permisos_por_rol_y_nivel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nivel_permiso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recurso = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    concedido = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permisos_por_rol_y_nivel", x => x.id);
                    table.ForeignKey(
                        name: "fk_permisos_por_rol_y_nivel_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_permisos_por_rol_y_nivel_niveles_permiso_nivel_permiso_id",
                        column: x => x.nivel_permiso_id,
                        principalTable: "niveles_permiso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth0_user_id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nivel_permiso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ultimo_acceso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuarios_niveles_permiso_nivel_permiso_id",
                        column: x => x.nivel_permiso_id,
                        principalTable: "niveles_permiso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maquinas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    planta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linea_sector = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    catalogo_maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criticidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    horas_operacion = table.Column<int>(type: "integer", nullable: false),
                    fecha_baja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maquinas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maquinas_plantas_planta_id",
                        column: x => x.planta_id,
                        principalTable: "plantas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reportes_historial",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    detalle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reportes_historial", x => x.id);
                    table.ForeignKey(
                        name: "fk_reportes_historial_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reportes_historial_reportes_reporte_id",
                        column: x => x.reporte_id,
                        principalTable: "reportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alertas_stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    criticidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    stock_al_disparar = table.Column<int>(type: "integer", nullable: false),
                    umbral_al_disparar = table.Column<int>(type: "integer", nullable: false),
                    fecha_disparo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_resolucion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resuelta_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alertas_stock", x => x.id);
                    table.ForeignKey(
                        name: "fk_alertas_stock_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_alertas_stock_repuestos_repuesto_id",
                        column: x => x.repuesto_id,
                        principalTable: "repuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permisos_por_usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recurso = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    concedido = table.Column<bool>(type: "boolean", nullable: false),
                    vigente_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    otorgado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permisos_por_usuario", x => x.id);
                    table.ForeignKey(
                        name: "fk_permisos_por_usuario_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_permisos_por_usuario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_alcance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios_alcance", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_alcance_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuarios_alcance_plantas_planta_id",
                        column: x => x.planta_id,
                        principalTable: "plantas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_usuarios_alcance_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maquinas_repuesto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad_por_equipo = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maquinas_repuesto", x => x.id);
                    table.ForeignKey(
                        name: "fk_maquinas_repuesto_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maquinas_repuesto_maquinas_maquina_id",
                        column: x => x.maquina_id,
                        principalTable: "maquinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_maquinas_repuesto_repuestos_repuesto_id",
                        column: x => x.repuesto_id,
                        principalTable: "repuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordenes_trabajo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    maquina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prioridad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    descripcion_problema = table.Column<string>(type: "text", nullable: false),
                    descripcion_resolucion = table.Column<string>(type: "text", nullable: true),
                    responsable_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_apertura = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    horas_resolucion = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordenes_trabajo", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordenes_trabajo_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordenes_trabajo_maquinas_maquina_id",
                        column: x => x.maquina_id,
                        principalTable: "maquinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordenes_trabajo_usuarios_responsable_usuario_id",
                        column: x => x.responsable_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recomendaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maquina_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origen = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prioridad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    cantidad_sugerida = table.Column<int>(type: "integer", nullable: false),
                    stock_al_generar = table.Column<int>(type: "integer", nullable: false),
                    justificacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    regla_aplicada = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    confianza = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    fecha_generacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_decision = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decidida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad_confirmada = table.Column<int>(type: "integer", nullable: true),
                    motivo_rechazo = table.Column<string>(type: "text", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recomendaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_recomendaciones_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recomendaciones_maquinas_maquina_id",
                        column: x => x.maquina_id,
                        principalTable: "maquinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recomendaciones_repuestos_repuesto_id",
                        column: x => x.repuesto_id,
                        principalTable: "repuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historial_ordenes_trabajo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden_trabajo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    campo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    valor_anterior = table.Column<string>(type: "text", nullable: true),
                    valor_nuevo = table.Column<string>(type: "text", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evento_bitacora_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_ordenes_trabajo", x => x.id);
                    table.ForeignKey(
                        name: "fk_historial_ordenes_trabajo_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historial_ordenes_trabajo_ordenes_trabajo_orden_trabajo_id",
                        column: x => x.orden_trabajo_id,
                        principalTable: "ordenes_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    saldo_resultante = table.Column<int>(type: "integer", nullable: false),
                    orden_trabajo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    fecha_movimiento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimientos_stock", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimientos_stock_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_stock_ordenes_trabajo_orden_trabajo_id",
                        column: x => x.orden_trabajo_id,
                        principalTable: "ordenes_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_stock_repuestos_repuesto_id",
                        column: x => x.repuesto_id,
                        principalTable: "repuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ordenes_trabajo_repuesto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden_trabajo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    costo_unitario_al_consumo = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordenes_trabajo_repuesto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordenes_trabajo_repuesto_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordenes_trabajo_repuesto_ordenes_trabajo_orden_trabajo_id",
                        column: x => x.orden_trabajo_id,
                        principalTable: "ordenes_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ordenes_trabajo_repuesto_repuestos_repuesto_id",
                        column: x => x.repuesto_id,
                        principalTable: "repuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alertas_stock_empresa_id_estado_criticidad",
                table: "alertas_stock",
                columns: new[] { "empresa_id", "estado", "criticidad" });

            migrationBuilder.CreateIndex(
                name: "ix_alertas_stock_empresa_id_repuesto_id_fecha_disparo",
                table: "alertas_stock",
                columns: new[] { "empresa_id", "repuesto_id", "fecha_disparo" });

            migrationBuilder.CreateIndex(
                name: "ix_alertas_stock_repuesto_id",
                table: "alertas_stock",
                column: "repuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_fallas_comunes_catalogo_maquina_id",
                table: "catalogo_fallas_comunes",
                column: "catalogo_maquina_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_repuestos_sugeridos_catalogo_maquina_id",
                table: "catalogo_repuestos_sugeridos",
                column: "catalogo_maquina_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogos_maquina_estado",
                table: "catalogos_maquina",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_catalogos_maquina_marca_modelo",
                table: "catalogos_maquina",
                columns: new[] { "marca", "modelo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contadores_documento_empresa_id_tipo_anio",
                table: "contadores_documento",
                columns: new[] { "empresa_id", "tipo", "anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_empresas_dominio",
                table: "empresas",
                column: "dominio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_empresas_plan_id",
                table: "empresas",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_empresas_tenant_id",
                table: "empresas",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventos_pendientes_fecha_evento",
                table: "eventos_pendientes",
                column: "fecha_evento");

            migrationBuilder.CreateIndex(
                name: "ix_evidencias_modelo_catalogo_maquina_id_promovida",
                table: "evidencias_modelo",
                columns: new[] { "catalogo_maquina_id", "promovida" });

            migrationBuilder.CreateIndex(
                name: "ix_evidencias_modelo_embedding",
                table: "evidencias_modelo",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_evidencias_modelo_empresa_id",
                table: "evidencias_modelo",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_ordenes_trabajo_empresa_id_usuario_id_fecha",
                table: "historial_ordenes_trabajo",
                columns: new[] { "empresa_id", "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_historial_ordenes_trabajo_orden_trabajo_id_fecha",
                table: "historial_ordenes_trabajo",
                columns: new[] { "orden_trabajo_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_catalogo_maquina_id",
                table: "maquinas",
                column: "catalogo_maquina_id");

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_empresa_id_codigo",
                table: "maquinas",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_empresa_id_planta_id_estado",
                table: "maquinas",
                columns: new[] { "empresa_id", "planta_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_planta_id",
                table: "maquinas",
                column: "planta_id");

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_repuesto_empresa_id",
                table: "maquinas_repuesto",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_repuesto_maquina_id_repuesto_id",
                table: "maquinas_repuesto",
                columns: new[] { "maquina_id", "repuesto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_maquinas_repuesto_repuesto_id",
                table: "maquinas_repuesto",
                column: "repuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_stock_empresa_id_repuesto_id_fecha_movimiento",
                table: "movimientos_stock",
                columns: new[] { "empresa_id", "repuesto_id", "fecha_movimiento" });

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_stock_orden_trabajo_id",
                table: "movimientos_stock",
                column: "orden_trabajo_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_stock_repuesto_id",
                table: "movimientos_stock",
                column: "repuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_niveles_permiso_empresa_id_nombre",
                table: "niveles_permiso",
                columns: new[] { "empresa_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_empresa_id_estado_fecha_apertura",
                table: "ordenes_trabajo",
                columns: new[] { "empresa_id", "estado", "fecha_apertura" });

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_empresa_id_maquina_id_fecha_apertura",
                table: "ordenes_trabajo",
                columns: new[] { "empresa_id", "maquina_id", "fecha_apertura" });

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_empresa_id_numero",
                table: "ordenes_trabajo",
                columns: new[] { "empresa_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_maquina_id",
                table: "ordenes_trabajo",
                column: "maquina_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_responsable_usuario_id",
                table: "ordenes_trabajo",
                column: "responsable_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_repuesto_empresa_id",
                table: "ordenes_trabajo_repuesto",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_repuesto_orden_trabajo_id_repuesto_id",
                table: "ordenes_trabajo_repuesto",
                columns: new[] { "orden_trabajo_id", "repuesto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordenes_trabajo_repuesto_repuesto_id",
                table: "ordenes_trabajo_repuesto",
                column: "repuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_permisos_por_rol_y_nivel_empresa_id_rol_nivel_permiso_id_re",
                table: "permisos_por_rol_y_nivel",
                columns: new[] { "empresa_id", "rol", "nivel_permiso_id", "recurso", "accion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permisos_por_rol_y_nivel_nivel_permiso_id",
                table: "permisos_por_rol_y_nivel",
                column: "nivel_permiso_id");

            migrationBuilder.CreateIndex(
                name: "ix_permisos_por_usuario_empresa_id_vigente_hasta",
                table: "permisos_por_usuario",
                columns: new[] { "empresa_id", "vigente_hasta" });

            migrationBuilder.CreateIndex(
                name: "ix_permisos_por_usuario_usuario_id_recurso_accion",
                table: "permisos_por_usuario",
                columns: new[] { "usuario_id", "recurso", "accion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_planes_nombre",
                table: "planes",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plantas_empresa_id_nombre",
                table: "plantas",
                columns: new[] { "empresa_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recomendaciones_empresa_id_estado_prioridad",
                table: "recomendaciones",
                columns: new[] { "empresa_id", "estado", "prioridad" });

            migrationBuilder.CreateIndex(
                name: "ix_recomendaciones_empresa_id_repuesto_id_fecha_generacion",
                table: "recomendaciones",
                columns: new[] { "empresa_id", "repuesto_id", "fecha_generacion" });

            migrationBuilder.CreateIndex(
                name: "ix_recomendaciones_maquina_id",
                table: "recomendaciones",
                column: "maquina_id");

            migrationBuilder.CreateIndex(
                name: "ix_recomendaciones_repuesto_id",
                table: "recomendaciones",
                column: "repuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_reportes_empresa_id_estado_tipo",
                table: "reportes",
                columns: new[] { "empresa_id", "estado", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_reportes_historial_empresa_id",
                table: "reportes_historial",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_reportes_historial_reporte_id_fecha",
                table: "reportes_historial",
                columns: new[] { "reporte_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_repuestos_empresa_id_estado_criticidad",
                table: "repuestos",
                columns: new[] { "empresa_id", "estado", "criticidad" });

            migrationBuilder.CreateIndex(
                name: "ix_repuestos_empresa_id_numero_parte",
                table: "repuestos",
                columns: new[] { "empresa_id", "numero_parte" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_rollback_empresa_id_estado",
                table: "solicitudes_rollback",
                columns: new[] { "empresa_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_rollback_usuario_objetivo_id",
                table: "solicitudes_rollback",
                column: "usuario_objetivo_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_auth0_user_id",
                table: "usuarios",
                column: "auth0_user_id",
                unique: true,
                filter: "fecha_baja IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_empresa_id_email",
                table: "usuarios",
                columns: new[] { "empresa_id", "email" },
                unique: true,
                filter: "fecha_baja IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_nivel_permiso_id",
                table: "usuarios",
                column: "nivel_permiso_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_alcance_empresa_id",
                table: "usuarios_alcance",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_alcance_planta_id",
                table: "usuarios_alcance",
                column: "planta_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_alcance_usuario_id_planta_id",
                table: "usuarios_alcance",
                columns: new[] { "usuario_id", "planta_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas_stock");

            migrationBuilder.DropTable(
                name: "catalogo_fallas_comunes");

            migrationBuilder.DropTable(
                name: "catalogo_repuestos_sugeridos");

            migrationBuilder.DropTable(
                name: "contadores_documento");

            migrationBuilder.DropTable(
                name: "eventos_pendientes");

            migrationBuilder.DropTable(
                name: "evidencias_modelo");

            migrationBuilder.DropTable(
                name: "historial_ordenes_trabajo");

            migrationBuilder.DropTable(
                name: "maquinas_repuesto");

            migrationBuilder.DropTable(
                name: "movimientos_stock");

            migrationBuilder.DropTable(
                name: "ordenes_trabajo_repuesto");

            migrationBuilder.DropTable(
                name: "permisos_por_rol_y_nivel");

            migrationBuilder.DropTable(
                name: "permisos_por_usuario");

            migrationBuilder.DropTable(
                name: "recomendaciones");

            migrationBuilder.DropTable(
                name: "reportes_historial");

            migrationBuilder.DropTable(
                name: "solicitudes_rollback");

            migrationBuilder.DropTable(
                name: "usuarios_alcance");

            migrationBuilder.DropTable(
                name: "ordenes_trabajo");

            migrationBuilder.DropTable(
                name: "repuestos");

            migrationBuilder.DropTable(
                name: "reportes");

            migrationBuilder.DropTable(
                name: "maquinas");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "catalogos_maquina");

            migrationBuilder.DropTable(
                name: "plantas");

            migrationBuilder.DropTable(
                name: "niveles_permiso");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "planes");
        }
    }
}
