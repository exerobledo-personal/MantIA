using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class QuitarMarcaModeloDeMaquina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "marca",
                table: "maquinas");

            migrationBuilder.DropColumn(
                name: "modelo",
                table: "maquinas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "marca",
                table: "maquinas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modelo",
                table: "maquinas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
