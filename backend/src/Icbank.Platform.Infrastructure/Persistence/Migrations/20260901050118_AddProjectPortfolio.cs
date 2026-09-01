using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icbank.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portfolio_projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    category = table.Column<int>(type: "int", nullable: false),
                    stage = table.Column<int>(type: "int", nullable: false),
                    owner = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    progress_percent = table.Column<int>(type: "int", nullable: false),
                    team_size = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    latest_update = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
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
                    table.PrimaryKey("PK_portfolio_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_project_milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_milestones_portfolio_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "portfolio_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_projects_category_sort",
                table: "portfolio_projects",
                columns: new[] { "category", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_projects_code",
                table: "portfolio_projects",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_portfolioproject_deleted_at",
                table: "portfolio_projects",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_project_milestones_project_sort",
                table: "project_milestones",
                columns: new[] { "project_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_projectmilestone_deleted_at",
                table: "project_milestones",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_milestones");

            migrationBuilder.DropTable(
                name: "portfolio_projects");
        }
    }
}
