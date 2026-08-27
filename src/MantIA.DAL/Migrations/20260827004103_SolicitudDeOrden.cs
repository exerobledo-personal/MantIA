using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudDeOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "controlada_por_usuario_id",
                table: "ordenes_trabajo",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_control",
                table: "ordenes_trabajo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_rechazo",
                table: "ordenes_trabajo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "solicitante_usuario_id",
                table: "ordenes_trabajo",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cobranza",
                table: "empresas",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_ultimo_aviso_mora",
                table: "empresas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_ultimo_pago",
                table: "empresas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "controlada_por_usuario_id",
                table: "ordenes_trabajo");

            migrationBuilder.DropColumn(
                name: "fecha_control",
                table: "ordenes_trabajo");

            migrationBuilder.DropColumn(
                name: "motivo_rechazo",
                table: "ordenes_trabajo");

            migrationBuilder.DropColumn(
                name: "solicitante_usuario_id",
                table: "ordenes_trabajo");

            migrationBuilder.DropColumn(
                name: "cobranza",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "fecha_ultimo_aviso_mora",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "fecha_ultimo_pago",
                table: "empresas");
        }
    }
}
