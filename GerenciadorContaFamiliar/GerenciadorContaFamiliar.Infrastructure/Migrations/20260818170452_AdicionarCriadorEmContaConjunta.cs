using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCriadorEmContaConjunta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CriadorId",
                table: "ContasConjuntas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ContasConjuntas_CriadorId",
                table: "ContasConjuntas",
                column: "CriadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContasConjuntas_Membros_CriadorId",
                table: "ContasConjuntas",
                column: "CriadorId",
                principalTable: "Membros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContasConjuntas_Membros_CriadorId",
                table: "ContasConjuntas");

            migrationBuilder.DropIndex(
                name: "IX_ContasConjuntas_CriadorId",
                table: "ContasConjuntas");

            migrationBuilder.DropColumn(
                name: "CriadorId",
                table: "ContasConjuntas");
        }
    }
}
