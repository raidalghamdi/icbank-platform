using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icbank.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectProgressUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_progress_updates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    progress_percent = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    reported_by = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    reported_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_progress_updates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_progress_updates_portfolio_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "portfolio_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_progress_updates_project_reported_at",
                table: "project_progress_updates",
                columns: new[] { "project_id", "reported_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_projectprogressupdate_deleted_at",
                table: "project_progress_updates",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_progress_updates");
        }
    }
}
