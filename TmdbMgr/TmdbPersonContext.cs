using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net.TMDb;

namespace VODMovieWebSite.TMDB
{


    public class TmdbPersonContext
    {
        private const string CreateSql = @"CREATE TABLE IF NOT EXISTS Person (ID INTEGER NOT NULL PRIMARY KEY, Name nvarchar[100], Adult int, KnownAs nvarchar[MAX], 
                                           Biography nvarchar[100], BirthDay nvarchar[100], DeathDay nvarchar[100],HomePage nvarchar[100],
                                           BirthPlace nvarchar[100],Poster nvarchar[100],
                                           Freebase nvarchar[100], FreebaseMid nvarchar[100], Imdb nvarchar[50], Tvdb nvarchar[50], Tvrage nvarchar[50]);";
        private const string DeleteSql = @"Delete from Person where ID={0} ;";
        private const string InsertSql = @"Insert INTO Person (ID,Name,Adult,KnownAs, Biography,BirthDay,DeathDay,HomePage,BirthPlace,Poster, Freebase, FreebaseMid, Imdb, Tvdb, Tvrage) 
                                            VALUES({0},{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}');";
        private const string ReplaceSql = @"Replace INTO Person (ID,Name,Adult,KnownAs, Biography,BirthDay,DeathDay,HomePage,BirthPlace,Poster, Freebase, FreebaseMid, Imdb, Tvdb, Tvrage) 
                                            VALUES({0},{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}');";
        private const string CreateIndex = @"CREATE UNIQUE INDEX idx_person_id ON Person (ID);";

        private const string DropSql = @"DROP TABLE IF EXISTS Person;";
        public List<MyPerson> Items { get; set; }


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

        public void Update(SQLiteConnection conn, int ID, string Name, bool Adult, string KnownAs, string Biography, string BirthDay, string DeathDay, string HomePage, string BirthPlace, string Poster)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(ReplaceSql, ID, 
                           TmdbContext.SafeString(Name), 
                           Adult, 
                           TmdbContext.SafeString(KnownAs), 
                           TmdbContext.SafeString(Biography), 
                           TmdbContext.SafeString(BirthDay), 
                           TmdbContext.SafeString(DeathDay), 
                           TmdbContext.SafeString(HomePage), 
                           TmdbContext.SafeString(BirthPlace), 
                           TmdbContext.SafeString(Poster));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Insert(SQLiteConnection conn, int ID, string Name, bool Adult, string KnownAs, string Biography, string BirthDay, string DeathDay, string HomePage, string BirthPlace, string Poster)
        {
            conn.Open();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = string.Format(InsertSql, ID, 
                                            TmdbContext.SafeString(Name), 
                                            Adult, 
                                            TmdbContext.SafeString(KnownAs), 
                                            TmdbContext.SafeString(Biography), 
                                            TmdbContext.SafeString(BirthDay), 
                                            TmdbContext.SafeString(DeathDay), 
                                            TmdbContext.SafeString(HomePage), 
                                            TmdbContext.SafeString(BirthPlace), 
                                            TmdbContext.SafeString(Poster));
            sqlite_cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Load(SQLiteConnection conn)
        {
            Items = new List<MyPerson>();

            conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM Person";

            SQLiteDataReader rdr = sqlite_cmd.ExecuteReader();
            while (rdr.Read())
            {
                MyPerson p = new MyPerson(rdr);
                p.Id = Convert.ToInt32(rdr[0]);
                Items.Add(p);
            }
            conn.Close();
        }
    }

    public class MyPerson
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Adult { get; set; }
        public List<string> KnownAs { get; set; }
        public string Biography { get; set; }
        public string BirthDay { get; set; }
        public string DeathDay { get; set; }
        public string HomePage { get; set; }
        public string BirthPlace { get; set; }
        public string Poster { get; set; }
        public MyPersonCredits Credits { get; set; }
        public MyPersonImages Images { get; set; }
        public MyExternalIds External { get; set; }

        public MyPerson()
        {

        }

        public MyPerson(Person person)
        {
            this.Id = person.Id;
            this.Name = person.Name;
            this.Adult = person.Adult;
            foreach (string s in person.KnownAs)
                this.KnownAs.Add(s);
            this.Biography = person.Biography;
            this.BirthDay = person.BirthDay;
            this.DeathDay = person.DeathDay;
            this.HomePage = person.HomePage;
            this.BirthPlace = person.BirthPlace;
            this.Poster = person.Poster;
            foreach (var c in person.Credits.Cast)
                this.Credits.Cast.Add(new MyPersonCast(c));
           
            foreach (var c in person.Credits.Crew)
                this.Credits.Crew.Add(new MyPersonCrew(c));
            
            foreach (var i in person.Images.Results)
                this.Images.Items.Add(new MyImage(i));

            this.External.Freebase = person.External.Freebase;
            this.External.FreebaseMid = person.External.FreebaseMid;
            this.External.Imdb = person.External.Imdb;
            this.External.Tvdb = (person.External.Tvdb.HasValue) ? person.External.Tvdb.Value : 0;
            this.External.Tvrage = (person.External.Tvrage.HasValue) ? person.External.Tvrage.Value : 0;
        }
    
        public MyPerson(SQLiteDataReader rdr)
        {
            this.Id = Convert.ToInt32(rdr[0]);
            this.Name = rdr[1].ToString();
            this.Adult =  (Convert.ToInt32(rdr[2]) == 0) ? false : true;
            foreach (string s in rdr[3].ToString().Split(','))
                this.KnownAs.Add(s.Trim());
            this.Biography = rdr[4].ToString();
            this.BirthDay = rdr[5].ToString();
            this.DeathDay = rdr[6].ToString();
            this.HomePage = rdr[7].ToString();
            this.BirthPlace = rdr[8].ToString();
            this.Poster = rdr[9].ToString();
            //foreach (var c in person.Credits.Cast)
            //    this.Credits.Cast.Add(new MyPersonCast(c));

            //foreach (var c in person.Credits.Crew)
            //    this.Credits.Crew.Add(new MyPersonCrew(c));

            //foreach (var i in person.Images.Results)
            //    this.Images.Items.Add(new MyImage(i));

            this.External.Freebase = rdr[10].ToString();
            this.External.FreebaseMid = rdr[11].ToString();
            this.External.Imdb = rdr[12].ToString();
            if (rdr[13] != null)
               this.External.Tvdb = Convert.ToInt32(rdr[13]);
            if (rdr[14] != null)
                this.External.Tvrage = Convert.ToInt32(rdr[14]);
        }
    }

    public class MyPersonCredit
    {
       
        public int Id { get;  set; }
        public string CreditId { get;  set; }
        public string Title { get;  set; }
        public string OriginalTitle { get;  set; }
        public string Poster { get;  set; }
        public DateTime? ReleaseDate { get;  set; }
        public bool Adult { get;  set; }
    }

    public class MyPersonCast : MyPersonCredit
    {
        public string Character { get; set; }

        public MyPersonCast()
        {

        }

        public MyPersonCast(PersonCast pc) 
        {
            Id = pc.Id;
            CreditId = pc.CreditId;
            Character = pc.Character;
            Title = pc.Title;
            OriginalTitle = pc.OriginalTitle;
            Poster = pc.Poster;
            ReleaseDate = pc.ReleaseDate;
            Adult = pc.Adult;
        }

        public MyPersonCast(SQLiteDataReader rdr)
        {
            Id = Convert.ToInt32(rdr[0]);
            CreditId = rdr[1].ToString();
            Character = rdr[2].ToString();
            Title = rdr[3].ToString();
            OriginalTitle = rdr[4].ToString();
            Poster = rdr[5].ToString();
            if (rdr[6] != null)
              ReleaseDate = DateTime.Parse(rdr[6].ToString());
            Adult = (Convert.ToInt32(rdr[7]) == 0) ? false:true;
        }
    }

    public class MyPersonCrew : MyPersonCredit
    {
        public string Department { get; set; }
        public string Job { get; set; }

        public MyPersonCrew()
        {

        }

        public MyPersonCrew(PersonCrew pc)
        {
            
            Id = pc.Id;
            CreditId = pc.CreditId;
            Title = pc.Title;
            OriginalTitle = pc.OriginalTitle;
            Poster = pc.Poster;
            ReleaseDate = pc.ReleaseDate;
            Adult = pc.Adult;
            Department = pc.Department;
            Job = pc.Job;
            
        }

        public MyPersonCrew(SQLiteDataReader rdr)
        {
            Id = Convert.ToInt32(rdr[0]);
            CreditId = rdr[1].ToString();
            Title = rdr[2].ToString();
            OriginalTitle = rdr[3].ToString();
            Poster = rdr[4].ToString();
            if (rdr[5] != null)
                ReleaseDate = DateTime.Parse(rdr[5].ToString());
            Adult = (Convert.ToInt32(rdr[6]) == 0) ? false : true;
            Department = rdr[7].ToString();
            Job = rdr[8].ToString();
        }
    }


    public class MyPersonCredits
    {
        
        public List<MyPersonCast> Cast { get; set; }
        public List<MyPersonCrew> Crew { get; set; }

        public MyPersonCredits()
        {
            Cast = new List<MyPersonCast>();
            Crew = new List<MyPersonCrew>();
        }
    }

    public class MyPersonImages
    {
        public List<MyImage> Items { get;  set; }
    }

    public class MyImage
    {
        
        public string FilePath { get;  set; }
        public short Width { get;  set; }
        public short Height { get;  set; }
        public string LanguageCode { get;  set; }
        public decimal AspectRatio { get;  set; }
        public decimal VoteAverage { get;  set; }
        public int VoteCount { get;  set; }

        public MyImage()
        { 
        }

        public MyImage(System.Net.TMDb.Image pi)
        {
            FilePath = pi.FilePath;
            Width = pi.Width;
            Height = pi.Height;
            LanguageCode = pi.LanguageCode;
            AspectRatio = pi.AspectRatio;
            VoteAverage = pi.VoteAverage;
            VoteCount = pi.VoteCount;
        }

    }

    public class MyMediaCredits
    {

        public List<MyMediaCast> Cast { get; set; }
        public List<MyMediaCrew> Crew { get; set; }

        public MyMediaCredits()
        {
            Cast = new List<MyMediaCast>();
            Crew = new List<MyMediaCrew>();
        }
    }

    public class MyMediaCredit
    {
        public int Id { get;  set; }
        public string CreditId { get;  set; }
        public string Name { get;  set; }
        public string Profile { get;  set; }
         
    }

    public class MyMediaCast : MyMediaCredit
    {
        public string Character { get; set; }

        public MyMediaCast()
        {

        }

        public MyMediaCast(MediaCast mc)
        {
            Id = mc.Id;
            CreditId = mc.CreditId;
            Name = mc.Name;
            Profile = mc.Profile;
        }

        public MyMediaCast(SQLiteDataReader rdr)
        {
            Id = Convert.ToInt32(rdr[0]);
            CreditId = rdr[1].ToString();
            Name = rdr[2].ToString(); 
            Profile = rdr[3].ToString();
        }
    }

    public class MyMediaCrew : MyMediaCredit
    {
        
        public string Department { get;  set; }
        public string Job { get;  set; }

        public MyMediaCrew()
        {

        }

        public MyMediaCrew(MediaCrew mc)
        {
            Id = mc.Id;
            CreditId = mc.CreditId;
            Name = mc.Name;
            Profile = mc.Profile;
            Department = mc.Department;
            Job = mc.Department;
        }

        public MyMediaCrew(SQLiteDataReader rdr)
        {
            Id = Convert.ToInt32(rdr[0]);
            CreditId = rdr[1].ToString();
            Name = rdr[2].ToString();
            Profile = rdr[3].ToString();
            Department = rdr[4].ToString();
            Job = rdr[5].ToString();
        }
    }

    public class MyExternalIds
    {
        
        public string Freebase { get; internal set; }
        public string FreebaseMid { get; internal set; }
        public string Imdb { get; internal set; }
        public int Tvdb { get; internal set; }
        public int Tvrage { get; internal set; }
    }
}
 