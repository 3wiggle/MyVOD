using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.TMDb;
using VODMovieWebSite.TMDB; 
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GCS
{
    public class TheMovieDB
    {
        public const string API_KEY  = @"375701e0cc40ba900e1f5ba3b12a7c50";
        public const string API_READ_ACCESS_TOKEN = @"eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIzNzU3MDFlMGNjNDBiYTkwMGUxZjViYTNiMTJhN2M1MCIsInN1YiI6IjYzYTYwYzAyYmVmZDkxMDBhOTFmMjUxNiIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.ON9us7z0dUN_q2B31ir3ArCWxTzw-7VF1ODcIXqWy2I";

        public static async Task Sample(CancellationToken cancellationToken)
        {
            using (var client = new ServiceClient(API_KEY))
            {
                for (int i = 1, count = 1000; i <= count; i++)
                {
                    var movies = await client.Movies.GetTopRatedAsync(null, i, cancellationToken);
                    count = movies.PageCount; // keep track of the actual page count

                    foreach (System.Net.TMDb.Movie m in movies.Results)
                    {
                        var movie = await client.Movies.GetAsync(m.Id, null, true, cancellationToken);

                        var personIds = movie.Credits.Cast.Select(s => s.Id)
                            .Union(movie.Credits.Crew.Select(s => s.Id));

                        foreach (var id in personIds)
                        {
                            var person = await client.People.GetAsync(id, true, cancellationToken);

                            foreach (var img in person.Images.Results)
                            {
                                string filepath = Path.Combine("People", img.FilePath.TrimStart('/'));
                                await TmdbContext.DownloadImage(img.FilePath, filepath, cancellationToken);
                            }
                        }
                    }
                }
            }
        }

        public static async Task<List<MyMovie>> SearchMovie(string name, CancellationToken cancellationToken)
        {
            List<MyMovie> rtn = new List<MyMovie>();

            using (var client = new ServiceClient(API_KEY))
            {
                var movies = await client.Movies.SearchAsync( HttpUtility.UrlEncode(name),null,false,null,true,1,cancellationToken);
               
                foreach (Movie m in movies.Results)
                {
                        var movie = await client.Movies.GetAsync(m.Id, null, true, cancellationToken);

                        rtn.Add( new MyMovie(movie)); // negative one indicates Temp for entry ambiguity search
                        
                        //foreach (var cast in movie.Credits.Cast.Take(5))
                        //{

                        ////var actor = Program.MovieDB.Actors.Where(a => a.Name ==  cast.Name).FirstOrDefault();
                        ////if (actor != null)
                        ////{

                        ////}
                        ////else
                        ////{
                        //    Person person = null;
                        //     Image img = null;
                        //    try
                        //    {
                        //        person = await client.People.GetAsync(cast.Id, true, cancellationToken);
                        //        rtn.Credits.Cast.Add( new MyMediaCast(cast));
                        //        img = person.Images.Results.First();
                        //        string filepath = Path.Combine("Actors", img.FilePath.TrimStart('/'));
                        //        await DownloadImage(img.FilePath, filepath, cancellationToken);
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //    string junk = ex.Message;
                        //    }

                        //    //}
                            
                        //}
                         
                    }
                
            }

            return rtn;
        }

        
      
    }
}
