using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortalisCSharp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProdutosColum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Removendo a coluna 'Produtos' da tabela 'Hortas'
            migrationBuilder.DropColumn(
                name: "Produtos",
                table: "Hortas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurando a coluna 'Produtos' caso seja necessário reverter
            migrationBuilder.AddColumn<string>(
                name: "Produtos",
                table: "Hortas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
