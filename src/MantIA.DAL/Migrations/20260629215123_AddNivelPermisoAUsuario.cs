using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNivelPermisoAUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "nivel_permiso_id",
                table: "usuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_nivel_permiso_id",
                table: "usuarios",
                column: "nivel_permiso_id");

            migrationBuilder.AddForeignKey(
                name: "fk_usuarios_niveles_permiso_nivel_permiso_id",
                table: "usuarios",
                column: "nivel_permiso_id",
                principalTable: "niveles_permiso",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_niveles_permiso_nivel_permiso_id",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_nivel_permiso_id",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "nivel_permiso_id",
                table: "usuarios");
        }
    }
}
