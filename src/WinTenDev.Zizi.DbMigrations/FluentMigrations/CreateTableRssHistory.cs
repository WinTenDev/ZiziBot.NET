﻿using FluentMigrator;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200705201614)]
public class CreateTableRssHistory : Migration
{
    private const string TableName = "RssHistory";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("Id").AsInt32().Identity().PrimaryKey()
            .WithColumn("ChatId").AsDouble()
            .WithColumn("RssSource").AsMySqlVarchar(255)
            .WithColumn("Url").AsMySqlText()
            .WithColumn("Title").AsMySqlVarchar(255)
            .WithColumn("PublishDate").AsDateTime()
            .WithColumn("Author").AsMySqlVarchar(150)
            .WithColumn("CreatedAt").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}