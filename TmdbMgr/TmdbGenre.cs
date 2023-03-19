using GCS;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Data.SQLite;
using System.Linq;
using System.Net.TMDb;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VODMovieWebSite
{
    public class GenreContext 
    {
        private const string CreateSql = @"CREATE TABLE IF NOT EXISTS Genre (ID INTEGER NOT NULL PRIMARY KEY, Code nvarchar[1], Name nvarchar[100]) ;";
        private const string DeleteSql = @"Delete from Genre where ID={0} ;";
        private const string InsertSql = @"INSERT INTO Genre (ID, Code, Name) VALUES({0},'{1}','{2}'); ";
        private const string ReplaceSql = @"REPLACE INTO Genre (ID,Code,Name) VALUES({0},'{1}','{2}');";
        private const string CreateIndex = @"CREATE UNIQUE INDEX idx_genre_id ON Genre (ID);";

        private const string DropSql      = @"DROP TABLE IF EXISTS Genre;";
        public List<MyGenre> Genres { get; set; }
         

        public void CreateTable(SQLiteConnection conn)
        {
            
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = CreateSql;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = CreateIndex;
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Drop(SQLiteConnection conn)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = DropSql;
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Delete(SQLiteConnection conn, int id)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(DeleteSql,id);
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Update(SQLiteConnection conn, int id, string code, string name)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql, id, code, name.Replace("'", "''"));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Insert(SQLiteConnection conn, int id, string code, string name)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql,id,code,name.Replace("'","''"));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Load(SQLiteConnection conn)
        {
            Genres = new List<MyGenre>();
            
            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT Id, Code, Name FROM Genre";

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
            {
                MyGenre g = new MyGenre();
                g.Id = Convert.ToInt32(rdr[0]);
                g.Code = rdr[1].ToString();
                g.Name = rdr[2].ToString();

                Genres.Add(g);
            }
            conn.Close();
        }
    }

    
    public class MyGenre
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
