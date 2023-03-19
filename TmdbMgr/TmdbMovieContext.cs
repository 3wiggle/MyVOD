using GCS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data.Entity.Migrations.Model;
using System.Data.SQLite;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.TMDb;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VODMovieWebSite;
using System.IO;
using System.Threading;
using System.Web.Configuration;

namespace VODMovieWebSite.TMDB
{   public class TmdbMovieContext
    {

        private const string CreateSql = @"CREATE TABLE IF NOT EXISTS Movie (
                                    ID INTEGER NOT NULL PRIMARY KEY,
                                    Title nvarchar[100],
                                    OriginalTitle nvarchar[100],
                                    TagLine nvarchar[100],
                                    Overview nvarchar[1000],
                                    Poster  nvarchar[100],
                                    Backdrop nvarchar[100],
                                    Adult int,
                                    Budget int,
                                    HomePage  nvarchar[256],
                                    Imdb nvarchar[100],
                                    ReleaseDate DateTime,
                                    Revenue numeric,
                                    Runtime int,
                                    VoteCount int,
                                    Popularity numeric,
                                    VoteAverage numeric,
                                    Status nvarchar[100]);";

        private const string DeleteSql = @"Delete from Movie where ID={0} ;";
        private const string InsertSql = @"INSERT INTO Movie (ID,Title,OriginalTitle,TagLine,Overview,Poster,Backdrop,Adult,Budget,HomePage,Imdb,ReleaseDate,Revenue,Runtime,VoteCount,Popularity,VoteAverage,Status)  
                                           VALUES({0},'{1}','{2}','{3}','{4}','{5}','{6}',{7},{8},'{9}','{10}','{11}',{12},{13},{14},{15},{16},'{17}'); ";
        private const string ReplaceSql = @"REPLACE INTO Movie (ID,Title,OriginalTitle,TagLine,Overview,Poster,Backdrop,Adult,Budget,HomePage,Imdb,ReleaseDate,Revenue,Runtime,VoteCount,Popularity,VoteAverage,Status)  
                                           VALUES({0},'{1}','{2}','{3}','{4}','{5}','{6}',{7},{8},'{9}','{10}','{11}',{12},{13},{14},{15},{16},'{17}'); ";
 
        private const string CreateIndex = @"CREATE UNIQUE INDEX idx_movie_id ON Movie (ID);";

        private const string DropSql = @"DROP TABLE IF EXISTS Movie;";


        public List<MyMovie> Items { get; set; }

        public TmdbMovieContext()
        {
            Items = new List<MyMovie>(); 
        }


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

        public void Update(SQLiteConnection conn, int id, string title, string orginalTitle, string tagLine, string overview, string poster, string backdrop, bool adult, int budget, string homePage, string imdb, DateTime? releaseDate, long revenue, int? runtime, int voteCount, decimal popularity, decimal voteAverage, string status )
        {
             
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(ReplaceSql, id,
                                                              TmdbContext.SafeString(title),
                                                              TmdbContext.SafeString(orginalTitle),
                                                              TmdbContext.SafeString(tagLine),
                                                              TmdbContext.SafeString(overview),
                                                              TmdbContext.SafeString(poster),
                                                              TmdbContext.SafeString(backdrop),
                                                              (adult) ? 1 : 0,
                                                              budget,
                                                              TmdbContext.SafeString(homePage),
                                                              TmdbContext.SafeString(imdb),
                                                              (releaseDate.HasValue) ? releaseDate.Value.ToShortDateString() : string.Empty,
                                                              revenue,
                                                              runtime,
                                                              voteCount,
                                                              popularity,
                                                              voteAverage,
                                                              TmdbContext.SafeString(status)
                                                                ); 
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Insert(SQLiteConnection conn, int id, string title, string orginalTitle, string tagLine, string overview, string poster, string backdrop, bool adult, int budget, string homePage, string imdb, DateTime? releaseDate, long revenue, int? runtime, int voteCount, decimal popularity, decimal voteAverage, string status)
        {
             
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql, id,
                                                              TmdbContext.SafeString(title),
                                                              TmdbContext.SafeString(orginalTitle),
                                                              TmdbContext.SafeString(tagLine),
                                                              TmdbContext.SafeString(overview),
                                                              TmdbContext.SafeString(poster),
                                                              TmdbContext.SafeString(backdrop),
                                                              (adult) ? 1 : 0,
                                                              budget,
                                                              TmdbContext.SafeString(homePage),
                                                              TmdbContext.SafeString(imdb),
                                                              (releaseDate.HasValue) ? releaseDate.Value.ToShortDateString() : string.Empty,
                                                              revenue,
                                                              runtime,
                                                              voteCount,
                                                              popularity,
                                                              voteAverage,
                                                              TmdbContext.SafeString(status)
                                                                );
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public MyMovie FindByID(SQLiteConnection conn, int id)
        {
            MyMovie rtn = null;

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format("SELECT * FROM Movie where ID={0}",id);

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            if (rdr.Read())
            {
                rtn = new MyMovie(rdr);
            }
            conn.Close();
            return rtn;

        }

        public List<MyMovie> FindByTitle(SQLiteConnection conn, string title)
        {
            List<MyMovie> rtn = new List<MyMovie>();

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format("SELECT * FROM Movie where Title = '{0}';", title);

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
            {
                rtn.Add(new MyMovie(rdr));
            }
            conn.Close();
            return rtn;
        }

        public List<MyMovie> Load(SQLiteConnection conn)
        {
            List<MyMovie> rtn = new List<MyMovie>();

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM Movie";

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
            {
                rtn.Add(new MyMovie(rdr));
            }
            conn.Close();
            return rtn;
        }

        public void SaveAll(SQLiteConnection conn)
        {
             
            conn.Open();
            foreach (var m in Items)
                Save(conn,m);
            conn.Close();
        }

        public void Save(SQLiteConnection conn, MyMovie m)
        {
            Update(conn, m.ID, m.Title, m.OriginalTitle, m.TagLine, m.Overview,
                    m.Poster, m.Backdrop, m.Adult, m.Budget, m.HomePage,m.Imdb, m.ReleaseDate,
                    m.Revenue, m.Runtime, m.VoteCount.Value, m.Popularity.Value, m.VoteAverage.Value, m.Status);
        }
    }


    public class MyMovie
    {
        private object locker = new object();
        public int ID { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string TagLine { get; set; }
        public string Overview { get; set; }
        public string Poster { get; set; }
        public string Backdrop { get; set; }
        public bool Adult { get; set; }
        public Collection BelongsTo { get; set; }
        public int Budget { get; set; }
        public List<MyGenre> Genres { get; set; }
        public string HomePage { get; set; }
        public string Imdb { get; set; }
        public IEnumerable<Company> Companies { get; set; }
        public IEnumerable<Country> Countries { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public long Revenue { get; set; }
        public int? Runtime { get; set; }
        public IEnumerable<Language> Languages { get; set; }
        public AlternativeTitles AlternativeTitles { get; set; }
        public MyMediaCredits Credits { get; set; }
        public MyImages Images { get; set; }
        public Videos Videos { get; set; }
        public Keywords Keywords { get; set; }
        public Releases Releases { get; set; }
        public Translations Translations { get; set; }
        public decimal? Popularity { get; set; }
        public decimal? VoteAverage { get; set; }
        public int? VoteCount { get; set; }
        public string Status { get; set; }
        public MyExternalIds External { get; set; }

        public System.Drawing.Image Cover { get { return GetCoverImage().Result; } }

        public MyMovie(SQLiteDataReader rdr)
        {
            this.Genres = new List<MyGenre>();
            this.ID = Convert.ToInt32(rdr[0]);
            this.Title = rdr[1].ToString();
            this.OriginalTitle = rdr[2].ToString();
            this.TagLine = rdr[3].ToString();
            this.Overview = rdr[4].ToString();
            this.Poster = rdr[5].ToString();
            this.Backdrop = rdr[6].ToString();
            this.Adult = (Convert.ToInt32(rdr[7]) == 0) ? false : true;
            this.Budget = Convert.ToInt32(rdr[8]);
            this.HomePage = rdr[9].ToString();
            this.Imdb = rdr[10].ToString();
            DateTime dt = DateTime.Now;
            if (DateTime.TryParse(rdr[11].ToString(), out dt))
                this.ReleaseDate = dt;

            this.Revenue = long.Parse(rdr[12].ToString());
            this.Runtime = int.Parse(rdr[13].ToString());
            this.VoteCount = int.Parse(rdr[14].ToString());
            this.Popularity = decimal.Parse(rdr[15].ToString());
            this.VoteAverage = decimal.Parse(rdr[16].ToString());

            External = new MyExternalIds();
        }

        public MyMovie(Movie mov)
        {
            lock (locker)
            {
                this.Genres = new List<MyGenre>();

                this.Adult = mov.Adult;
                this.AlternativeTitles = mov.AlternativeTitles;
                this.Backdrop = mov.Backdrop;
                this.BelongsTo = mov.BelongsTo;
                this.Budget = mov.Budget;
                this.Companies = mov.Companies;
                this.Countries = mov.Countries;
                this.Credits = new MyMediaCredits();
                foreach (var c in mov.Credits.Cast)
                    this.Credits.Cast.Add(new MyMediaCast(c));

                foreach (var c in mov.Credits.Crew)
                    this.Credits.Crew.Add(new MyMediaCrew(c));

                this.External = new MyExternalIds();
                this.External.Freebase = mov.External.Freebase;
                this.External.FreebaseMid = mov.External.FreebaseMid;
                this.External.Imdb = mov.External.Imdb;
                this.External.Tvdb = (mov.External.Tvdb.HasValue) ? mov.External.Tvdb.Value : 0 ;
                this.External.Tvrage = (mov.External.Tvrage.HasValue)? mov.External.Tvrage.Value : 0;

                foreach (var g in mov.Genres)
                    this.Genres.Add(new MyGenre() { Id = g.Id, Name = g.Name });

                this.HomePage = mov.HomePage;
                this.ID = mov.Id;
                this.Images = new MyImages();
                foreach (var i in mov.Images.Posters)
                    this.Images.Posters.Add(new MyImage(i));
                foreach (var i in mov.Images.Backdrops)
                    this.Images.Backdrops.Add(new MyImage(i));

                this.Imdb = mov.Imdb;
                this.Keywords = mov.Keywords;
                this.Languages = mov.Languages;
                this.OriginalTitle = mov.OriginalTitle;
                this.Overview = mov.Overview;
                this.Popularity = mov.Popularity;
                this.Poster = mov.Poster;
                this.ReleaseDate = mov.ReleaseDate;
                this.Releases = mov.Releases;
                this.Revenue = mov.Revenue;
                this.Runtime = mov.Runtime;
                this.Status = mov.Status;
                this.TagLine = mov.TagLine;
                this.Title = mov.Title;
                this.Translations = mov.Translations;
                this.Videos = mov.Videos;
                this.VoteAverage = mov.VoteAverage;
                this.VoteCount = mov.VoteCount;
            }
        }

        private async Task<System.Drawing.Image> GetCoverImage()
        {
            System.Drawing.Image rtn = null;
            string fullPath = Path.Combine(WebConfigurationManager.AppSettings["WebRoot"], Images.Posters[0].FilePath);
            if (File.Exists(fullPath))
                return Bitmap.FromFile(fullPath); 
            
            await DownloadImage(Images.Posters[0].FilePath, fullPath, new CancellationToken());
            try
            {
                 rtn = System.Drawing.Image.FromFile(fullPath);
            }
            catch(Exception ex)
            {
                return rtn;
            }
            return rtn;
        }

        public void Hydrate(bool isTemp=true)
        {
             
        }

        public static async Task DownloadImage(string filename, string localpath, CancellationToken cancellationToken)
        {
            if (!File.Exists(localpath))
            {
                string folder = Path.GetDirectoryName(localpath);
                Directory.CreateDirectory(folder);

                var storage = new StorageClient();
                using (var fileStream = new FileStream(localpath, FileMode.Create, FileAccess.Write, FileShare.None, short.MaxValue, true))
                {
                    try
                    {
                        await storage.DownloadAsync(filename, fileStream, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError(ex.ToString());
                    }
                }
            }
        }
    }



    public class MyImages
    {
        public List<MyImage> Backdrops { get; set; }
        public List<MyImage> Posters { get; set; }

        public MyImages()
        {
            Backdrops = new List<MyImage>();
            Posters = new List<MyImage>();
        }
    }
}


//IEnumerable<Company> Companies { get; internal set; }
//IEnumerable<Country> Countries { get; internal set; }
//Enumerable<MyGenre> Genres { get; internal set; }
//Collection BelongsTo { get; internal set; }
//IEnumerable<Language> Languages { get; internal set; }
//AlternativeTitles AlternativeTitles { get; internal set; }
//MediaCredits Credits { get; internal set; }
//Images Images { get; internal set; }
//Videos Videos { get; internal set; }
//Keywords Keywords { get; internal set; }
//Releases Releases { get; internal set; }
//Translations Translations { get; internal set; }


//ExternalIds External { get; internal set; }