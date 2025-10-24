using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortalisCSharp.Migrations
{
    public partial class RemoveUnusedColumnsFromIndicacoes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remover colunas que não são usadas pelo código atual
            migrationBuilder.DropColumn(
                name: "TipoHorta",
                table: "Indicacoes");

            migrationBuilder.DropColumn(
                name: "AlimentosCultivados",
                table: "Indicacoes");

            migrationBuilder.DropColumn(
                name: "NomeHorta",
                table: "Indicacoes");

            migrationBuilder.DropColumn(
                name: "Localizacao",
                table: "Indicacoes");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Indicacoes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recriar colunas caso precise reverter a migration
            migrationBuilder.AddColumn<string>(
                name: "TipoHorta",
                table: "Indicacoes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlimentosCultivados",
                table: "Indicacoes",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeHorta",
                table: "Indicacoes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Localizacao",
                table: "Indicacoes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Indicacoes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}