using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icbank.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_year_activations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    activation_date = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tags_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    reach = table.Column<int>(type: "int", nullable: true),
                    engagement = table.Column<int>(type: "int", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_year_activations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "archive_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    body_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    occasion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    tone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    source_file = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    embedding_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archive_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brand_fonts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    font_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    font_file_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_fonts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brand_logos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    logo_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    transparent = table.Column<bool>(type: "bit", nullable: false),
                    default_width = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_logos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    report_data_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "design_templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    template_name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    canvas_width = table.Column<int>(type: "int", nullable: false),
                    canvas_height = table.Column<int>(type: "int", nullable: false),
                    background_panel_config_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    text_slots_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    logo_slots_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    thumbnail_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    prompt_hint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    extras_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_design_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gac_news_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    body_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    body_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    external_ref = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tags_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gac_news_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gac_publications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title_ar = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    original_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    file_size_bytes = table.Column<int>(type: "int", nullable: true),
                    page_count = table.Column<int>(type: "int", nullable: true),
                    tags_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source_domain = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gac_publications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gac_social_posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    external_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    content_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    content_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    post_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    media_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    media_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    metrics_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gac_social_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "generated_outputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    topic = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    model_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    output_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    archive_ref_ids_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    selected = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_outputs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "international_days",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    day_name_ar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    day_name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    annual_date = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    official_organizer = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    official_organizer_source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    history_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    suggestions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_searched_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_international_days", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_system = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "style_profile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tone_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    avg_paragraph_length = table.Column<float>(type: "real", nullable: true),
                    opener_patterns_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    closer_patterns_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recurring_keywords_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quote_usage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_style_profile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    azure_oid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    is_locked = table.Column<bool>(type: "bit", nullable: false),
                    failed_attempts = table.Column<int>(type: "int", nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    password_changed_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weekend_places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    maps_query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_weekend_places", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_year_activation_channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activation_id = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_year_activation_channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_year_activation_channels_ai_year_activations_activation_id",
                        column: x => x.activation_id,
                        principalTable: "ai_year_activations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_year_media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activation_id = table.Column<int>(type: "int", nullable: false),
                    object_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_ai_year_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_year_media_ai_year_activations_activation_id",
                        column: x => x.activation_id,
                        principalTable: "ai_year_activations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_year_metrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activation_id = table.Column<int>(type: "int", nullable: false),
                    metric_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    metric_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_year_metrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_year_metrics_ai_year_activations_activation_id",
                        column: x => x.activation_id,
                        principalTable: "ai_year_activations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "day_activations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    day_id = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: true),
                    entity_name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    activation_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    platform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    media_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    verified = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_day_activations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_day_activations_international_days_day_id",
                        column: x => x.day_id,
                        principalTable: "international_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "day_yearly_themes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    day_id = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    theme_ar = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    theme_en = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    theme_source_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_day_yearly_themes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_day_yearly_themes_international_days_day_id",
                        column: x => x.day_id,
                        principalTable: "international_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "intl_day_sources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    related_table = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    related_id = table.Column<int>(type: "int", nullable: false),
                    day_id = table.Column<int>(type: "int", nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    source_publisher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    accessed_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intl_day_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_intl_day_sources_international_days_day_id",
                        column: x => x.day_id,
                        principalTable: "international_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intl_search_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    day_id = table.Column<int>(type: "int", nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    searched_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intl_search_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_intl_search_history_international_days_day_id",
                        column: x => x.day_id,
                        principalTable: "international_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    page_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permissions_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    details_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activity_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "final_media_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    report_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    period_label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    date_from = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    date_to = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    prepared_by = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    beneficiary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    reference_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    classification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    issue_date = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    kpis_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    executive_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    top_news_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    timeline_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    digital_presence_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    editorial_tone_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    deep_analysis_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    regional_comparison_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recommendations_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    alerts_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quotes_appendix_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    methodology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sources_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source_items_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generated_by_user_id = table.Column<int>(type: "int", nullable: true),
                    generated_by_name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ai_model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    locked_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    content_sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    pdf_storage_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    view_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_final_media_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_final_media_reports_users_generated_by_user_id",
                        column: x => x.generated_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "generated_designs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    template_id = table.Column<int>(type: "int", nullable: true),
                    title_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    body_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    background_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    selected_logo_ids_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    final_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_by_user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_designs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_designs_design_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "design_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_generated_designs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "media_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    report_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    audience = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date_from = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    date_to = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    sources_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    executive_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    content_md = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    stats_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    overall_tone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    source_items_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generated_by_user_id = table.Column<int>(type: "int", nullable: true),
                    generated_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ai_model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_reports_users_generated_by_user_id",
                        column: x => x.generated_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prompt_frameworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    prompt_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    variables_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    example_input = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    example_output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tags_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recommended_model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_approved = table.Column<bool>(type: "bit", nullable: false),
                    usage_count = table.Column<int>(type: "int", nullable: false),
                    created_by_user_id = table.Column<int>(type: "int", nullable: true),
                    created_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_frameworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_frameworks_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    issue_no = table.Column<int>(type: "int", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    subtitle_ar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    month = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    cover_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    editor_letter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contributions_open_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    contributions_close_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    published_pdf_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    created_by_user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_issues_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_section_sla_defaults",
                columns: table => new
                {
                    section_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    sla_days = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "int", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_section_sla_defaults", x => x.section_type);
                    table.ForeignKey(
                        name: "FK_shorfah_section_sla_defaults_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_page_overrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    page_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    grant_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    created_by_user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_page_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_page_overrides_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_page_overrides_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_page_overrides_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_page_overrides_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    assigned_by = table.Column<int>(type: "int", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekend_drafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    weekend_date = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    model_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    content_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generated_by_user_id = table.Column<int>(type: "int", nullable: true),
                    approved_by_user_id = table.Column<int>(type: "int", nullable: true),
                    rejected_reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekend_drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekend_drafts_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_weekend_drafts_users_generated_by_user_id",
                        column: x => x.generated_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports_qa_queries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    query_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    wizard_answers_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    search_query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    final_report_id = table.Column<int>(type: "int", nullable: true),
                    result_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports_qa_queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reports_qa_queries_final_media_reports_final_report_id",
                        column: x => x.final_report_id,
                        principalTable: "final_media_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reports_qa_queries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    issue_id = table.Column<int>(type: "int", nullable: false),
                    parent_section_id = table.Column<int>(type: "int", nullable: true),
                    section_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    owner_user_id = table.Column<int>(type: "int", nullable: true),
                    owner_role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    include_in_pdf = table.Column<bool>(type: "bit", nullable: false),
                    auto_generate = table.Column<bool>(type: "bit", nullable: true),
                    generation_prompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    workflow_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    content_md = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    content_html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contributed_by_user_id = table.Column<int>(type: "int", nullable: true),
                    contributed_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    reviewed_by_user_id = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    review_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approved_by_user_id = table.Column<int>(type: "int", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sla_days = table.Column<int>(type: "int", nullable: true),
                    sla_starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    sla_deadline = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_sections_shorfah_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "shorfah_issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shorfah_sections_shorfah_sections_parent_section_id",
                        column: x => x.parent_section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_sections_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_sections_users_contributed_by_user_id",
                        column: x => x.contributed_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_sections_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_sections_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_assignments_shorfah_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shorfah_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    issue_id = table.Column<int>(type: "int", nullable: true),
                    section_id = table.Column<int>(type: "int", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_notifications_shorfah_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "shorfah_issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_notifications_shorfah_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_section_media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    media_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    media_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    caption_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_section_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_section_media_shorfah_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_section_permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    role_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    permission = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_section_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_section_permissions_shorfah_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shorfah_section_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_workflow_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    actor_user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    from_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_workflow_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_workflow_log_shorfah_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shorfah_workflow_log_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shorfah_reminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_id = table.Column<int>(type: "int", nullable: false),
                    assignment_id = table.Column<int>(type: "int", nullable: true),
                    recipient_user_id = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    reminder_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shorfah_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shorfah_reminders_shorfah_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "shorfah_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shorfah_reminders_shorfah_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "shorfah_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shorfah_reminders_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_created_at",
                table: "activity_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_user_id",
                table: "activity_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_deleted_at",
                table: "activity_logs",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_ai_year_activation_channels_activation_id",
                table: "ai_year_activation_channels",
                column: "activation_id");

            migrationBuilder.CreateIndex(
                name: "ix_aiyearactivationchannel_deleted_at",
                table: "ai_year_activation_channels",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_ai_year_activation_channels_activation_channel",
                table: "ai_year_activation_channels",
                columns: new[] { "activation_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_year_activations_year_month",
                table: "ai_year_activations",
                columns: new[] { "year", "month" });

            migrationBuilder.CreateIndex(
                name: "ix_aiyearactivation_deleted_at",
                table: "ai_year_activations",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_ai_year_media_activation_id",
                table: "ai_year_media",
                column: "activation_id");

            migrationBuilder.CreateIndex(
                name: "ix_aiyearmedia_deleted_at",
                table: "ai_year_media",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_ai_year_metrics_activation_id",
                table: "ai_year_metrics",
                column: "activation_id");

            migrationBuilder.CreateIndex(
                name: "ix_aiyearmetric_deleted_at",
                table: "ai_year_metrics",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_archiveentry_deleted_at",
                table: "archive_entries",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_brandfont_deleted_at",
                table: "brand_fonts",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_brand_fonts_single_default",
                table: "brand_fonts",
                column: "is_default",
                unique: true,
                filter: "[is_default] = 1");

            migrationBuilder.CreateIndex(
                name: "ix_brandlogo_deleted_at",
                table: "brand_logos",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_dailyreport_deleted_at",
                table: "daily_reports",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_daily_reports_report_date",
                table: "daily_reports",
                column: "report_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_day_activations_day_id",
                table: "day_activations",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "ix_dayactivation_deleted_at",
                table: "day_activations",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_dayyearlytheme_deleted_at",
                table: "day_yearly_themes",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_day_yearly_themes_day_year",
                table: "day_yearly_themes",
                columns: new[] { "day_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_design_templates_category",
                table: "design_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_designtemplate_deleted_at",
                table: "design_templates",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_final_media_reports_generated_by_user_id",
                table: "final_media_reports",
                column: "generated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_finalmediareport_deleted_at",
                table: "final_media_reports",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_final_media_reports_report_number",
                table: "final_media_reports",
                column: "report_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gac_news_items_kind",
                table: "gac_news_items",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "ix_gac_news_items_published_at",
                table: "gac_news_items",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "ix_gacnewsitem_deleted_at",
                table: "gac_news_items",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_gac_publications_category",
                table: "gac_publications",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_gac_publications_display_order",
                table: "gac_publications",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "ix_gac_publications_status",
                table: "gac_publications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_gacpublication_deleted_at",
                table: "gac_publications",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_gac_social_posts_posted_at",
                table: "gac_social_posts",
                column: "posted_at");

            migrationBuilder.CreateIndex(
                name: "ix_gacsocialpost_deleted_at",
                table: "gac_social_posts",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_gac_social_posts_platform_external_id",
                table: "gac_social_posts",
                columns: new[] { "platform", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_generated_designs_created_by_user_id",
                table: "generated_designs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_generated_designs_template_id",
                table: "generated_designs",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_generateddesign_deleted_at",
                table: "generated_designs",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_generated_outputs_selected",
                table: "generated_outputs",
                column: "selected");

            migrationBuilder.CreateIndex(
                name: "ix_generatedoutput_deleted_at",
                table: "generated_outputs",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_international_days_day_name_ar",
                table: "international_days",
                column: "day_name_ar");

            migrationBuilder.CreateIndex(
                name: "ix_internationalday_deleted_at",
                table: "international_days",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_intl_day_sources_day_id",
                table: "intl_day_sources",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "ix_intl_day_sources_related",
                table: "intl_day_sources",
                columns: new[] { "related_table", "related_id" });

            migrationBuilder.CreateIndex(
                name: "ix_intldaysource_deleted_at",
                table: "intl_day_sources",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_intl_search_history_day_id",
                table: "intl_search_history",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "ix_intl_search_history_searched_at",
                table: "intl_search_history",
                column: "searched_at");

            migrationBuilder.CreateIndex(
                name: "ix_intlsearchhistory_deleted_at",
                table: "intl_search_history",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_media_reports_date_range",
                table: "media_reports",
                columns: new[] { "date_from", "date_to" });

            migrationBuilder.CreateIndex(
                name: "ix_media_reports_generated_by_user_id",
                table: "media_reports",
                column: "generated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mediareport_deleted_at",
                table: "media_reports",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_page_deleted_at",
                table: "pages",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_pages_slug",
                table: "pages",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permission_deleted_at",
                table: "permissions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_permissions_name",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prompt_frameworks_category",
                table: "prompt_frameworks",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_prompt_frameworks_created_by_user_id",
                table: "prompt_frameworks",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_promptframework_deleted_at",
                table: "prompt_frameworks",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_reports_qa_queries_final_report_id",
                table: "reports_qa_queries",
                column: "final_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_qa_queries_user_id",
                table: "reports_qa_queries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reportsqaquery_deleted_at",
                table: "reports_qa_queries",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_page_id",
                table: "role_permissions",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_rolepermission_deleted_at",
                table: "role_permissions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "role_page_perm_idx",
                table: "role_permissions",
                columns: new[] { "role_id", "page_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_deleted_at",
                table: "roles",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_assignments_section_id",
                table: "shorfah_assignments",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_assignments_user_id",
                table: "shorfah_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahassignment_deleted_at",
                table: "shorfah_assignments",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_shorfah_assignments_section_user",
                table: "shorfah_assignments",
                columns: new[] { "section_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_issues_created_by_user_id",
                table: "shorfah_issues",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_issues_status",
                table: "shorfah_issues",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahissue_deleted_at",
                table: "shorfah_issues",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_shorfah_issues_issue_no",
                table: "shorfah_issues",
                column: "issue_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_notifications_issue_id",
                table: "shorfah_notifications",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_notifications_section_id",
                table: "shorfah_notifications",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_notifications_user_id",
                table: "shorfah_notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_notifications_user_unread",
                table: "shorfah_notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_shorfahnotification_deleted_at",
                table: "shorfah_notifications",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_reminders_assignment_id",
                table: "shorfah_reminders",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_reminders_recipient_user_id",
                table: "shorfah_reminders",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_reminders_section_id",
                table: "shorfah_reminders",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahreminder_deleted_at",
                table: "shorfah_reminders",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_section_media_section_id",
                table: "shorfah_section_media",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahsectionmedia_deleted_at",
                table: "shorfah_section_media",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_section_permissions_section_id",
                table: "shorfah_section_permissions",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_section_permissions_user_id",
                table: "shorfah_section_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahsectionpermission_deleted_at",
                table: "shorfah_section_permissions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_shorfah_section_sla_defaults_updated_by_user_id",
                table: "shorfah_section_sla_defaults",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shorfah_sections_approved_by_user_id",
                table: "shorfah_sections",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shorfah_sections_contributed_by_user_id",
                table: "shorfah_sections",
                column: "contributed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_sections_issue_id",
                table: "shorfah_sections",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_sections_owner_user_id",
                table: "shorfah_sections",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_sections_parent_section_id",
                table: "shorfah_sections",
                column: "parent_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_shorfah_sections_reviewed_by_user_id",
                table: "shorfah_sections",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_sections_sla_deadline",
                table: "shorfah_sections",
                column: "sla_deadline");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_sections_workflow_status",
                table: "shorfah_sections",
                column: "workflow_status");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahsection_deleted_at",
                table: "shorfah_sections",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_workflow_log_actor_user_id",
                table: "shorfah_workflow_log",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfah_workflow_log_section_id",
                table: "shorfah_workflow_log",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_shorfahworkflowlog_deleted_at",
                table: "shorfah_workflow_log",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_styleprofile_deleted_at",
                table: "style_profile",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_systemsetting_deleted_at",
                table: "system_settings",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_page_overrides_created_by_user_id",
                table: "user_page_overrides",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_page_overrides_page_id",
                table: "user_page_overrides",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_page_overrides_permission_id",
                table: "user_page_overrides",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_userpageoverride_deleted_at",
                table: "user_page_overrides",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_user_page_overrides_user_page_permission",
                table: "user_page_overrides",
                columns: new[] { "user_id", "page_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by",
                table: "user_roles",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_userrole_deleted_at",
                table: "user_roles",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "user_role_idx",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_deleted_at",
                table: "users",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_users_azure_oid",
                table: "users",
                column: "azure_oid",
                unique: true,
                filter: "[azure_oid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekend_drafts_approved_by_user_id",
                table: "weekend_drafts",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_weekend_drafts_generated_by_user_id",
                table: "weekend_drafts",
                column: "generated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekend_drafts_status",
                table: "weekend_drafts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_weekend_drafts_weekend_date",
                table: "weekend_drafts",
                column: "weekend_date");

            migrationBuilder.CreateIndex(
                name: "ix_weekenddraft_deleted_at",
                table: "weekend_drafts",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_weekend_places_is_active",
                table: "weekend_places",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_weekendplace_deleted_at",
                table: "weekend_places",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_logs");

            migrationBuilder.DropTable(
                name: "ai_year_activation_channels");

            migrationBuilder.DropTable(
                name: "ai_year_media");

            migrationBuilder.DropTable(
                name: "ai_year_metrics");

            migrationBuilder.DropTable(
                name: "archive_entries");

            migrationBuilder.DropTable(
                name: "brand_fonts");

            migrationBuilder.DropTable(
                name: "brand_logos");

            migrationBuilder.DropTable(
                name: "daily_reports");

            migrationBuilder.DropTable(
                name: "day_activations");

            migrationBuilder.DropTable(
                name: "day_yearly_themes");

            migrationBuilder.DropTable(
                name: "gac_news_items");

            migrationBuilder.DropTable(
                name: "gac_publications");

            migrationBuilder.DropTable(
                name: "gac_social_posts");

            migrationBuilder.DropTable(
                name: "generated_designs");

            migrationBuilder.DropTable(
                name: "generated_outputs");

            migrationBuilder.DropTable(
                name: "intl_day_sources");

            migrationBuilder.DropTable(
                name: "intl_search_history");

            migrationBuilder.DropTable(
                name: "media_reports");

            migrationBuilder.DropTable(
                name: "prompt_frameworks");

            migrationBuilder.DropTable(
                name: "reports_qa_queries");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "shorfah_notifications");

            migrationBuilder.DropTable(
                name: "shorfah_reminders");

            migrationBuilder.DropTable(
                name: "shorfah_section_media");

            migrationBuilder.DropTable(
                name: "shorfah_section_permissions");

            migrationBuilder.DropTable(
                name: "shorfah_section_sla_defaults");

            migrationBuilder.DropTable(
                name: "shorfah_workflow_log");

            migrationBuilder.DropTable(
                name: "style_profile");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "user_page_overrides");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "weekend_drafts");

            migrationBuilder.DropTable(
                name: "weekend_places");

            migrationBuilder.DropTable(
                name: "ai_year_activations");

            migrationBuilder.DropTable(
                name: "design_templates");

            migrationBuilder.DropTable(
                name: "international_days");

            migrationBuilder.DropTable(
                name: "final_media_reports");

            migrationBuilder.DropTable(
                name: "shorfah_assignments");

            migrationBuilder.DropTable(
                name: "pages");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "shorfah_sections");

            migrationBuilder.DropTable(
                name: "shorfah_issues");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
