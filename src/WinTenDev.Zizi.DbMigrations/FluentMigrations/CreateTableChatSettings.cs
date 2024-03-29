﻿using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200621163214)]
[UsedImplicitly]
public class CreateTableChatSettings : Migration
{
    public const string TableName = "group_settings";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("chat_title").AsString()
            .WithColumn("chat_type").AsString()
            .WithColumn("members_count").AsInt64().WithDefaultValue(-1)
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("event_log_chat_id").AsInt64().WithDefaultValue(0)
            .WithColumn("is_admin").AsBoolean().WithDefaultValue(0)
            .WithColumn("enable_bot").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_afk_status").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_anti_malfiles").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_badword_filter").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_fed_cas_ban").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_fed_es2_ban").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_fed_spamwatch").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_find_notes").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_find_tags").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_fire_check").AsBoolean().WithDefaultValue(0)
            .WithColumn("enable_flood_check").AsBoolean().WithDefaultValue(0)
            .WithColumn("enable_human_verification").AsBoolean().WithDefaultValue(0)
            .WithColumn("enable_profile_photo_check").AsBoolean().WithDefaultValue(0)
            .WithColumn("enable_reply_notification").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_restriction").AsBoolean().WithDefaultValue(0)
            .WithColumn("enable_security").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_url_filtering").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_unified_welcome").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_warn_username").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_welcome_message").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_word_filter_global").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_word_filter_group").AsBoolean().WithDefaultValue(1)
            .WithColumn("enable_zizi_mata").AsBoolean().WithDefaultValue(1)
            .WithColumn("flood_offset").AsInt16().WithDefaultValue(7)
            .WithColumn("last_tags_message_id").AsInt64().WithDefaultValue(-1)
            .WithColumn("last_warn_username_message_id").AsInt64().WithDefaultValue(-1)
            .WithColumn("last_welcome_message_id").AsInt64().WithDefaultValue(-1)
            .WithColumn("rules_link").AsMySqlMediumText().WithDefaultValue("")
            .WithColumn("rules_text").AsMySqlMediumText().WithDefaultValue("")
            .WithColumn("warning_username_limit").AsInt16().WithDefaultValue(3)
            .WithColumn("welcome_message").AsMySqlMediumText().WithDefaultValue("")
            .WithColumn("welcome_button").AsMySqlText().WithDefaultValue("")
            .WithColumn("welcome_media").AsString(150).WithDefaultValue("")
            .WithColumn("welcome_media_type").AsInt16().WithDefaultValue(-1)
            .Indexed("chat_id");
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}