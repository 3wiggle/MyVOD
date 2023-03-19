using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

 
namespace VODMovieWebSite.TMDB
{
    public class TmdbPersonCastContext
    {
        private const string CreateSql = @"CREATE TABLE IF NOT EXISTS PersonCast (ID INTEGER NOT NULL PRIMARY KEY,CreditId nvarchar[50],Character nvarchar[50], Title nvarchar[50], OriginalTitle nvarchar[50],Poster nvarchar[50],ReleaseDate  nvarchar[50],Adult int);";
        private const string DeleteSql = @"Delete from PersonCast where ID={0} ;";
        private const string InsertSql = @"INSERT INTO PersonCast (ID,CreditId,Character,Title,OriginalTitle,Poster,ReleaseDate,Adult) 
                                                                   VALUES({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}'); ";
        private const string ReplaceSql = @"REPLACE INTO PersonCast (ID,CreditId,Character,Title,OriginalTitle,Poster,ReleaseDate,Adult) 
                                                                   VALUES({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}'); ";
        private const string CreateIndex = @"CREATE UNIQUE INDEX idx_personcast_id ON PersonCast (ID);";

        private const string DropSql = @"DROP TABLE IF EXISTS PersonCast;";
        public List<MyPersonCast> Cast { get; set; }


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

        public void Update(SQLiteConnection conn, int id, string creditId, string character, string title, string originalTitle,string Poster, DateTime releaseDate, bool adult)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(ReplaceSql, id, TmdbContext.SafeString(creditId), TmdbContext.SafeString(character), TmdbContext.SafeString(title), TmdbContext.SafeString(originalTitle), TmdbContext.SafeString(Poster), releaseDate.ToShortDateString(), (adult) ? 1:0);
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Insert(SQLiteConnection conn, int id, string creditId, string character, string title, string originalTitle, string Poster, DateTime releaseDate, bool adult)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql, id, TmdbContext.SafeString(creditId), TmdbContext.SafeString(character), TmdbContext.SafeString(title), TmdbContext.SafeString(originalTitle), TmdbContext.SafeString(Poster), releaseDate.ToShortDateString(), (adult) ? 1 : 0);
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Load(SQLiteConnection conn)
        {
            Cast = new List<MyPersonCast>(); 

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM PersonCast";

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
                Cast.Add( new MyPersonCast(rdr));
            
            conn.Close();
        }
    }
}
