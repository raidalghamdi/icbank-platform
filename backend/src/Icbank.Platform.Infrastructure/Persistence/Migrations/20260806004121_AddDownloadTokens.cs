using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icbank.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "download_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    token_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    resource_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    resource_id = table.Column<int>(type: "int", nullable: false),
                    issued_to_user_id = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_download_tokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_download_tokens_expires_at",
                table: "download_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_download_tokens_resource",
                table: "download_tokens",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_download_tokens_token_hash",
                table: "download_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_downloadtoken_deleted_at",
                table: "download_tokens",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "download_tokens");
        }
    }
}
