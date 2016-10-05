using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
//using System.Text;
//using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace PlayerDB
{
    public class DB
    {
        private IDbConnection db;
        private String table;
        private List<string> fields;

        private string paramlist0;
        private string paramlist1;

        public bool Connected
        {
            get
            {
                return (db != null);
            }
        }

        public DB()
        {
            TShockAPI.Hooks.AccountHooks.AccountDelete += OnAccountDelete;
        }

        public DB( String Table, String[] Fields )
        {
            TShockAPI.Hooks.AccountHooks.AccountDelete += OnAccountDelete;
            Connect(Table, Fields);
        }

        void OnAccountDelete(TShockAPI.Hooks.AccountDeleteEventArgs arg)
        {
            if (!Connected)
                return;

            DelUserData(arg.User.UUID);
        }

        public void Connect(string Table, String[] Fields)
        {
            Connect(Table, Fields.ToList());
        }

        public void Connect(string Table, List<string> Fields)
        {
            if( Connected )
                throw new SystemException("Attempted to connect the database while already connected!");

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

            columns.Add(new SqlColumn("UserID", MySqlDbType.String) { Primary = true, Unique = true });
            int i = 0;
            foreach (string field in fields)
            {
                columns.Add(new SqlColumn(field, MySqlDbType.Text, 100));
                paramlist0 += ",@" + i.ToString();
                paramlist1 += ",@" + (i + 1).ToString();
                i++;
            }
            paramlist0 = paramlist0.Substring(1);
            paramlist1 = paramlist1.Substring(1);

            sqlcreator.EnsureTableStructure(new SqlTable(table, columns));


            //QueryResult result = db.QueryReader("SELECT * FROM " + table + ";");

            /* No longer read on init, instead read on access - because there's no place to store it without logged-in players
            string uuid = "";

            while( result.Read() )
            {
                uuid = result.Reader.Get<string>("UserID");
                foreach (string field in fields)
                {
                    //data[ new Tuple<string,string>(uuid,field) ] = result.Reader.Get<string>(field);
                }
            }
            */
        }

        private TSPlayer FindPlayer(string uuid)
        {
            foreach (TSPlayer p in TShock.Players)
            {
                if (p.UUID == uuid)
                {
                    return p;
                }
            }
            return null;
        }

        private Dictionary<string,string> ReadUserData(string uuid)
        {
            if (!Connected)
                throw new SystemException("Database not connected in ReadUserData()");

            Dictionary<string, string> ret = new Dictionary<string, string> { };
            QueryResult result = db.QueryReader("SELECT * FROM " + table + " WHERE UserID=@0;", uuid);
            if (result.Read())
            {
                foreach (string f in fields)
                {
                    ret.Add(f, result.Get<string>(f));
                }
            }
            return ret;
        }

        private string ReadUserData(string uuid, string field, string defaultval = null)
        {
            if (!Connected)
                throw new SystemException("Database not connected in ReadUserData()");

            if (!fields.Contains(field))
                throw new ArgumentException("Field not in database.", "field");

            if ( db is SqliteConnection )
            {
                using (SqliteConnection dbl = (SqliteConnection)db)
                {
                    string ret;
                    QueryResult result = dbl.QueryReader("SELECT " + field + " FROM " + table + " WHERE UserID=@0;", uuid);

                    if (result.Read())
                    {
                        ret = result.Get<string>(field);
                        return ret;
                    }
                    return defaultval;
                }

            }
            else
            {
                QueryResult result = db.QueryReader("SELECT " + field + " FROM " + table + " WHERE UserID=@0;", uuid);
                string ret;

                if (result.Read())
                {
                    ret = result.Get<string>(field);
                    db.Close();
                    return ret;
                }
                db.Close();
                return defaultval;
            }


        }

        private void WriteUserData(string uuid, List<string> values)
        {
            if (!Connected)
                throw new SystemException("Database not connected in WriteUserData()"); ;

            values.Insert(0, uuid);

            if (db is SqliteConnection)
            {
                using (SqliteConnection dbl = (SqliteConnection)db)
                {
                    dbl.Query("DELETE FROM " + table + " WHERE UserID=@0;", uuid);
                    dbl.Query("INSERT INTO " + table + " VALUES (@0," + paramlist1 + ");", values.ToArray());
                }
            }
            else
            {
                db.Query("DELETE FROM " + table + " WHERE UserID=@0;", uuid);
                db.Close();
                db.Query("INSERT INTO " + table + " VALUES (@0," + paramlist1 + ");", values.ToArray());
                db.Close();
            }
        }

        private void WriteUserData(string uuid, string[] values)
        {
            WriteUserData(uuid, values.ToList());
        }

        private void WriteUserData(string uuid, Dictionary<string,string> data)
        {
            List<string> values = new List<string> { };
            foreach( string field in fields )
            {
                if( data.ContainsKey(field) )
                    values.Add(data[field]);
                else
                    values.Add(null);
            }
            WriteUserData(uuid, values);
        }

        private void WriteUserData(string uuid, string field, string value)
        {
            Dictionary<string, string> data;
            data = ReadUserData(uuid);
            if( data.ContainsKey(field) )
                data[field] = value;
            else
                data.Add(field, value);
            WriteUserData(uuid, data);
        }

        private void WriteUserData(TSPlayer p)
        {
            List<string> values = new List<string> { };

            foreach (string field in fields)
            {
                if (p.GetData<bool>("dbhas" + field))
                    values.Add(p.GetData<string>(field));
                else
                    values.Add("");
            }

            WriteUserData(p.UUID, values);
        }

        public string GetUserData(TSPlayer p, string field, string defaultval = null)
        {
            if (p == null)
            {
                TShock.Log.ConsoleError("DB.GetUserData() called with a null player!");
                return defaultval;
            }
            if( !p.IsLoggedIn )
            {
                TShock.Log.ConsoleError("DB.GetUserData() called before player was logged in!");
                return defaultval;
            }
            if( p.UUID == "" )
            {
                TShock.Log.ConsoleError("DB.GetUserData() called with a null UID for unknown reasons!");
                return defaultval;
            }
            if (p.GetData<bool>("dbhas" + field))
                return (string)p.GetData<string>(field);
            else
            {
                //string ret = ReadUserData(p.UUID, field, defaultval);
                /* wait, why would this be here exactly?
                if( ret != defaultval )
                {
                    WriteUserData(p.UUID, field, ret);
                }
                */
                //return ret;
                return ReadUserData(p.UUID, field, defaultval);
            }
        }

        public List<string> GetUserData(TSPlayer p) // May return an empty list if data is not found.
        {
            List<string> ret = new List<string> { };
            bool checkeddb = false;
            foreach (string field in fields)
            {
                if (p.GetData<bool>("dbhas" + field))
                    ret.Add(p.GetData<string>(field));
                else if (!checkeddb)
                {
                    QueryResult result = db.QueryReader("SELECT * FROM " + table + " WHERE UserID=@0;", p.UUID);

                    if (result.Read())
                    {
                        foreach (string f in fields)
                        {
                            p.SetData<bool>("dbhas" + field, true);
                            p.SetData<string>(field, result.Get<string>(f));
                        }
                        ret.Add( p.GetData<string>(field) );
                    }
                    checkeddb = true;
                }
            }
            return ret;
        }

       public void SetUserData(TSPlayer p, List<string> values)
        {
            int i = 0;
            foreach (string value in values)
            {
                p.SetData<bool>("dbhas" + fields[i], true);
                p.SetData<string>(fields[i], value);
                i += 1;
            }
            WriteUserData(p);
        }

        public void SetUserData(TSPlayer p, String[] values)
        {
            int i = 0;
            foreach (string value in values)
            {
                p.SetData<bool>("dbhas" + fields[i], true);
                p.SetData<string>(fields[i], value);
                i += 1;
            }
            WriteUserData(p);
        }

        public void SetUserData(string uuid, List<string> values)
        {
            TSPlayer p = FindPlayer(uuid);
            if( p != null )
                SetUserData(p, values);
            else
                WriteUserData(uuid, values);
        }

        public void SetUserData(string uuid, String[] values)
        {
            TSPlayer p = FindPlayer(uuid);
            if( p != null )
                SetUserData(p, values);
            else
                WriteUserData(uuid, values);
        }

        public void SetUserData(TSPlayer p, string field, string value)
        {
            if (!fields.Contains(field))
                return;

            p.SetData<bool>("dbhas" + field, true);
            p.SetData<string>(field, value);

            WriteUserData(p);
        }

        public void SetUserData(string uuid, string field, string value)
        {
            foreach (TSPlayer p in TShock.Players)
            {
                if (p.UUID == uuid)
                {
                    SetUserData(p, field, value);
                    return;
                }
            }

            WriteUserData(uuid, field, value);
        }

        public void ResetAllUserData(List<string> values)
        {
            string uuid = "";
            QueryResult allids = db.QueryReader("SELECT UserID FROM " + table + ";");
            while( allids.Read() )
            {
                uuid = allids.Get<string>("UserID");
                SetUserData(uuid, values);
            }
        }

        public void ResetAllUserData(string field, string value)
        {
            string uuid = "";
            QueryResult allids = db.QueryReader("SELECT UserID FROM " + table + ";");
            while (allids.Read())
            {
                uuid = allids.Get<string>("UserID");
                SetUserData(uuid, field, value);
            }
        }

        public void DelUserData(TSPlayer p)
        {
            foreach (string field in fields)
            {
                p.SetData<bool>("dbhas" + field, false);
            }
            db.Query("DELETE FROM " + table + " WHERE UserID=@0;", p.UUID);
        }

        public void DelUserData(string uuid)
        {
            TSPlayer p = FindPlayer(uuid);
            if( p != null )
                DelUserData(p);
            else
                db.Query("DELETE FROM " + table + " WHERE UserID=@0;", uuid);
        }

        private void clearDB()
        {
            foreach (TSPlayer p in TShock.Players)
            {
                foreach (string field in fields)
                {
                    p.SetData<bool>("dbhas" + field, false);
                }
            }
            db.Query("DELETE FROM " + table);
        }
    }
}
