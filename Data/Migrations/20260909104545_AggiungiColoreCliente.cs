using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AggiungiColoreCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Colore",
                table: "Clienti",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Colore",
                table: "Clienti");
        }
    }
}
