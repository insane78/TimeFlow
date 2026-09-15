using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AggiungiCompensoGiornalieroUtente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompensoGiornaliero",
                table: "AspNetUsers",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensoGiornaliero",
                table: "AspNetUsers");
        }
    }
}
