﻿using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200314172001)]
[UsedImplicitly]
public class CreateTableRssSettings : Migration
{
    internal const string TableName = "rss_settings";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("from_id").AsInt64().NotNullable()
            .WithColumn("chat_id").AsInt64().NotNullable()
            .WithColumn("url_feed").AsMySqlText().NotNullable()
            .WithColumn("created_at").AsDateTime().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}