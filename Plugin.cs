using ConnectionLog.Models;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Xna.Framework;
using OTA;
using OTA.Command;
using OTA.Commands;
using OTA.Data;
using OTA.Data.Dapper.Extensions;
using OTA.Data.Dapper.Mappers;
using OTA.Extensions;
using OTA.Logging;
using OTA.Plugin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ConnectionLog
{
    //public class DapperPagination
    //{

    //}
    //public static class DapperPaginationExtension
    //{
    //    public static IEnumerable<T> Page<T>
    //    (
    //        this IDbConnection cnn,
    //        string[] orderBy,
    //        string orderKey = "Id",
    //        int page = 0,
    //        int pageSize = 20,
    //        object param = null,
    //        IDbTransaction transaction = null,
    //        bool buffered = true,
    //        int? commandTimeout = default(int?),
    //        CommandType? commandType = default(CommandType?)
    //    ) where T : class
    //    {
    //        var sql = new StringBuilder();

    //        orderKey = ColumnMapper.Enclose(orderKey);


    //        if (DatabaseFactory.Provider == "sqlite")
    //        {
    //            sql.Append(
    //                  "SELECT * " +
    //                  "FROM LogItems a " +
    //                 $"where (select count(b.Id) from LogItems b where b.DateAdded < a.DateAdded  order by b.DateAdded) / 5 = 0 " +
    //                 $"order by a.DateAdded"
    //            );
    //        }
    //        else
    //        {
    //            //select *
    //            //from
    //            //(
    //            //    SELECT *, ROW_NUMBER() OVER(ORDER BY "Id") rownum

    //            //    FROM "LogItems"
    //            //) x
    //            //where rownum / 5 = 0
    //            sql.Append($"select * from ");
    //        }

    //        if (param != null) sql.AddClause(param, false, true);

    //        if (orderBy != null && orderBy.Length > 0)
    //        {
    //            sql.Append(" order by ");
    //            bool first = true;
    //            foreach (var ob in orderBy)
    //            {
    //                if (!first) sql.Append(',');
    //                sql.Append(ob);
    //                first = false;
    //            }
    //        }

    //        return cnn.Query<T>(sql.ToString(), param, transaction, buffered, commandTimeout, commandType);
    //    }
    //}

    [OTAVersion(1, 0)]
    public class Plugin : BasePlugin
    {
        public Plugin()
        {
            this.Author = "DeathCradle";
            this.Description = "Connection logger";
            this.Name = "Connection logger";
            this.Version = "1.0";
        }

        protected override void Initialized(object state)
        {
            base.Initialized(state);
        }

        protected override void Enabled()
        {
            base.Enabled();
            ProgramLog.Plugin.Log("Connection log initialised");

            this.AddCommand("cl-list")
                .WithPermissionNode("connectionlog.list")
                .WithHelpText("list -page <page>")
                .WithDescription("View the latest connection log")
                .Calls(Cmd_ConnectionLog_List);
        }

        void Cmd_ConnectionLog_List(ISender sender, ArgumentList args)
        {
            if(!OTA.Data.DatabaseFactory.Available)
            {
                sender.Message(255, Color.Red, "No database available.");
                return;
            }

            int page = 0;
            args.TryPopAny("-page", out page);

            if(page == 0 && args.Count == 1)
            {
                if (!args.TryGetInt(0, out page)) page = 1;
            }
            if (page < 1) page = 1;

            using (var db = OTA.Data.DatabaseFactory.CreateConnection())
            {
                using (var txn = db.BeginTransaction())
                {
                    // TODO: pagination info
                    const Int32 MaxChatLines = 9;
                    var res = db.Query<LogItem>
                    (
                         "SELECT * " +
                        $"FROM {TableMapper.TypeToName<LogItem>()} a " +
                         "where cast(( " +
                            $"SELECT COUNT(b.{ColumnMapper.Enclose("Id")}) " +
                            $"FROM {TableMapper.TypeToName<LogItem>()} b " +
                            $"where b.{ColumnMapper.Enclose("Id")} < a.{ColumnMapper.Enclose("Id")} " +
                        $") / {MaxChatLines} as int) = ({page} - 1) " +
                        $"order by a.{ColumnMapper.Enclose("DateAdded")} desc",
                        transaction: txn
                    );

                    if (res != null)
                    {
                        if (res.Count() > 0)
                        {
                            foreach (var item in res)
                            {
                                sender.Message(255, Color.Orange, $"{item.PlayerName} - {item.DateAdded:HH:mm:ss dd/MM/yyyy}");
                            }
                        }
                        else sender.Message(255, Color.Red, "No connection logs available.");
                    }
                    else sender.Message(255, Color.Red, "No connection logs available.");
                }
            }
        }

        [Hook]
        void OnPlayerJoin(ref HookContext ctx, ref HookArgs.PlayerEnteredGame args)
        {
            using (var db = OTA.Data.DatabaseFactory.CreateConnection())
            {
                using (var txn = db.BeginTransaction())
                {
                    db.Insert(new LogItem()
                    {
                        PlayerName = ctx.Player.name,
                        IpAddress = Terraria.Netplay.Clients[args.Slot].RemoteIPAddress(),
                        DateAdded = DateTime.Now
                    }, transaction: txn);
                    txn.Commit();
                }
            }
        }
    }
}
