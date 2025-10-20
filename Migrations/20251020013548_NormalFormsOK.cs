using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortalisCSharp.Migrations
{
    /// <inheritdoc />
    public partial class NormalFormsOK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HortaProdutos",
                columns: table => new
                {
                    HortaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HortaProdutos", x => new { x.HortaId, x.ProdutoId });
                    table.ForeignKey(
                        name: "FK_HortaProdutos_Hortas_HortaId",
                        column: x => x.HortaId,
                        principalTable: "Hortas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HortaProdutos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HortaProdutos_ProdutoId",
                table: "HortaProdutos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Nome",
                table: "Produtos",
                column: "Nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HortaProdutos");

            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
