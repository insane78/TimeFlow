using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AggiungiIndiceUnicoRegistrazioneOre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RegistrazioniOre_UtenteId_Data_ProgettoId_AttivitaId",
                table: "RegistrazioniOre",
                columns: new[] { "UtenteId", "Data", "ProgettoId", "AttivitaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrazioniOre_UtenteId_Data_ProgettoId_AttivitaId",
                table: "RegistrazioniOre");
        }
    }
}
