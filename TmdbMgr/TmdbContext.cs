using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.TMDb;
using System.IO;
using System.Threading;

namespace VODMovieWebSite.TMDB
{
    public class TmdbContext
    {
        public SQLiteConnection Conn { get; set; }
        public GenreContext Genres { get; set; }
        public TmdbMovieContext Movies { get; set; }
        public TmdbPersonContext Persons { get; set; }  
        public MyMediaCredits MediaCredits { get; set; }
        public MyPersonCredits PersonCredits { get; set; }

        public TmdbContext()
        {
            Conn = CreateConnection();
            Conn.Close();

            Persons = new TmdbPersonContext();
            PersonCredits = new MyPersonCredits();
            MediaCredits = new MyMediaCredits();
            Movies = new TmdbMovieContext();
            Genres = new GenreContext();
            
        }

        public void CreateDatabase()
        {
            Movies.CreateTable(Conn);
            (new TmdbPersonContext()).CreateTable(Conn);
            (new TmdbPersonCastContext()).CreateTable(Conn);
            (new TmdbPersonCrewContext()).CreateTable(Conn);
            (new TmdbMediaCastContext()).CreateTable(Conn);
            (new TmdbMediaCrewContext()).CreateTable(Conn);

            Genres.CreateTable(Conn);
        }

        public SQLiteConnection CreateConnection()
        {

            SQLiteConnection sqlite_conn; // Create a new database connection:
           
            sqlite_conn = new SQLiteConnection("Data Source=movies.db; Version=3; New=True; Compress=True; ");
           // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return sqlite_conn;
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

        public static string SafeString(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            return data.Replace("'", "''");
        }

    }
}