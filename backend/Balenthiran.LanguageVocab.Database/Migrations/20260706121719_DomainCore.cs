using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balenthiran.LanguageVocab.Database.Migrations
{
    /// <inheritdoc />
    public partial class DomainCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Hanzi = table.Column<string>(type: "text", nullable: false),
                    Pinyin = table.Column<string>(type: "text", nullable: false),
                    PinyinNormalised = table.Column<string>(type: "text", nullable: false),
                    Glosses = table.Column<List<string>>(type: "text[]", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    FrequencyRank = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnswerLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    VocabItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Given = table.Column<string>(type: "text", nullable: false),
                    Verdict = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerLogs_VocabItems_VocabItemId",
                        column: x => x.VocabItemId,
                        principalTable: "VocabItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWordStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    VocabItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Strength = table.Column<int>(type: "integer", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimesSeen = table.Column<int>(type: "integer", nullable: false),
                    TimesCorrect = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWordStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWordStates_VocabItems_VocabItemId",
                        column: x => x.VocabItemId,
                        principalTable: "VocabItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerLogs_UserId_VocabItemId",
                table: "AnswerLogs",
                columns: new[] { "UserId", "VocabItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerLogs_VocabItemId",
                table: "AnswerLogs",
                column: "VocabItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWordStates_UserId_VocabItemId",
                table: "UserWordStates",
                columns: new[] { "UserId", "VocabItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWordStates_VocabItemId",
                table: "UserWordStates",
                column: "VocabItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabItems_Language_FrequencyRank",
                table: "VocabItems",
                columns: new[] { "Language", "FrequencyRank" });

            migrationBuilder.CreateIndex(
                name: "IX_VocabItems_Language_Hanzi",
                table: "VocabItems",
                columns: new[] { "Language", "Hanzi" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerLogs");

            migrationBuilder.DropTable(
                name: "UserWordStates");

            migrationBuilder.DropTable(
                name: "VocabItems");
        }
    }
}
