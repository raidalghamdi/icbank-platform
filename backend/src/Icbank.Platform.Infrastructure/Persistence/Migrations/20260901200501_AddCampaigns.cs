using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icbank.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    objective = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    audience = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    owner = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    progress_percent = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    latest_update = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    reach_count = table.Column<int>(type: "int", nullable: false),
                    impressions_count = table.Column<int>(type: "int", nullable: false),
                    engagement_count = table.Column<int>(type: "int", nullable: false),
                    published_items = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campaign_channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campaign_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    published_items = table.Column<int>(type: "int", nullable: false),
                    reach_count = table.Column<int>(type: "int", nullable: false),
                    engagement_count = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_channels_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_deliverables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campaign_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    is_completed = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_deliverables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_deliverables_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_channels_campaign_sort",
                table: "campaign_channels",
                columns: new[] { "campaign_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_campaignchannel_deleted_at",
                table: "campaign_channels",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_deliverables_campaign_sort",
                table: "campaign_deliverables",
                columns: new[] { "campaign_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_campaigndeliverable_deleted_at",
                table: "campaign_deliverables",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_deleted_at",
                table: "campaigns",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_audience_status_sort",
                table: "campaigns",
                columns: new[] { "audience", "status", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_code",
                table: "campaigns",
                column: "code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_channels");

            migrationBuilder.DropTable(
                name: "campaign_deliverables");

            migrationBuilder.DropTable(
                name: "campaigns");
        }
    }
}
