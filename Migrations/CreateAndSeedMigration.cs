using ConnectionLog.Models;
using FluentMigrator;
using OTA.Data.Dapper;
using OTA.Data.Dapper.Extensions;
using System;

namespace ConnectionLog.Migrations
{
    [OTAMigration(1, typeof(Plugin))]
    public class CreateAndSeed : Migration
    {
        public override void Up()
        {
            var logItem = this.Create.Table<LogItem>();

            logItem.WithColumn("Id")
                .AsInt64()
                .Identity()
                .PrimaryKey()
                .NotNullable()
                .Unique();

            logItem.WithColumn("PlayerName")
                .AsString(255)
                .NotNullable();

            logItem.WithColumn("IpAddress")
                .AsString(50)
                .NotNullable();

            logItem.WithColumn("DateAdded")
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentDateTime);
        }

        public override void Down()
        {
            this.Delete.Table<LogItem>();
        }
    }
}
