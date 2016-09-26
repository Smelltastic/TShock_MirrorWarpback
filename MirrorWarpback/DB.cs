using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace MirrorWarpback
{
    public class DB
    {
        private IDbConnection db;
        private String table;
        private List<string> fields;

        public bool Connected
        {
            get
            {
                return (db != null);
            }
        }

        public DB()
        {

        }

        public DB( String Table, String[] Fields )
        {
            Connect(Table, Fields);
        }

        public void Connect(string Table, String[] Fields)
        {
            Connect(Table, Fields.ToList());
        }

        public void Connect( string Table, List<string> Fields)
        {
            table = Table;
            fields = Fields;

            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] dbHost = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                            dbHost[0],
                            dbHost.Length == 1 ? "3306" : dbHost[1],
                            TShock.Config.MySqlDbName,
                            TShock.Config.MySqlUsername,
                            TShock.Config.MySqlPassword)
                    };
                    break;

                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, Table + ".sqlite");
                    db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;
            }

            SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            List<SqlColumn> columns = new List<SqlColumn>();

            columns.Add(new SqlColumn("UserID", MySqlDbType.Int32) { Primary = true, Unique = true, Length = 4 });
            foreach (string field in fields)
            {
                columns.Add(new SqlColumn(field, MySqlDbType.Text, 100));
            }

            sqlcreator.EnsureTableStructure(new SqlTable(table, columns));
        }

        public string GetUserData(int userid, string field)
        {
            if (!Connected)
                return null;

            if (!fields.Contains(field))
                return null;

            QueryResult result = db.QueryReader("SELECT " + field + " FROM " + table + " WHERE UserID=" + userid + ";");
            
            if (result.Read())
            {
                return result.Get<String>(field);
            }
            else
            {
                return null;
            }            
        }

        /* Currently disabled because for some reason it causes a HUGE delay and hangs up the plugin.
        public void SetUserData(int userid, string field, string data)
        {
            if (!Connected)
                return;

            if (!fields.Contains(field))
                return;

            TShock.Players[0].SendInfoMessage("Checking presense for user " + userid + ".");
            QueryResult result = db.QueryReader("SELECT * FROM " + table + " WHERE UserID=@0;", userid);
            if( ! result.Read() )
            {
                TShock.Players[0].SendInfoMessage("No data found for user " + userid + ", creating.");
                db.Query("INSERT INTO " + table + " (UserID, " + field + ") VALUES (@0, @1);", userid, "");
            }

            TShock.Players[0].SendInfoMessage("Sending update query.");
            db.Query("UPDATE " + table + " SET " + field + "='" + data + "' WHERE UserID=" + userid + ";");

            TShock.Players[0].SendInfoMessage("Update query sent.");

            //db.Query("UPDATE " + table + " SET " + field + "=@1 WHERE Key=@0; IF @@ROWCOUNT = 0 INSERT INTO " + table + " (" + field + ") VALUES (@1);", userid, data);
            //db.Query("INSERT INTO " + table + "(UserId, " + field + ") VALUES (@0, @1);", userid, data);
        }
        */

        public void SetUserData(int userid, List<string>fields )
        {
            string all = userid + ",'" + String.Join("','", fields) + "'";
            db.Query("DELETE FROM " + table + " WHERE UserID=@0;", userid);
            db.Query("INSERT INTO " + table + " VALUES (" + all + ");");
        }

        public void DelUserData(int userid)
        {
            db.Query("DELETE FROM " + table + " WHERE UserID=@0;", userid);
        }

        private void clearDB()
        {
            db.Query("DELETE FROM " + table);
        }
    }
}
