using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SymptomsToFreeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert the symptoms array column to free text, preserving values.
            migrationBuilder.Sql(
                "ALTER TABLE patient_profiles ALTER COLUMN \"Symptoms\" TYPE text USING array_to_string(\"Symptoms\", ', ');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE patient_profiles ALTER COLUMN \"Symptoms\" TYPE text[] USING CASE WHEN \"Symptoms\" = '' THEN ARRAY[]::text[] ELSE string_to_array(\"Symptoms\", ', ') END;");
        }
    }
}
