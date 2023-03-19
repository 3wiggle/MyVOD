using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
 
using System.Linq;
using System.Globalization;
using System.Configuration;
 
using System.Reflection;

namespace GCS
{
    enum LoadState { None,Version,Movies,Actors,Directors,TheEnd}
    [Serializable]
    public class MovieDatabase
    {
        public MistAPI mMist = null;
        public MistAPI Mist 
        { 
            get
            {
                if (mMist == null)
                {
                    string ep = null;
                    try
                    {
                        ep = string.Format(ConfigurationManager.AppSettings["MistAPI"], Program.GetLocalIP());
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Mist Server error!  Sync not possible...", ex);
                    }

                    mMist = new MistAPI() { EndPoint = ep };
                }

                return mMist;
            }
         
        }
        public List<MistStream> Streams { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Version {get; set;}
        public List<Movie> Movies {get; set;}
        public List<Genre> Genres { get; set; }
        public List<Actor> Actors { get; set; }
        public List<string> Directors { get; set; }

        private string mWebRoot = string.Empty;
        public string WebRoot
        {
            get
            {
                if (string.IsNullOrEmpty(mWebRoot))
                {
                    if (ConfigurationManager.AppSettings["webRoot"] != null)
                        mWebRoot = ConfigurationManager.AppSettings["webRoot"];
                    else
                    {
                        mWebRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    }
                }

                return mWebRoot;

            }
        }

        private int[] _years = null; 
        public int[] Years 
            { 
              get 
                {
                if (_years == null)
                {
                    _years = Program.MovieDB.Movies.Where(o => o.Year > 0).Select(o => o.Year).Distinct().OrderByDescending(o => o).ToArray();
                    //Array.Sort(_years);
                    //Array.Reverse(_years);
                }
                   return _years;
                } 
            }
        
        public string GenresHtml 
        { 
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (Genre g in Genres)
                    sb.Append(string.Format(VODMovieWebSite.Properties.Resource1.GenreTemplate, g.Name) + Environment.NewLine);
                return sb.ToString();
            }
        }

        public string YearsHtml
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (int y in Program.MovieDB.Years)
                    sb.Append(string.Format(VODMovieWebSite.Properties.Resource1.YearTemplate, y) + Environment.NewLine);
                return sb.ToString();
            }
        }

        public string MoviesHtml
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                //int i = 1;
                foreach (Movie m in Movies)
                {
                 
                        sb.Append(
                          string.Format(VODMovieWebSite.Properties.Resource1.MovieTemplate, m.ID, m.CoverimageKey, m.Title)
                       );
                    
                }
                return sb.ToString();
            }
        }

        public string YearSectionsHtml
        {
            get 
            {
                 
                StringBuilder sb = new StringBuilder();
                foreach(int y in Years )
                    sb.Append(yearSection(y));
                return sb.ToString();
            }
        }

        public string CurrentGenre { get; set;  }
        
        public string GenreMovieList
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var m in  Movies)
                {
                   if (m.Genres.Contains(CurrentGenre))
                       sb.Append(
                               string.Format(VODMovieWebSite.Properties.Resource1.MovieTemplate, m.ID, m.CoverimageKey, m.Title));
                    
                }
                return sb.ToString();
            }
        }

        // EMDB has convoluted data record layout.. 
        // we parse from Top Down... keeping track of what 'section' we are currently reading...
        // the section name becomes the STATE for data parsing needs...
        private LoadState state = LoadState.None;

        private string yearSection(int year)
        {
            return VODMovieWebSite.Properties.Resource1.YearSectionTemplate.
                Replace("#YEAR#", year.ToString()).
                Replace("#YEAR_SECTION#", MoviesForYearSection(year));
        }

        private string MoviesForYearSection(int year)
        {
            StringBuilder sb = new StringBuilder();
            var movies = from Movie m in Program.MovieDB.Movies where m.Year == year select m;
            foreach (Movie m in movies)
                sb.Append(
                    string.Format(VODMovieWebSite.Properties.Resource1.MovieTemplate, 
                                        m.ID, m.CoverimageKey,m.Title)
                         );

            return sb.ToString();
        }

        public MovieDatabase()
        {
            Streams = new List<MistStream>();
            Movies = new List<Movie>();
            Actors = new List<Actor>();
            Directors = new List<string>();
            CreateGenres();
        }
       
        private void CreateGenres()
        {
           Genres = new List<Genre>();
           Genres.Add(new Genre('A',"Action"));
           Genres.Add(new Genre('T',"Thriller"));
           Genres.Add(new Genre('H',"Horror"));
           Genres.Add(new Genre('D',"Drama"));
           Genres.Add(new Genre('S',"Sci-Fi"));
           Genres.Add(new Genre('M',"Music"));
           Genres.Add(new Genre('K',"Comedy")); 
           Genres.Add(new Genre('O',"War"));
           Genres.Add(new Genre('V',"Adventure"));
           Genres.Add(new Genre('F',"Fantasy"));
           Genres.Add(new Genre('!',"Crime"));
           Genres.Add(new Genre('C',"Animation"));
           Genres.Add(new Genre('I',"Family"));
           Genres.Add(new Genre('U',"Documentary"));
           Genres.Add(new Genre('W',"Western"));
           Genres.Add(new Genre('G', "History"));
           Genres.Add(new Genre('R',"Romance"));
           Genres.Add(new Genre('N', "News"));
           Genres.Add(new Genre('Y',"Mystery"));
           Genres.Add(new Genre('P',"Sport"));
           Genres.Add(new Genre('G',"Game-Show"));
           Genres.Add(new Genre('@',"Biography"));
           Genres.Add(new Genre('X',"Adult"));
           Genres.Add(new Genre('s',"Short"));
           Genres.Add(new Genre('f',"Film-Noir"));
           Genres.Add(new Genre('m', "Musical"));
           Genres.Add(new Genre('t', "Talk-Show"));
           Genres.Add(new Genre('r', "Reality-TV"));
        }
         
        // remove leading and trailing double quotes - along with any spaces
        private string CleanString(string raw)
        {
            return raw.Replace('"', ' ').Trim();
        }

        /// <summary>
        /// Load Movie Database from EMDB database file
        /// </summary>
        public void Load()
        {
            string path = ConfigurationManager.AppSettings["ConnectionString"];

            if (!File.Exists(path))
                throw new Exception("Unable to open " + path);

            LastUpdate = File.GetLastWriteTime(path);

            List<string> data = new List<string>();
            List<char> buffer = new List<char>();

            Movie movie = null;
            Actor actor = null;
            bool inValue = false;  // first colon on line delimits Label from Value 
            char c;

            using (FileStream fs = File.OpenRead(path))
            {
                using (StreamReader streamReader = new StreamReader(fs, Encoding.UTF8))
                {
                    while (!streamReader.EndOfStream)
                    {
                        c = (char)streamReader.Read();

                        // the ordering of these conditionals is vital
                        if (c == ']')
                        {
                            state = LoadState.None;
                            buffer.Clear();
                            data.Clear();
                        }

                        // end or start of object
                        if (c == '}')  // end of some sort of data record
                        {
                            switch (state)
                            {
                                case LoadState.Movies:
                                    if (movie != null) //&& File.Exists(movie.Location))
                                        Movies.Add(movie);
                                    movie = null;
                                    break;
                                case LoadState.Actors:
                                    if (actor != null && !string.IsNullOrEmpty(actor.Name))
                                        Actors.Add(actor);
                                    actor = null;
                                    break;
                                case LoadState.Directors:  //Directors.Add(data[1]);                               
                                    break;
                            }

                            continue;
                        }

                        if (c == '{')  // start of record array
                        {
                            switch (state)
                            {
                                case LoadState.Movies:
                                    //if (movie != null)
                                    //    Movies.Add(movie);

                                    movie = new Movie() { ID = Movies.Count() + 1 };
                                    buffer.Clear();
                                    data.Clear();
                                    break;
                                case LoadState.Actors:
                                    if (actor != null && !string.IsNullOrEmpty(actor.Name))
                                        Actors.Add(actor);
                                    actor = new Actor() { Key = Actors.Count() + 1 };
                                    buffer.Clear();
                                    data.Clear();
                                    break;

                                case LoadState.Directors:
                                    buffer.Clear();
                                    data.Clear();
                                    break;
                            }

                            continue;
                        }
                        // test if we are on a label/data delimiter
                        if (c == ':' && inValue == false)  //inValue flag allows colons in value data
                        {
                            // contents of our read buffer contains the line label (if any)
                            string label = new string(buffer.ToArray()).Replace('"', ' ').Trim();

                            inValue = true;
                            buffer.Clear();
                            data.Add(label);

                            switch (label)
                            {
                                //movies is a JSON array - square brackets
                                // we do not have data on this line - just need to flag we are in the movie array
                                // or into the supporting data array(s)
                                case "movies": state = LoadState.Movies; break;
                                case "actors": state = LoadState.Actors; break;
                                case "directors": state = LoadState.Directors; break;
                            }
                            continue;
                        }

                        if (c == '\r')  // ignore carriage returns - and trigger on following new line
                            continue;

                        if (c == 0x1e)  // while reading Value data - we may have tuples delimited with 1E
                        {
                            if (buffer.Count > 0)
                                data.Add(CleanString(new string(buffer.ToArray())));
                            else
                                data.Add(string.Empty);  // necessary to keep ordinals in sync

                            buffer.Clear();
                            continue;
                        }

                        if (c == '\n')  // new line indicates end of data lines for record
                        {
                            if (inValue)  // finish up final data element
                                data.Add(new string(buffer.ToArray()).Replace('"', ' ').Trim());

                            if (data.Count > 0)  // do we have data???
                            {
                                if (state == LoadState.Movies)  // are we reading movie data?
                                {

                                    switch (data[0])           // which data line have we just received
                                    {
                                        case "title":
                                            movie.Title = data[1];
                                            movie.AKA = data[2];
                                            movie.Studio = data[3];
                                            break;
                                        case "year":
                                            if (!string.IsNullOrEmpty(data[3]))
                                                movie.Duration = TimeSpan.FromMinutes(Convert.ToInt32(data[3]));
                                            movie.Location = data[8];
                                            if (!string.IsNullOrEmpty(data[1]))
                                                movie.Year = Convert.ToInt32(data[1]);
                                            movie.Country = data[4].TrimStart('#');

                                            movie.CoverimageKey = String.Format("{0:D6}", Convert.ToInt32(data[15])) + ".jpg";
                                            break;
                                        case "cast":
                                            if (!string.IsNullOrEmpty(data[1]))
                                            {
                                                List<int> nums = new List<int>();
                                                string[] x = data[1].Split(',');
                                                foreach (string s in x)
                                                    nums.Add(Convert.ToInt32(s));
                                                movie.ActorKeys = nums.ToArray();
                                            }
                                            break;
                                        case "plot": movie.Plot = data[1]; break;
                                        case "genres":
                                            if (!string.IsNullOrEmpty(data[1]))
                                            {
                                                char[] array = data[1].ToCharArray();
                                                movie.Genre = new List<Genre>();
                                                foreach (char g in array)
                                                {
                                                    var item = (from i in Genres where i.Code == g select i).FirstOrDefault();
                                                    if (item != null)
                                                        movie.Genre.Add(item);
                                                }
                                            }

                                            if (!string.IsNullOrEmpty(data[3]))
                                                movie.Added = DateTime.ParseExact(data[3], "yyyyMMdd", CultureInfo.InvariantCulture);
                                            movie.Certification = data[4];
                                            movie.IMDB = data[8];
                                            break;
                                        case "tagline": movie.TagLine = data[1]; break;

                                    }
                                }

                                if (state == LoadState.Actors)
                                {
                                    if (!string.IsNullOrEmpty(data[0]))
                                    {
                                        switch (data[0])           // which data line have we just received
                                        {
                                            case "name": actor.Name = data[1].TrimEnd(',').Trim(); break;
                                            case "id": actor.ID = data[1]; break;
                                        }
                                    }
                                }

                                if (state == LoadState.Directors)
                                {
                                    if (!string.IsNullOrEmpty(data[0]))
                                    {
                                        switch (data[0])           // which data line have we just received
                                        {
                                            case "name": Directors.Add(data[1]); break;
                                        }
                                    }
                                }
                            }

                            inValue = false;
                            buffer.Clear();
                            data.Clear();
                        }
                        else
                            buffer.Add(c);
                    }
                }
            }

            Movies = SortMovies();

        
        }

        public List<Movie> SortMovies()
        {
            // Define the list of words to ignore in the sorting process
            List<string> ignoreWords = new List<string> { "the", "an", "a" };

            // Sort the movies list while ignoring the ignoreWords in the Title property
            List<Movie> sortedMovies = Movies.OrderBy(movie =>
            {
                // Split the Title property of the movie into words
                string[] words = movie.Title.Split(' ');

                // Remove any ignoreWords from the array of words
                words = words.Where(w => !ignoreWords.Contains(w.ToLower())).ToArray();

                // Concatenate the remaining words back into a string and return it
                return string.Join(" ", words);
            }).ToList();
            
            return sortedMovies;

        }

        /// <summary>
        /// Register all movies with Mist Server
        /// </summary>
        /// <returns>true / false</returns>
        public bool MistSync()
        {

            try
            {
                LoadStreams();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\r\n" + ex.Message);
                
            }
 
            int s = -1;
            try
            {
                foreach (Movie m in Movies)
                {

                    s = m.Location.ToUpper().IndexOf(@"<SCB>");
                    if (s >= 0 )
                    {
                        m.Location = m.Location.Substring(s+5);
                    }
                    
                    if (Streams.FirstOrDefault(x => x.Name.ToLower() == m.SourceName) == null)
                    {
                        string source = @"/cygdrive/" + m.Location.Replace(":", "").Replace("\\", "/");
                        try
                        {
                            Mist.AddStream(m.SourceName, source);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine("Mist Add: " + ex.Message);
                        }
                    }

                    if (string.IsNullOrEmpty(m.CoverimageKey))
                        continue;
                    
                    if (m.ActorKeys == null)
                        continue;
                    string junk = string.Empty;

                    foreach (int a in m.ActorKeys)
                    {
                        if (a < 0 || a >= Actors.Count)
                            continue;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Generic Movie: "+ ex.Message);
            }

            if (Program.ConsoleLog)
                Console.WriteLine("\r\nMist Server Stream Sync Complete!");

            Mist.SaveConfig();
            return true;
        }

        /// <summary>
        /// Retrieve all registered Steams in MIST
        /// </summary>
        public void LoadStreams()
        { 
             
            var streams = Mist.GetStreams();
            
            if (streams == null || streams.Count < 1)
            {
                if (Program.ConsoleLog) 
                    Console.WriteLine("Mist Server Stream Count = 0!");
               return;
            }

            Streams.Clear();

            foreach (var s in streams)
            { 
                MistStream ms = new MistStream();
                foreach (var p in s.Value)
                {
                    switch(p.Key)
                    {
                        case "error": ms.Error = p.Value; break;
                        case "name" : ms.Name = p.Value; break;
                        case "online": ms.Online = p.Value; break;
                        case "source": ms.Source = p.Value; break;

                    }
                }
                Streams.Add(ms);    
            }
        }

    }
    
    [Serializable]
    public class Movie
    {
        private string mLocalIP = null;
        private List<string> ignoreWords = new List<string> { "the", "an", "a" };
        public int ID { get; set; }
        public string Title { get; set; }
        public string Studio { get; set; }
        public string AKA { get; set; }
        public string Plot { get; set; }
        public string TagLine { get; set; }
        public int Year { get; set; }
        public TimeSpan Duration{ get; set; }
        public string Country{ get; set; }
        public string Location{ get; set; }
        public string LocalIP { 
                get 
                    { 
                        if (string.IsNullOrEmpty(mLocalIP)) 
                            mLocalIP = Program.GetLocalIP();
                        return mLocalIP;
                    } 
                set { mLocalIP = value; } }
        public string CoverimageKey{ get; set; }
        public List<Genre> Genre {get; set;}
        public string Genres 
        { 
            get 
            { 
               return string.Join(",", (from n in Genre select n.Name)); 
            } 
        }
        public string IMDB{get; set;} 
        public DateTime? Added {get; set;}
        public string Certification { get; set; }
        public int[] ActorKeys { get; set; }
        public int Director { get; set; }
        public string CastHtml 
        {
            get 
            {
                if (ActorKeys == null || ActorKeys.Length == 0)
                    return null;

                StringBuilder sb = new StringBuilder();
                string mask = VODMovieWebSite.Properties.Resource1.ActorTemplate;
                foreach(int i in ActorKeys)
                {
                    if (i < 0 || i >= Program.MovieDB.Actors.Count())
                        continue;

                    Actor a = Program.MovieDB.Actors[i];
                    string link = null;
                    if (a.ID.StartsWith("tmdb-"))
                        link = @"https://www.themoviedb.org/person/" + a.ID;
                    else
                        link = @"https://www.imdb.com/name/nm" + a.ID;
                    string junk = mask.Replace("#ACTOR_LINK#", link).
                                       Replace("#ACTOR_PHOTO#", a.ID + ".jpg").
                                       Replace("#ACTOR_NAME#", a.Name).
                                       Replace("#ACTOR_CHARACTER#", a.RoleHtml);
                    sb.Append(junk + Environment.NewLine);
                }
                return sb.ToString();
            }
        }
        public string TrailerLink 
        {
            get 
            {
                return string.Format(VODMovieWebSite.Properties.Resource1.TrailerTemplate, IMDB, Title);
            }
        }

        public string VodLink
        {
            get
            {
                return string.Format(string.Format(@"vod.htm?m={0}", ID));
            }
        }
 
        // clean up the movie title allowing it to be acceptable as Mist Stream Name
        // so Mist can find movie source file in StreamLink and as posting name into Mist
        public string SourceName
        {
            get
            {
               return ("gcs"+Title).
                        Replace(" ", "").        // replace whitespace with underscore
                        Replace("_", "").      // strip out underscores
                        Replace(":","").
                        Replace("*", "").
                        Replace("?", "").
                        Replace("\"", "").
                        Replace("<", "").
                        Replace(">", "").
                        Replace("'", "").
                        Replace("|", "").
                        Replace("/", "").
                        Replace("!","").
                        Replace(",","").
                        Replace("é","e").
                        Replace("à", "a").ToLower();
            }
        }

        public string StreamLink
        {
            get
            {
                return string.Format("http://{0}:{1}/{2}.html",
                    LocalIP,
                    ConfigurationManager.AppSettings["MistStreamPort"],
                    SourceName);
            }
        }
        public string PosterLink
        {
            get
            {
                return string.Format("http://{0}:{1}/backdrops/{2}",LocalIP, ConfigurationManager.AppSettings["MoviePort"], CoverimageKey);
            }
        }

        public string PlayerLink
        {
            get
            {
                return string.Format("http://{0}:{1}/player.js",LocalIP,ConfigurationManager.AppSettings["MistStreamPort"]);
            }
        }
        
        public Movie()
        {
            Genre = new List<Genre>();
            Director = -1;
        }
        // Reserved for future use: COnsidering direct connection to on line movie database  
        //public Movie(System.Net.TMDb.Movie mov, int id )
        //{
        //    this.ID = mov.Id;
        //    this.IMDB = mov.Imdb;
        //    this.Added = DateTime.Now;
        //    this.Year = (mov.ReleaseDate.HasValue) ? mov.ReleaseDate.Value.Year : 0 ;
        //    this.Country = mov.Countries.ToArray()[0].Name;
        //    this.Duration = new TimeSpan(0, (mov.Runtime.HasValue) ? mov.Runtime.Value : 0 , 0);
        //    this.Plot = mov.Overview;
        //    this.TagLine = mov.TagLine;
        //    this.Title = mov.Title;
        //    this.AKA = mov.OriginalTitle;

        //    //
        //    //this.CoverimageKey = We create time using same id as movie for both cover and poster;
        //}
    }

    [Serializable]
    public class MovieFeatureCodes
    {
        //Special Feature codes..
        //00-00-00-00-00---0
        //  |  |  |  |  |   | 
        //00|  |  |  |  |   | = ??
        //  -00|  |  |  |   | = ??
        //     -00|  |  |   | = Medium
        //        -00|  |   | = Source
        //           -00--- | = ??
        //                  | = ??
        private string _rawData = string.Empty;

        public string[] Items = null;
        public string Medium { get { return Items[2];} set { Items[2] = value;} }
        public string Source { get { return Items[3];} set { Items[3] = value;} }
        
        public MovieFeatureCodes(string raw)
        {
            _rawData = raw;
            Items = raw.Split('-');
        }
    }

    [Serializable]
    public class MovieGroupFlags
    {
        private string _rawData = string.Empty;

        public MovieGroupFlags(string raw)
        {
            _rawData = raw;

        }
    }
    [Serializable]
    public class Genre
    {
        public char Code { get; set; }
        public string Name { get; set; }
        public Genre(char code, string name)
        {
            this.Code = code;
            this.Name = name;
        }
    }
    [Serializable]
    public class Actor
    { 
        public int Key { get; set; }
        public string Name { get; set; }
        public string ID { get; set; }
        public string Role { get; set; }
        public string RoleHtml { get { if (string.IsNullOrEmpty(Role)) return string.Empty; else return "(" + Role + ")"; } }
       
    }
}