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
            // Comentado para evitar remover a coluna 'Produtos' caso o banco já não a possua
            // ou para evitar inconsistências entre código e esquema:
            // migrationBuilder.DropColumn(
            //     name: "Produtos",
            //     table: "Hortas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Comentado para manter simetria com Up
            // migrationBuilder.AddColumn<string>(
            //     name: "Produtos",
            //     table: "Hortas",
            //     type: "nvarchar(1000)",
            //     maxLength: 1000,
            //     nullable: true);
        }
    }
}
