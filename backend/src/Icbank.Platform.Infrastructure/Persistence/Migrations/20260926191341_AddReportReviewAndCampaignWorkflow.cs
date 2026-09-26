using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icbank.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportReviewAndCampaignWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "final_media_reports",
                type: "datetimeoffset(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_by_name",
                table: "final_media_reports",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "layout_json",
                table: "final_media_reports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<bool>(
                name: "is_user_managed",
                table: "campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "key_messages",
                table: "campaigns",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "plan_json",
                table: "campaigns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "planned_channels",
                table: "campaigns",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "source_request_id",
                table: "campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stage",
                table: "campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "target_audience",
                table: "campaigns",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "team_members",
                table: "campaigns",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateTable(
                name: "campaign_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    requesting_department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    objective = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    target_audience = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    proposed_start = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    proposed_end = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    key_messages = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    support_types = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    submitted_by_user_id = table.Column<int>(type: "int", nullable: false),
                    submitted_by_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    review_note = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    reviewed_by_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    campaign_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_requests_status_created",
                table: "campaign_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_requests_submitted_by",
                table: "campaign_requests",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaignrequest_deleted_at",
                table: "campaign_requests",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_requests");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "final_media_reports");

            migrationBuilder.DropColumn(
                name: "approved_by_name",
                table: "final_media_reports");

            migrationBuilder.DropColumn(
                name: "layout_json",
                table: "final_media_reports");

            migrationBuilder.DropColumn(
                name: "is_user_managed",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "key_messages",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "plan_json",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "planned_channels",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "source_request_id",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "stage",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "target_audience",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "team_members",
                table: "campaigns");
        }
    }
}
