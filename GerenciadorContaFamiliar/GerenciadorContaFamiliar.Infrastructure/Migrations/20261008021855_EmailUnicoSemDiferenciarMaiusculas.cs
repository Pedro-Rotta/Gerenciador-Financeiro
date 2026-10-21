using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmailUnicoSemDiferenciarMaiusculas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Troca o índice único (que diferenciava maiúsculas) por um em lower("Email"):
            // "Ana@Email.com" e "ana@email.com" passam a ser o mesmo e-mail também no banco.
            // O EF Core não modela índice por expressão, por isso o SQL direto.
            migrationBuilder.DropIndex(
                name: "IX_Membros_Email_Unico",
                table: "Membros");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Membros_Email_Unico\" ON \"Membros\" (lower(\"Email\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Membros_Email_Unico\";");

            migrationBuilder.CreateIndex(
                name: "IX_Membros_Email_Unico",
                table: "Membros",
                column: "Email",
                unique: true);
        }
    }
}
