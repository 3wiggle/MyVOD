using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VODMovieWebSite.TMDB
{
    public abstract class MyResource
    {
        public int Id { get; internal set; }
    }
}
