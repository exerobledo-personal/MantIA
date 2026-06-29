using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MantIA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixAuth0UserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "auth0user_id",
                table: "usuarios",
                newName: "auth0_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "auth0_user_id",
                table: "usuarios",
                newName: "auth0user_id");
        }
    }
}
