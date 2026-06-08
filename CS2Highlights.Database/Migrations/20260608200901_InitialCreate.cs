using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Highlights.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<string>(type: "TEXT", nullable: false),
                    DemoPath = table.Column<string>(type: "TEXT", nullable: false),
                    DemoFileName = table.Column<string>(type: "TEXT", nullable: false),
                    Map = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SelectedPlayerSteamId = table.Column<string>(type: "TEXT", nullable: false),
                    SelectedPlayerName = table.Column<string>(type: "TEXT", nullable: false),
                    ParsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TickStart = table.Column<int>(type: "INTEGER", nullable: false),
                    TickEnd = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerSide = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrenadeEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tick = table.Column<int>(type: "INTEGER", nullable: false),
                    ThrowerSteamId = table.Column<string>(type: "TEXT", nullable: false),
                    GrenadeType = table.Column<string>(type: "TEXT", nullable: false),
                    DmgToEnemies = table.Column<int>(type: "INTEGER", nullable: false),
                    DmgToTeam = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemiesBlinded = table.Column<int>(type: "INTEGER", nullable: false),
                    TeammatesBlinded = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrenadeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrenadeEvents_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrenadeEvents_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Highlights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundId = table.Column<int>(type: "INTEGER", nullable: true),
                    HighlightType = table.Column<string>(type: "TEXT", nullable: true),
                    LowlightType = table.Column<string>(type: "TEXT", nullable: true),
                    TickStart = table.Column<int>(type: "INTEGER", nullable: false),
                    TickEnd = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ClipPath = table.Column<string>(type: "TEXT", nullable: true),
                    RenderStatus = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Highlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Highlights_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Highlights_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KillEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tick = table.Column<int>(type: "INTEGER", nullable: false),
                    KillerSteamId = table.Column<string>(type: "TEXT", nullable: false),
                    VictimSteamId = table.Column<string>(type: "TEXT", nullable: false),
                    Weapon = table.Column<string>(type: "TEXT", nullable: false),
                    IsHeadshot = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWallbang = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNoscope = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KillEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KillEvents_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KillEvents_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RenderJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HighlightId = table.Column<int>(type: "INTEGER", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ClipPath = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenderJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RenderJobs_Highlights_HighlightId",
                        column: x => x.HighlightId,
                        principalTable: "Highlights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrenadeEvents_MatchId",
                table: "GrenadeEvents",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GrenadeEvents_RoundId",
                table: "GrenadeEvents",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_MatchId",
                table: "Highlights",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_RoundId",
                table: "Highlights",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_KillEvents_MatchId",
                table: "KillEvents",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_KillEvents_RoundId",
                table: "KillEvents",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchId_SelectedPlayerSteamId",
                table: "Matches",
                columns: new[] { "MatchId", "SelectedPlayerSteamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_HighlightId",
                table: "RenderJobs",
                column: "HighlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_MatchId",
                table: "Rounds",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrenadeEvents");

            migrationBuilder.DropTable(
                name: "KillEvents");

            migrationBuilder.DropTable(
                name: "RenderJobs");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "Highlights");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
