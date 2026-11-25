using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261124090000_AddWorkflowTraceEntries")]
public partial class AddWorkflowTraceEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "WorkflowTraceEntries", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            AgentName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            Outcome = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
            ProviderId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            IsDegraded = table.Column<bool>(type: "bit", nullable: false),
            ElapsedMilliseconds = table.Column<long>(type: "bigint", nullable: false),
            InputTokens = table.Column<int>(type: "int", nullable: true),
            OutputTokens = table.Column<int>(type: "int", nullable: true),
            EstimatedCostUsd = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_WorkflowTraceEntries", x => x.Id);
            table.ForeignKey("FK_WorkflowTraceEntries_CopilotRuns_RunId", x => x.RunId,
                "CopilotRuns", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_WorkflowTraceEntries_RunId_CreatedAtUtc",
            "WorkflowTraceEntries", new[] { "RunId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("WorkflowTraceEntries");
}
