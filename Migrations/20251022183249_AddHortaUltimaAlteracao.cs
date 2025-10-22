using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortalisCSharp.Migrations
{
    /// <inheritdoc />
    public partial class AddHortaUltimaAlteracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaAlteracao",
                table: "Hortas",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UltimaAlteracao",
                table: "Hortas");
        }
    }
}
