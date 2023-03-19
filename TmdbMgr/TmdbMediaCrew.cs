using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

 
namespace VODMovieWebSite.TMDB
{
    public class TmdbMediaCrewContext
    {
        private const string CreateSql = @"CREATE TABLE IF NOT EXISTS MediaCrew 
                       (ID INTEGER NOT NULL PRIMARY KEY, 
                        CreditId nvarchar[50], 
                        Name nvarchar[50], 
                        Profile nvarchar[50]);";
        private const string DeleteSql = @"Delete from MediaCrew where ID={0} ;";
        private const string InsertSql = @"INSERT INTO MediaCrew (ID,CreditId,Name,Profile) 
                                                                   VALUES({0},'{1}','{2}','{3}'); ";
        private const string ReplaceSql = @"REPLACE INTO MediaCrew (ID,CreditId,Name,Profile) 
                                                                    VALUES({0},'{1}','{2}','{3}'); ";
        private const string CreateIndex = @"CREATE UNIQUE INDEX idx_mediacrew_id ON MediaCrew (ID);";

        private const string DropSql = @"DROP TABLE IF EXISTS MediaCrew;";
        public List<MyMediaCrew> Crew { get; set; }


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
            sqlite_cmd.CommandText = string.Format(DeleteSql, id);
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Update(SQLiteConnection conn, int id, string creditId, string name, string profile)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(ReplaceSql, id, TmdbContext.SafeString(creditId), TmdbContext.SafeString(name), TmdbContext.SafeString(profile));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Insert(SQLiteConnection conn, int id, string creditId, string name, string profile)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql, id, TmdbContext.SafeString(creditId), TmdbContext.SafeString(name), TmdbContext.SafeString(profile)); 
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Load(SQLiteConnection conn)
        {
            Crew = new List<MyMediaCrew>();

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM MediaCrew";

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
                Crew.Add(new MyMediaCrew(rdr));

            conn.Close();
        }
    }
}
