using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIndiceEmailUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Membros_Email_Unico",
                table: "Membros",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Membros_Email_Unico",
                table: "Membros");
        }
    }
}
