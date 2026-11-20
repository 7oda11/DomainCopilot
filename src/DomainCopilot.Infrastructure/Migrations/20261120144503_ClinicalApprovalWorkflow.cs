using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClinicalApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalNoteDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteDrafts_CopilotRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "CopilotRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovedContent = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_ClinicalNoteDrafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "ClinicalNoteDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNoteCitations",
                columns: table => new
                {
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    QuotedText = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteCitations", x => new { x.DraftId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_ClinicalNoteCitations_ClinicalNoteDrafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "ClinicalNoteDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_DraftId",
                table: "ApprovalRecords",
                column: "DraftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteDrafts_RunId",
                table: "ClinicalNoteDrafts",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRecords");

            migrationBuilder.DropTable(
                name: "ClinicalNoteCitations");

            migrationBuilder.DropTable(
                name: "ClinicalNoteDrafts");
        }
    }
}
