using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarContaPrincipalEMembroContaPrincipalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContaPrincipalId",
                table: "Membros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContaPrincipal",
                table: "Contas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Membros_ContaPrincipalId",
                table: "Membros",
                column: "ContaPrincipalId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Membros_Contas_ContaPrincipalId",
                table: "Membros",
                column: "ContaPrincipalId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Membros_Contas_ContaPrincipalId",
                table: "Membros");

            migrationBuilder.DropIndex(
                name: "IX_Membros_ContaPrincipalId",
                table: "Membros");

            migrationBuilder.DropColumn(
                name: "ContaPrincipalId",
                table: "Membros");

            migrationBuilder.DropColumn(
                name: "ContaPrincipal",
                table: "Contas");
        }
    }
}
