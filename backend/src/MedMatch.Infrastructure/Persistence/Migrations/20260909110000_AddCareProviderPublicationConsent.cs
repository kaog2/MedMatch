using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MedMatch.Infrastructure.Persistence;

#nullable disable

namespace MedMatch.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MedMatchDbContext))]
[Migration("20260909110000_AddCareProviderPublicationConsent")]
public partial class AddCareProviderPublicationConsent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PublicWebsiteUrl", table: "clinics", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "PublicationConsentGranted", table: "clinics", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "PublicationConsentAt", table: "clinics", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PublicWebsiteUrl", table: "clinics");
        migrationBuilder.DropColumn(name: "PublicationConsentGranted", table: "clinics");
        migrationBuilder.DropColumn(name: "PublicationConsentAt", table: "clinics");
    }
}