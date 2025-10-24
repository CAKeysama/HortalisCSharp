using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortalisCSharp.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Indicacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    TipoHorta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlimentosCultivados = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NomeHorta = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Localizacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AreaNome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Indicacoes_AreaNome",
                table: "Indicacoes",
                column: "AreaNome");

            migrationBuilder.CreateIndex(
                name: "IX_Indicacoes_Latitude_Longitude",
                table: "Indicacoes",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Indicacoes");
        }
    }
}
