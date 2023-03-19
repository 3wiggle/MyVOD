using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 Id = pc.Id;
        CreditId = pc.CreditId;
            Title = pc.Title;
            OriginalTitle = pc.OriginalTitle;
            Poster = pc.Poster;
            ReleaseDate = pc.ReleaseDate;
            Adult = pc.Adult;
            Department = pc.Department;
            Job = pc.Job; 
 */

namespace VODMovieWebSite.TMDB
{
    public class TmdbPersonCrewContext
    {
        private const string CreateSql = @"CREATE TABLE IF NOT EXISTS PersonCrew (ID INTEGER NOT NULL PRIMARY KEY,CreditId nvarchar[50],Title nvarchar[50], OriginalTitle nvarchar[50],Poster nvarchar[50],ReleaseDate  nvarchar[50],Adult int, Department nvarchar[50], Job nvarchar[50]);";
        private const string DeleteSql = @"Delete from PersonCrew where ID={0} ;";
        private const string InsertSql = @"INSERT INTO PersonCrew (ID,CreditId,Title,OriginalTitle,Poster,ReleaseDate,Adult,Department,Job) 
                                                                   VALUES({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}'); ";
        private const string ReplaceSql = @"REPLACE INTO PersonCrew (ID,CreditId,Title,OriginalTitle,Poster,ReleaseDate,Adult,Department,Job) 
                                                                   VALUES({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}'); ";
        private const string CreateIndex = @"CREATE UNIQUE INDEX idx_personcrew_id ON PersonCrew (ID);";

        private const string DropSql = @"DROP TABLE IF EXISTS PersonCrew;";
        public List<MyPersonCrew> Cast { get; set; }


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

        public void Update(SQLiteConnection conn, int id, string creditId, string title, string originalTitle, string Poster, DateTime releaseDate, bool adult, string Department, string Job)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(ReplaceSql, id, 
                                    TmdbContext.SafeString(creditId), 
                                    TmdbContext.SafeString(title), 
                                    TmdbContext.SafeString(originalTitle), 
                                    TmdbContext.SafeString(Poster), 
                                    releaseDate.ToShortDateString(), 
                                    (adult) ? 1 : 0,
                                    TmdbContext.SafeString(Department),
                                    TmdbContext.SafeString(Job));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Insert(SQLiteConnection conn, int id, string creditId, string title, string originalTitle, string Poster, DateTime releaseDate, bool adult, string Department, string Job)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql, id,
                                    TmdbContext.SafeString(creditId),
                                    TmdbContext.SafeString(title),
                                    TmdbContext.SafeString(originalTitle),
                                    TmdbContext.SafeString(Poster),
                                    releaseDate.ToShortDateString(),
                                    (adult) ? 1 : 0,
                                    TmdbContext.SafeString(Department),
                                    TmdbContext.SafeString(Job));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }
        public void Load(SQLiteConnection conn)
        {
            Cast = new List<MyPersonCrew>();

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM PersonCrew";

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
                Cast.Add(new MyPersonCrew(rdr));

            conn.Close();
        }
    }
 
}
