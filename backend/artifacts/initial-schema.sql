IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [ai_year_activations] (
        [Id] int NOT NULL IDENTITY,
        [title] nvarchar(300) NOT NULL,
        [month] int NOT NULL,
        [year] int NOT NULL,
        [activation_date] nvarchar(50) NULL,
        [type] nvarchar(50) NOT NULL,
        [description] nvarchar(max) NULL,
        [tags_json] nvarchar(max) NOT NULL,
        [status] nvarchar(20) NOT NULL,
        [reach] int NULL,
        [engagement] int NULL,
        [notes] nvarchar(max) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_ai_year_activations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [archive_entries] (
        [Id] int NOT NULL IDENTITY,
        [title] nvarchar(300) NOT NULL,
        [body_text] nvarchar(max) NOT NULL,
        [date] datetimeoffset(3) NULL,
        [occasion] nvarchar(200) NULL,
        [tone] nvarchar(100) NULL,
        [source_file] nvarchar(260) NULL,
        [embedding_json] nvarchar(max) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_archive_entries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [brand_fonts] (
        [Id] int NOT NULL IDENTITY,
        [font_name] nvarchar(200) NOT NULL,
        [font_file_url] nvarchar(500) NOT NULL,
        [is_default] bit NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_brand_fonts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [brand_logos] (
        [Id] int NOT NULL IDENTITY,
        [logo_name] nvarchar(200) NOT NULL,
        [file_url] nvarchar(500) NOT NULL,
        [transparent] bit NOT NULL,
        [default_width] int NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_brand_logos] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [daily_reports] (
        [Id] int NOT NULL IDENTITY,
        [report_date] date NOT NULL,
        [report_data_json] nvarchar(max) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_daily_reports] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [design_templates] (
        [Id] int NOT NULL IDENTITY,
        [template_name_ar] nvarchar(200) NOT NULL,
        [category] nvarchar(100) NOT NULL,
        [canvas_width] int NOT NULL,
        [canvas_height] int NOT NULL,
        [background_panel_config_json] nvarchar(max) NULL,
        [text_slots_json] nvarchar(max) NOT NULL,
        [logo_slots_json] nvarchar(max) NOT NULL,
        [thumbnail_url] nvarchar(500) NULL,
        [prompt_hint] nvarchar(max) NULL,
        [extras_json] nvarchar(max) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_design_templates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [gac_news_items] (
        [Id] int NOT NULL IDENTITY,
        [kind] nvarchar(20) NOT NULL,
        [title_ar] nvarchar(400) NOT NULL,
        [title_en] nvarchar(400) NULL,
        [body_ar] nvarchar(max) NULL,
        [body_en] nvarchar(max) NULL,
        [category] nvarchar(30) NULL,
        [source_url] nvarchar(500) NULL,
        [image_url] nvarchar(500) NULL,
        [published_at] datetimeoffset(3) NULL,
        [external_ref] nvarchar(100) NULL,
        [tags_json] nvarchar(max) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_gac_news_items] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [gac_publications] (
        [Id] int NOT NULL IDENTITY,
        [title_ar] nvarchar(400) NOT NULL,
        [title_en] nvarchar(400) NULL,
        [category] nvarchar(30) NOT NULL,
        [language] nvarchar(10) NOT NULL,
        [description_ar] nvarchar(max) NULL,
        [description_en] nvarchar(max) NULL,
        [version] nvarchar(50) NULL,
        [published_at] datetimeoffset(3) NULL,
        [original_url] nvarchar(500) NULL,
        [file_url] nvarchar(500) NOT NULL,
        [file_size_bytes] int NULL,
        [page_count] int NULL,
        [tags_json] nvarchar(max) NOT NULL,
        [source_domain] nvarchar(20) NOT NULL,
        [status] nvarchar(20) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_gac_publications] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [gac_social_posts] (
        [Id] int NOT NULL IDENTITY,
        [platform] nvarchar(20) NOT NULL,
        [external_id] nvarchar(200) NOT NULL,
        [content_ar] nvarchar(max) NULL,
        [content_en] nvarchar(max) NULL,
        [post_url] nvarchar(500) NOT NULL,
        [media_url] nvarchar(500) NULL,
        [media_type] nvarchar(10) NOT NULL,
        [posted_at] datetimeoffset(3) NULL,
        [metrics_json] nvarchar(max) NULL,
        [account] nvarchar(100) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_gac_social_posts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [generated_outputs] (
        [Id] int NOT NULL IDENTITY,
        [topic] nvarchar(300) NOT NULL,
        [model_name] nvarchar(50) NOT NULL,
        [output_text] nvarchar(max) NOT NULL,
        [archive_ref_ids_json] nvarchar(max) NOT NULL,
        [selected] bit NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_generated_outputs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [international_days] (
        [Id] int NOT NULL IDENTITY,
        [day_name_ar] nvarchar(300) NOT NULL,
        [day_name_en] nvarchar(300) NULL,
        [annual_date] nvarchar(50) NULL,
        [category] nvarchar(100) NULL,
        [official_organizer] nvarchar(300) NULL,
        [official_organizer_source] nvarchar(500) NULL,
        [history_summary] nvarchar(max) NULL,
        [history_source] nvarchar(500) NULL,
        [suggestions_json] nvarchar(max) NOT NULL,
        [last_searched_at] datetimeoffset(3) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_international_days] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [pages] (
        [Id] int NOT NULL IDENTITY,
        [slug] nvarchar(100) NOT NULL,
        [name_ar] nvarchar(200) NOT NULL,
        [icon] nvarchar(100) NULL,
        [sort_order] int NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_pages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [permissions] (
        [Id] int NOT NULL IDENTITY,
        [name] nvarchar(30) NOT NULL,
        [name_ar] nvarchar(100) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [roles] (
        [Id] int NOT NULL IDENTITY,
        [name] nvarchar(100) NOT NULL,
        [name_ar] nvarchar(200) NOT NULL,
        [description] nvarchar(1000) NULL,
        [is_system] bit NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [style_profile] (
        [Id] int NOT NULL IDENTITY,
        [tone_summary] nvarchar(max) NULL,
        [avg_paragraph_length] real NULL,
        [opener_patterns_json] nvarchar(max) NOT NULL,
        [closer_patterns_json] nvarchar(max) NOT NULL,
        [recurring_keywords_json] nvarchar(max) NOT NULL,
        [quote_usage] nvarchar(20) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_style_profile] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [system_settings] (
        [Id] int NOT NULL IDENTITY,
        [key] nvarchar(150) NOT NULL,
        [value] nvarchar(max) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_system_settings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [users] (
        [Id] int NOT NULL IDENTITY,
        [email] nvarchar(256) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [title] nvarchar(200) NULL,
        [department] nvarchar(200) NULL,
        [password_hash] nvarchar(512) NULL,
        [azure_oid] nvarchar(100) NULL,
        [is_active] bit NOT NULL,
        [is_locked] bit NOT NULL,
        [failed_attempts] int NOT NULL,
        [last_login] datetime2(3) NULL,
        [password_changed_at] datetime2(3) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [weekend_places] (
        [Id] int NOT NULL IDENTITY,
        [name] nvarchar(300) NOT NULL,
        [description] nvarchar(max) NOT NULL,
        [image_url] nvarchar(500) NULL,
        [city] nvarchar(100) NOT NULL,
        [maps_query] nvarchar(500) NULL,
        [is_active] bit NOT NULL,
        [sort_order] int NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_weekend_places] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [ai_year_activation_channels] (
        [Id] int NOT NULL IDENTITY,
        [activation_id] int NOT NULL,
        [channel] nvarchar(50) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_ai_year_activation_channels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ai_year_activation_channels_ai_year_activations_activation_id] FOREIGN KEY ([activation_id]) REFERENCES [ai_year_activations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [ai_year_media] (
        [Id] int NOT NULL IDENTITY,
        [activation_id] int NOT NULL,
        [object_path] nvarchar(500) NOT NULL,
        [file_name] nvarchar(260) NULL,
        [content_type] nvarchar(100) NULL,
        [sort_order] int NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_ai_year_media] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ai_year_media_ai_year_activations_activation_id] FOREIGN KEY ([activation_id]) REFERENCES [ai_year_activations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [ai_year_metrics] (
        [Id] int NOT NULL IDENTITY,
        [activation_id] int NOT NULL,
        [metric_key] nvarchar(100) NOT NULL,
        [metric_value] nvarchar(500) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_ai_year_metrics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ai_year_metrics_ai_year_activations_activation_id] FOREIGN KEY ([activation_id]) REFERENCES [ai_year_activations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [day_activations] (
        [Id] int NOT NULL IDENTITY,
        [day_id] int NOT NULL,
        [year] int NULL,
        [entity_name] nvarchar(300) NULL,
        [entity_type] nvarchar(100) NULL,
        [activation_type] nvarchar(100) NULL,
        [platform] nvarchar(100) NULL,
        [description] nvarchar(max) NULL,
        [media_url] nvarchar(500) NULL,
        [source_url] nvarchar(500) NULL,
        [country] nvarchar(100) NULL,
        [verified] bit NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_day_activations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_day_activations_international_days_day_id] FOREIGN KEY ([day_id]) REFERENCES [international_days] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [day_yearly_themes] (
        [Id] int NOT NULL IDENTITY,
        [day_id] int NOT NULL,
        [year] int NOT NULL,
        [theme_ar] nvarchar(400) NULL,
        [theme_en] nvarchar(400) NULL,
        [theme_source_url] nvarchar(500) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_day_yearly_themes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_day_yearly_themes_international_days_day_id] FOREIGN KEY ([day_id]) REFERENCES [international_days] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [intl_day_sources] (
        [Id] int NOT NULL IDENTITY,
        [related_table] nvarchar(100) NOT NULL,
        [related_id] int NOT NULL,
        [day_id] int NULL,
        [source_url] nvarchar(500) NULL,
        [source_title] nvarchar(400) NULL,
        [source_publisher] nvarchar(200) NULL,
        [accessed_at] datetimeoffset(3) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_intl_day_sources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_intl_day_sources_international_days_day_id] FOREIGN KEY ([day_id]) REFERENCES [international_days] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [intl_search_history] (
        [Id] int NOT NULL IDENTITY,
        [query] nvarchar(500) NOT NULL,
        [day_id] int NULL,
        [ip_address] nvarchar(45) NULL,
        [searched_at] datetimeoffset(3) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_intl_search_history] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_intl_search_history_international_days_day_id] FOREIGN KEY ([day_id]) REFERENCES [international_days] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [role_permissions] (
        [Id] int NOT NULL IDENTITY,
        [role_id] int NOT NULL,
        [page_id] int NOT NULL,
        [permission_id] int NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_role_permissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_role_permissions_pages_page_id] FOREIGN KEY ([page_id]) REFERENCES [pages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_role_permissions_permissions_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_role_permissions_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [activity_logs] (
        [Id] int NOT NULL IDENTITY,
        [user_id] int NULL,
        [action] nvarchar(100) NOT NULL,
        [entity_type] nvarchar(100) NULL,
        [entity_id] nvarchar(100) NULL,
        [details_json] nvarchar(max) NULL,
        [ip_address] nvarchar(45) NULL,
        [user_agent] nvarchar(512) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_activity_logs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_activity_logs_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [final_media_reports] (
        [Id] int NOT NULL IDENTITY,
        [report_number] nvarchar(50) NOT NULL,
        [title] nvarchar(300) NOT NULL,
        [report_type] nvarchar(20) NOT NULL,
        [period_label] nvarchar(200) NOT NULL,
        [date_from] datetimeoffset(3) NOT NULL,
        [date_to] datetimeoffset(3) NOT NULL,
        [prepared_by] nvarchar(300) NULL,
        [beneficiary] nvarchar(300) NULL,
        [reference_number] nvarchar(100) NULL,
        [classification] nvarchar(200) NULL,
        [issue_date] datetimeoffset(3) NOT NULL,
        [kpis_json] nvarchar(max) NOT NULL,
        [executive_summary] nvarchar(max) NULL,
        [top_news_json] nvarchar(max) NOT NULL,
        [timeline_json] nvarchar(max) NOT NULL,
        [digital_presence_json] nvarchar(max) NOT NULL,
        [editorial_tone_json] nvarchar(max) NOT NULL,
        [deep_analysis_json] nvarchar(max) NOT NULL,
        [regional_comparison_json] nvarchar(max) NOT NULL,
        [recommendations_json] nvarchar(max) NOT NULL,
        [alerts_json] nvarchar(max) NOT NULL,
        [quotes_appendix_json] nvarchar(max) NOT NULL,
        [methodology] nvarchar(max) NULL,
        [sources_json] nvarchar(max) NOT NULL,
        [source_items_json] nvarchar(max) NOT NULL,
        [generated_by_user_id] int NULL,
        [generated_by_name] nvarchar(300) NULL,
        [ai_model] nvarchar(100) NULL,
        [status] nvarchar(20) NOT NULL,
        [locked_at] datetimeoffset(3) NOT NULL,
        [content_sha256] nvarchar(64) NOT NULL,
        [pdf_storage_key] nvarchar(500) NULL,
        [view_count] int NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_final_media_reports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_final_media_reports_users_generated_by_user_id] FOREIGN KEY ([generated_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [generated_designs] (
        [Id] int NOT NULL IDENTITY,
        [template_id] int NULL,
        [title_text] nvarchar(max) NULL,
        [body_text] nvarchar(max) NULL,
        [background_image_url] nvarchar(500) NULL,
        [selected_logo_ids_json] nvarchar(max) NOT NULL,
        [final_image_url] nvarchar(500) NULL,
        [department] nvarchar(200) NULL,
        [created_by_user_id] int NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_generated_designs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_generated_designs_design_templates_template_id] FOREIGN KEY ([template_id]) REFERENCES [design_templates] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_generated_designs_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [media_reports] (
        [Id] int NOT NULL IDENTITY,
        [title] nvarchar(300) NOT NULL,
        [report_type] nvarchar(20) NOT NULL,
        [audience] nvarchar(20) NOT NULL,
        [date_from] datetimeoffset(3) NOT NULL,
        [date_to] datetimeoffset(3) NOT NULL,
        [sources_json] nvarchar(max) NOT NULL,
        [executive_summary] nvarchar(max) NULL,
        [content_md] nvarchar(max) NOT NULL,
        [stats_json] nvarchar(max) NULL,
        [overall_tone] nvarchar(100) NULL,
        [source_items_json] nvarchar(max) NOT NULL,
        [generated_by_user_id] int NULL,
        [generated_by_name] nvarchar(200) NULL,
        [ai_model] nvarchar(100) NULL,
        [status] nvarchar(20) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_media_reports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_media_reports_users_generated_by_user_id] FOREIGN KEY ([generated_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [prompt_frameworks] (
        [Id] int NOT NULL IDENTITY,
        [name_ar] nvarchar(200) NOT NULL,
        [name_en] nvarchar(200) NULL,
        [description_ar] nvarchar(max) NULL,
        [category] nvarchar(30) NOT NULL,
        [kind] nvarchar(20) NOT NULL,
        [prompt_text] nvarchar(max) NOT NULL,
        [variables_json] nvarchar(max) NOT NULL,
        [example_input] nvarchar(max) NULL,
        [example_output] nvarchar(max) NULL,
        [tags_json] nvarchar(max) NOT NULL,
        [recommended_model] nvarchar(100) NULL,
        [is_approved] bit NOT NULL,
        [usage_count] int NOT NULL,
        [created_by_user_id] int NULL,
        [created_by_name] nvarchar(200) NULL,
        [status] nvarchar(20) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_prompt_frameworks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_prompt_frameworks_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_issues] (
        [Id] int NOT NULL IDENTITY,
        [issue_no] int NOT NULL,
        [title_ar] nvarchar(300) NOT NULL,
        [subtitle_ar] nvarchar(300) NULL,
        [month] int NOT NULL,
        [year] int NOT NULL,
        [cover_image_url] nvarchar(500) NULL,
        [editor_letter] nvarchar(max) NULL,
        [contributions_open_at] datetimeoffset(3) NULL,
        [contributions_close_at] datetimeoffset(3) NULL,
        [status] nvarchar(20) NOT NULL,
        [published_pdf_url] nvarchar(500) NULL,
        [published_at] datetimeoffset(3) NULL,
        [created_by_user_id] int NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_issues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_issues_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_section_sla_defaults] (
        [section_type] nvarchar(30) NOT NULL,
        [sla_days] int NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by_user_id] int NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_section_sla_defaults] PRIMARY KEY ([section_type]),
        CONSTRAINT [FK_shorfah_section_sla_defaults_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [user_page_overrides] (
        [Id] int NOT NULL IDENTITY,
        [user_id] int NOT NULL,
        [page_id] int NOT NULL,
        [permission_id] int NOT NULL,
        [grant_type] nvarchar(10) NOT NULL,
        [created_by_user_id] int NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_user_page_overrides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_user_page_overrides_pages_page_id] FOREIGN KEY ([page_id]) REFERENCES [pages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_user_page_overrides_permissions_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_user_page_overrides_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_user_page_overrides_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [user_roles] (
        [Id] int NOT NULL IDENTITY,
        [user_id] int NOT NULL,
        [role_id] int NOT NULL,
        [assigned_by] int NULL,
        [assigned_at] datetime2(3) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_user_roles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_user_roles_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_user_roles_users_assigned_by] FOREIGN KEY ([assigned_by]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_user_roles_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [weekend_drafts] (
        [Id] int NOT NULL IDENTITY,
        [weekend_date] nvarchar(20) NOT NULL,
        [city] nvarchar(100) NOT NULL,
        [status] nvarchar(20) NOT NULL,
        [model_name] nvarchar(50) NOT NULL,
        [content_json] nvarchar(max) NOT NULL,
        [generated_by_user_id] int NULL,
        [approved_by_user_id] int NULL,
        [rejected_reason] nvarchar(max) NULL,
        [approved_at] datetimeoffset(3) NULL,
        [published_at] datetimeoffset(3) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_weekend_drafts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_weekend_drafts_users_approved_by_user_id] FOREIGN KEY ([approved_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_weekend_drafts_users_generated_by_user_id] FOREIGN KEY ([generated_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [reports_qa_queries] (
        [Id] int NOT NULL IDENTITY,
        [user_id] int NULL,
        [user_name] nvarchar(200) NULL,
        [query_type] nvarchar(20) NOT NULL,
        [wizard_answers_json] nvarchar(max) NULL,
        [search_query] nvarchar(500) NULL,
        [final_report_id] int NULL,
        [result_summary] nvarchar(max) NULL,
        [metadata_json] nvarchar(max) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_reports_qa_queries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_reports_qa_queries_final_media_reports_final_report_id] FOREIGN KEY ([final_report_id]) REFERENCES [final_media_reports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_reports_qa_queries_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_sections] (
        [Id] int NOT NULL IDENTITY,
        [issue_id] int NOT NULL,
        [parent_section_id] int NULL,
        [section_type] nvarchar(30) NOT NULL,
        [title_ar] nvarchar(300) NOT NULL,
        [description_ar] nvarchar(max) NULL,
        [display_order] int NOT NULL,
        [owner_user_id] int NULL,
        [owner_role] nvarchar(100) NULL,
        [include_in_pdf] bit NOT NULL,
        [auto_generate] bit NULL,
        [generation_prompt] nvarchar(max) NULL,
        [workflow_status] nvarchar(30) NOT NULL,
        [content_md] nvarchar(max) NULL,
        [content_html] nvarchar(max) NULL,
        [contributed_by_user_id] int NULL,
        [contributed_at] datetimeoffset(3) NULL,
        [reviewed_by_user_id] int NULL,
        [reviewed_at] datetimeoffset(3) NULL,
        [review_notes] nvarchar(max) NULL,
        [approved_by_user_id] int NULL,
        [approved_at] datetimeoffset(3) NULL,
        [rejection_reason] nvarchar(max) NULL,
        [sla_days] int NULL,
        [sla_starts_at] datetimeoffset(3) NULL,
        [sla_deadline] datetimeoffset(3) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_sections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_sections_shorfah_issues_issue_id] FOREIGN KEY ([issue_id]) REFERENCES [shorfah_issues] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_shorfah_sections_shorfah_sections_parent_section_id] FOREIGN KEY ([parent_section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_sections_users_approved_by_user_id] FOREIGN KEY ([approved_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_sections_users_contributed_by_user_id] FOREIGN KEY ([contributed_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_sections_users_owner_user_id] FOREIGN KEY ([owner_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_sections_users_reviewed_by_user_id] FOREIGN KEY ([reviewed_by_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_assignments] (
        [Id] int NOT NULL IDENTITY,
        [section_id] int NOT NULL,
        [user_id] int NOT NULL,
        [role] nvarchar(50) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_assignments_shorfah_sections_section_id] FOREIGN KEY ([section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_shorfah_assignments_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_notifications] (
        [Id] int NOT NULL IDENTITY,
        [user_id] int NOT NULL,
        [issue_id] int NULL,
        [section_id] int NULL,
        [type] nvarchar(50) NOT NULL,
        [title] nvarchar(300) NOT NULL,
        [body] nvarchar(max) NULL,
        [url] nvarchar(500) NULL,
        [is_read] bit NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_notifications_shorfah_issues_issue_id] FOREIGN KEY ([issue_id]) REFERENCES [shorfah_issues] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_notifications_shorfah_sections_section_id] FOREIGN KEY ([section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_notifications_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_section_media] (
        [Id] int NOT NULL IDENTITY,
        [section_id] int NOT NULL,
        [media_url] nvarchar(500) NOT NULL,
        [media_type] nvarchar(10) NOT NULL,
        [caption_ar] nvarchar(max) NULL,
        [display_order] int NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_section_media] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_section_media_shorfah_sections_section_id] FOREIGN KEY ([section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_section_permissions] (
        [Id] int NOT NULL IDENTITY,
        [section_id] int NOT NULL,
        [user_id] int NULL,
        [role_name] nvarchar(100) NULL,
        [permission] nvarchar(20) NOT NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_section_permissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_section_permissions_shorfah_sections_section_id] FOREIGN KEY ([section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_shorfah_section_permissions_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_workflow_log] (
        [Id] int NOT NULL IDENTITY,
        [section_id] int NOT NULL,
        [actor_user_id] int NULL,
        [action] nvarchar(50) NOT NULL,
        [from_status] nvarchar(30) NULL,
        [to_status] nvarchar(30) NULL,
        [notes] nvarchar(max) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_workflow_log] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_workflow_log_shorfah_sections_section_id] FOREIGN KEY ([section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_shorfah_workflow_log_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE TABLE [shorfah_reminders] (
        [Id] int NOT NULL IDENTITY,
        [section_id] int NOT NULL,
        [assignment_id] int NULL,
        [recipient_user_id] int NOT NULL,
        [channel] nvarchar(10) NOT NULL,
        [reminder_type] nvarchar(20) NOT NULL,
        [sent_at] datetimeoffset(3) NULL,
        [status] nvarchar(20) NULL,
        [message] nvarchar(max) NULL,
        [created_at] datetime2(3) NOT NULL,
        [created_by] nvarchar(100) NOT NULL,
        [updated_at] datetime2(3) NULL,
        [updated_by] nvarchar(100) NULL,
        [deleted_at] datetime2(3) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_shorfah_reminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_shorfah_reminders_shorfah_assignments_assignment_id] FOREIGN KEY ([assignment_id]) REFERENCES [shorfah_assignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_shorfah_reminders_shorfah_sections_section_id] FOREIGN KEY ([section_id]) REFERENCES [shorfah_sections] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_shorfah_reminders_users_recipient_user_id] FOREIGN KEY ([recipient_user_id]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_activity_logs_created_at] ON [activity_logs] ([created_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_activity_logs_user_id] ON [activity_logs] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_activitylog_deleted_at] ON [activity_logs] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_ai_year_activation_channels_activation_id] ON [ai_year_activation_channels] ([activation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_aiyearactivationchannel_deleted_at] ON [ai_year_activation_channels] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_ai_year_activation_channels_activation_channel] ON [ai_year_activation_channels] ([activation_id], [channel]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_ai_year_activations_year_month] ON [ai_year_activations] ([year], [month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_aiyearactivation_deleted_at] ON [ai_year_activations] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_ai_year_media_activation_id] ON [ai_year_media] ([activation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_aiyearmedia_deleted_at] ON [ai_year_media] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_ai_year_metrics_activation_id] ON [ai_year_metrics] ([activation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_aiyearmetric_deleted_at] ON [ai_year_metrics] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_archiveentry_deleted_at] ON [archive_entries] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_brandfont_deleted_at] ON [brand_fonts] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ux_brand_fonts_single_default] ON [brand_fonts] ([is_default]) WHERE [is_default] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_brandlogo_deleted_at] ON [brand_logos] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_dailyreport_deleted_at] ON [daily_reports] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_daily_reports_report_date] ON [daily_reports] ([report_date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_day_activations_day_id] ON [day_activations] ([day_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_dayactivation_deleted_at] ON [day_activations] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_dayyearlytheme_deleted_at] ON [day_yearly_themes] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_day_yearly_themes_day_year] ON [day_yearly_themes] ([day_id], [year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_design_templates_category] ON [design_templates] ([category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_designtemplate_deleted_at] ON [design_templates] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_final_media_reports_generated_by_user_id] ON [final_media_reports] ([generated_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_finalmediareport_deleted_at] ON [final_media_reports] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_final_media_reports_report_number] ON [final_media_reports] ([report_number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gac_news_items_kind] ON [gac_news_items] ([kind]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gac_news_items_published_at] ON [gac_news_items] ([published_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gacnewsitem_deleted_at] ON [gac_news_items] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gac_publications_category] ON [gac_publications] ([category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gac_publications_display_order] ON [gac_publications] ([display_order]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gac_publications_status] ON [gac_publications] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gacpublication_deleted_at] ON [gac_publications] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gac_social_posts_posted_at] ON [gac_social_posts] ([posted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_gacsocialpost_deleted_at] ON [gac_social_posts] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_gac_social_posts_platform_external_id] ON [gac_social_posts] ([platform], [external_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_generated_designs_created_by_user_id] ON [generated_designs] ([created_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_generated_designs_template_id] ON [generated_designs] ([template_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_generateddesign_deleted_at] ON [generated_designs] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_generated_outputs_selected] ON [generated_outputs] ([selected]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_generatedoutput_deleted_at] ON [generated_outputs] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_international_days_day_name_ar] ON [international_days] ([day_name_ar]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_internationalday_deleted_at] ON [international_days] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_intl_day_sources_day_id] ON [intl_day_sources] ([day_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_intl_day_sources_related] ON [intl_day_sources] ([related_table], [related_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_intldaysource_deleted_at] ON [intl_day_sources] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_intl_search_history_day_id] ON [intl_search_history] ([day_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_intl_search_history_searched_at] ON [intl_search_history] ([searched_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_intlsearchhistory_deleted_at] ON [intl_search_history] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_media_reports_date_range] ON [media_reports] ([date_from], [date_to]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_media_reports_generated_by_user_id] ON [media_reports] ([generated_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_mediareport_deleted_at] ON [media_reports] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_page_deleted_at] ON [pages] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_pages_slug] ON [pages] ([slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_permission_deleted_at] ON [permissions] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_permissions_name] ON [permissions] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_prompt_frameworks_category] ON [prompt_frameworks] ([category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_prompt_frameworks_created_by_user_id] ON [prompt_frameworks] ([created_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_promptframework_deleted_at] ON [prompt_frameworks] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_reports_qa_queries_final_report_id] ON [reports_qa_queries] ([final_report_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_reports_qa_queries_user_id] ON [reports_qa_queries] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_reportsqaquery_deleted_at] ON [reports_qa_queries] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_role_permissions_page_id] ON [role_permissions] ([page_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_role_permissions_permission_id] ON [role_permissions] ([permission_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_rolepermission_deleted_at] ON [role_permissions] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [role_page_perm_idx] ON [role_permissions] ([role_id], [page_id], [permission_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_role_deleted_at] ON [roles] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_roles_name] ON [roles] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_assignments_section_id] ON [shorfah_assignments] ([section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_assignments_user_id] ON [shorfah_assignments] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahassignment_deleted_at] ON [shorfah_assignments] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_shorfah_assignments_section_user] ON [shorfah_assignments] ([section_id], [user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_issues_created_by_user_id] ON [shorfah_issues] ([created_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_issues_status] ON [shorfah_issues] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahissue_deleted_at] ON [shorfah_issues] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_shorfah_issues_issue_no] ON [shorfah_issues] ([issue_no]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_notifications_issue_id] ON [shorfah_notifications] ([issue_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_notifications_section_id] ON [shorfah_notifications] ([section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_notifications_user_id] ON [shorfah_notifications] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_notifications_user_unread] ON [shorfah_notifications] ([user_id], [is_read]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahnotification_deleted_at] ON [shorfah_notifications] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_reminders_assignment_id] ON [shorfah_reminders] ([assignment_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_reminders_recipient_user_id] ON [shorfah_reminders] ([recipient_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_reminders_section_id] ON [shorfah_reminders] ([section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahreminder_deleted_at] ON [shorfah_reminders] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_section_media_section_id] ON [shorfah_section_media] ([section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahsectionmedia_deleted_at] ON [shorfah_section_media] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_section_permissions_section_id] ON [shorfah_section_permissions] ([section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_section_permissions_user_id] ON [shorfah_section_permissions] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahsectionpermission_deleted_at] ON [shorfah_section_permissions] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_shorfah_section_sla_defaults_updated_by_user_id] ON [shorfah_section_sla_defaults] ([updated_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_shorfah_sections_approved_by_user_id] ON [shorfah_sections] ([approved_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_shorfah_sections_contributed_by_user_id] ON [shorfah_sections] ([contributed_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_sections_issue_id] ON [shorfah_sections] ([issue_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_sections_owner_user_id] ON [shorfah_sections] ([owner_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_sections_parent_section_id] ON [shorfah_sections] ([parent_section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_shorfah_sections_reviewed_by_user_id] ON [shorfah_sections] ([reviewed_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_sections_sla_deadline] ON [shorfah_sections] ([sla_deadline]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_sections_workflow_status] ON [shorfah_sections] ([workflow_status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahsection_deleted_at] ON [shorfah_sections] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_workflow_log_actor_user_id] ON [shorfah_workflow_log] ([actor_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfah_workflow_log_section_id] ON [shorfah_workflow_log] ([section_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_shorfahworkflowlog_deleted_at] ON [shorfah_workflow_log] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_styleprofile_deleted_at] ON [style_profile] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_systemsetting_deleted_at] ON [system_settings] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_system_settings_key] ON [system_settings] ([key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_page_overrides_created_by_user_id] ON [user_page_overrides] ([created_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_page_overrides_page_id] ON [user_page_overrides] ([page_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_page_overrides_permission_id] ON [user_page_overrides] ([permission_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_userpageoverride_deleted_at] ON [user_page_overrides] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_user_page_overrides_user_page_permission] ON [user_page_overrides] ([user_id], [page_id], [permission_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_assigned_by] ON [user_roles] ([assigned_by]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_role_id] ON [user_roles] ([role_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_userrole_deleted_at] ON [user_roles] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [user_role_idx] ON [user_roles] ([user_id], [role_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_user_deleted_at] ON [users] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [ux_users_azure_oid] ON [users] ([azure_oid]) WHERE [azure_oid] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [ux_users_email] ON [users] ([email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_weekend_drafts_approved_by_user_id] ON [weekend_drafts] ([approved_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_weekend_drafts_generated_by_user_id] ON [weekend_drafts] ([generated_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_weekend_drafts_status] ON [weekend_drafts] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_weekend_drafts_weekend_date] ON [weekend_drafts] ([weekend_date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_weekenddraft_deleted_at] ON [weekend_drafts] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_weekend_places_is_active] ON [weekend_places] ([is_active]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    CREATE INDEX [ix_weekendplace_deleted_at] ON [weekend_places] ([deleted_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805064627_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805064627_InitialCreate', N'8.0.29');
END;
GO

COMMIT;
GO

