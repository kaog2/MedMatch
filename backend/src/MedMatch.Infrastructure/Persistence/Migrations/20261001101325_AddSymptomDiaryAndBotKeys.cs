using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSymptomDiaryAndBotKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "symptom_diary_sheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    OverallWellbeing = table.Column<int>(type: "integer", nullable: true),
                    SleepQuality = table.Column<int>(type: "integer", nullable: true),
                    SleepHours = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    DailyNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symptom_diary_sheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_symptom_diary_sheets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_bot_api_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_bot_api_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_bot_api_keys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "symptom_diary_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SymptomName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PainType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BodyLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    Triggers = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Relievers = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MedicationsTaken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symptom_diary_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_symptom_diary_entries_symptom_diary_sheets_SheetId",
                        column: x => x.SheetId,
                        principalTable: "symptom_diary_sheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_symptom_diary_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_symptom_diary_entries_Category",
                table: "symptom_diary_entries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_symptom_diary_entries_SheetId",
                table: "symptom_diary_entries",
                column: "SheetId");

            migrationBuilder.CreateIndex(
                name: "IX_symptom_diary_entries_UserId_Date",
                table: "symptom_diary_entries",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_symptom_diary_sheets_UserId_Date",
                table: "symptom_diary_sheets",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_bot_api_keys_KeyHash",
                table: "user_bot_api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_bot_api_keys_UserId_IsActive",
                table: "user_bot_api_keys",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "symptom_diary_entries");

            migrationBuilder.DropTable(
                name: "user_bot_api_keys");

            migrationBuilder.DropTable(
                name: "symptom_diary_sheets");
        }
    }
}
