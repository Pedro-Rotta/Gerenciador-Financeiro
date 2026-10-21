using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorContaFamiliar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CategoriaSubCategoriaPertenceConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContaConjuntaId",
                table: "SubCategorias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContaId",
                table: "SubCategorias",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Membros",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeDeUsuario",
                table: "Membros",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Membros",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "ContasConjuntas",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ContaConjuntaId",
                table: "Categorias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContaId",
                table: "Categorias",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubCategorias_ContaConjuntaId",
                table: "SubCategorias",
                column: "ContaConjuntaId");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategorias_ContaId",
                table: "SubCategorias",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_Membros_NomeDeUsuario_Unico",
                table: "Membros",
                column: "NomeDeUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_ContaConjuntaId",
                table: "Categorias",
                column: "ContaConjuntaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_ContaId",
                table: "Categorias",
                column: "ContaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_Conta",
                table: "Categorias",
                column: "ContaId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_ContaConjunta",
                table: "Categorias",
                column: "ContaConjuntaId",
                principalTable: "ContasConjuntas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategorias_Conta",
                table: "SubCategorias",
                column: "ContaId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategorias_ContaConjunta",
                table: "SubCategorias",
                column: "ContaConjuntaId",
                principalTable: "ContasConjuntas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_Conta",
                table: "Categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_ContaConjunta",
                table: "Categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategorias_Conta",
                table: "SubCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategorias_ContaConjunta",
                table: "SubCategorias");

            migrationBuilder.DropIndex(
                name: "IX_SubCategorias_ContaConjuntaId",
                table: "SubCategorias");

            migrationBuilder.DropIndex(
                name: "IX_SubCategorias_ContaId",
                table: "SubCategorias");

            migrationBuilder.DropIndex(
                name: "IX_Membros_NomeDeUsuario_Unico",
                table: "Membros");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_ContaConjuntaId",
                table: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_ContaId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "ContaConjuntaId",
                table: "SubCategorias");

            migrationBuilder.DropColumn(
                name: "ContaId",
                table: "SubCategorias");

            migrationBuilder.DropColumn(
                name: "NomeDeUsuario",
                table: "Membros");

            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "Membros");

            migrationBuilder.DropColumn(
                name: "ContaConjuntaId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "ContaId",
                table: "Categorias");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Membros",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "ContasConjuntas",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
