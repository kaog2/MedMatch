using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ModerationNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recommendations_users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_clinics",
                columns: table => new
                {
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendation_clinics", x => new { x.RecommendationId, x.ClinicId });
                    table.ForeignKey(
                        name: "FK_recommendation_clinics_clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recommendation_clinics_recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_diagnosis_tags",
                columns: table => new
                {
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendation_diagnosis_tags", x => new { x.RecommendationId, x.DiagnosisTagId });
                    table.ForeignKey(
                        name: "FK_recommendation_diagnosis_tags_diagnosis_tags_DiagnosisTagId",
                        column: x => x.DiagnosisTagId,
                        principalTable: "diagnosis_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recommendation_diagnosis_tags_recommendations_Recommendatio~",
                        column: x => x.RecommendationId,
                        principalTable: "recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recommendation_clinics_ClinicId",
                table: "recommendation_clinics",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_recommendation_diagnosis_tags_DiagnosisTagId",
                table: "recommendation_diagnosis_tags",
                column: "DiagnosisTagId");

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_AuthorUserId",
                table: "recommendations",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_Status_CreatedAt",
                table: "recommendations",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recommendation_clinics");

            migrationBuilder.DropTable(
                name: "recommendation_diagnosis_tags");

            migrationBuilder.DropTable(
                name: "recommendations");
        }
    }
}
