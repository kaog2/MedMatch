using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisTagsAndMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diagnosis_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diagnosis_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "match_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedDiagnoses = table.Column<string[]>(type: "text[]", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_diagnosis_tags",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_diagnosis_tags", x => new { x.UserId, x.DiagnosisTagId });
                    table.ForeignKey(
                        name: "FK_patient_diagnosis_tags_diagnosis_tags_DiagnosisTagId",
                        column: x => x.DiagnosisTagId,
                        principalTable: "diagnosis_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_patient_diagnosis_tags_patient_profiles_UserId",
                        column: x => x.UserId,
                        principalTable: "patient_profiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_tags_Name",
                table: "diagnosis_tags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_tags_Slug",
                table: "diagnosis_tags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_notifications_UserId_IsRead",
                table: "match_notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_match_notifications_UserId_MatchedUserId",
                table: "match_notifications",
                columns: new[] { "UserId", "MatchedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_diagnosis_tags_DiagnosisTagId",
                table: "patient_diagnosis_tags",
                column: "DiagnosisTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_notifications");

            migrationBuilder.DropTable(
                name: "patient_diagnosis_tags");

            migrationBuilder.DropTable(
                name: "diagnosis_tags");
        }
    }
}
