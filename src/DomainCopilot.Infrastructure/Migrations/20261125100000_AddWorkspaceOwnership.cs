using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261125100000_AddWorkspaceOwnership")]
public partial class AddWorkspaceOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CreatedByUserId", table: "ClinicalCases", type: "nvarchar(200)",
            maxLength: 200, nullable: false, defaultValue: "system");

        migrationBuilder.AddColumn<string>(
            name: "CreatedByUserId", table: "CopilotRuns", type: "nvarchar(200)",
            maxLength: 200, nullable: false, defaultValue: "system");
        migrationBuilder.AddColumn<string>(
            name: "Question", table: "CopilotRuns", type: "nvarchar(max)",
            maxLength: 8000, nullable: false, defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "UploadedByUserId", table: "SourceDocuments", type: "nvarchar(200)",
            maxLength: 200, nullable: false, defaultValue: "system");

        migrationBuilder.CreateIndex("IX_ClinicalCases_CreatedByUserId_CreatedAtUtc",
            "ClinicalCases", new[] { "CreatedByUserId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_CopilotRuns_CreatedByUserId_CreatedAtUtc",
            "CopilotRuns", new[] { "CreatedByUserId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_SourceDocuments_UploadedByUserId_CreatedAtUtc",
            "SourceDocuments", new[] { "UploadedByUserId", "CreatedAtUtc" });
        migrationBuilder.Sql("UPDATE dbo.CopilotRuns SET Question = N'Legacy run; original question was not stored.' WHERE Question = N'';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_ClinicalCases_CreatedByUserId_CreatedAtUtc", "ClinicalCases");
        migrationBuilder.DropIndex("IX_CopilotRuns_CreatedByUserId_CreatedAtUtc", "CopilotRuns");
        migrationBuilder.DropIndex("IX_SourceDocuments_UploadedByUserId_CreatedAtUtc", "SourceDocuments");
        migrationBuilder.DropColumn("CreatedByUserId", "ClinicalCases");
        migrationBuilder.DropColumn("CreatedByUserId", "CopilotRuns");
        migrationBuilder.DropColumn("Question", "CopilotRuns");
        migrationBuilder.DropColumn("UploadedByUserId", "SourceDocuments");
    }
}
