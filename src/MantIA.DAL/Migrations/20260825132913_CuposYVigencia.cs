using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class CuposYVigencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dias_vigencia",
                table: "planes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "es_prueba",
                table: "planes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_ordenes_trabajo",
                table: "planes",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "max_maquinas_habilitadas",
                table: "empresas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fin_vigencia",
                table: "empresas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "inicio_vigencia",
                table: "empresas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_ordenes_trabajo",
                table: "empresas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_plantas_habilitadas",
                table: "empresas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_usuarios_habilitados",
                table: "empresas",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dias_vigencia",
                table: "planes");

            migrationBuilder.DropColumn(
                name: "es_prueba",
                table: "planes");

            migrationBuilder.DropColumn(
                name: "max_ordenes_trabajo",
                table: "planes");

            migrationBuilder.DropColumn(
                name: "fin_vigencia",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "inicio_vigencia",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "max_ordenes_trabajo",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "max_plantas_habilitadas",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "max_usuarios_habilitados",
                table: "empresas");

            migrationBuilder.AlterColumn<int>(
                name: "max_maquinas_habilitadas",
                table: "empresas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
