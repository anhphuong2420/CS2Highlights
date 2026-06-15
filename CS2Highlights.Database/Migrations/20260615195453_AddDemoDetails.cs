using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Highlights.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    MapName = table.Column<string>(type: "TEXT", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayersJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoDetails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoDetails_FileName",
                table: "DemoDetails",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoDetails");
        }
    }
}
