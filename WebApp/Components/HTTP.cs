//#define DEBUG
// Embedded HTTP server, partial 1.1 support with keep-alive and session management
// REQUIRES: Sockets.dll (or Sockets.cs)

// v1.0

// HttpServer: Main class that implements a HTTP server on top of Sockets server.
//   Includes session management via cookies, keep-alive, UTF8 transfer of text
//   and simple query string parsing (via GET or POST).
// HttpRequest: Represents the request made by the user, with header fields,
//   query string and cookies pre-parsed. Typical server code will read the 
//   properties of this request to determine what to do.
// HttpResponse: The response which is to be sent back to the user. A simple
//   server will set the Content property, but you can send binary information
//   via RawContent, and change the mime type or response code.
// Session: A container into which you may put state information that will be
//  available in future requests from the same user.
// IHttpHandler: You must implement this interface in order to process
//   requests.
// SubstitutingFileHandler: An implementation of IHttpHandler that reads files
//   from disk and allows substitutions of <%pseudotags> within text documents.

// (C) Richard Smith 2008
//   bobjanova@gmail.com
// If downloaded from CodeProject, this file is subject to the CodeProject Open Licence 1.0.
// If downloaded from elsewhere, you may freely distribute the source code, as long as this
// header is not removed or modified. You may not charge for the source code or any compiled
// library that includes this class; however you may link to it from commercial software. Please
// leave a credit to the original download location in your documentation or About box.

// Simple HTTP server
using RedCorona.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace GCS
{
    public class HttpServer 
	{
	    private Server s;
		private Hashtable hostmap = new Hashtable();	// Map<string, string>: Host => Home folder
		private ArrayList handlers = new ArrayList();	// List<IHttpHandler>
		private Hashtable sessions = new Hashtable();	// Map<string,Session>
		
		private int sessionTimeout = 600;
		
		public Hashtable Hostmap { get { return hostmap; } }
		public Server Server { get { return s ; } }
		public ArrayList Handlers { get { return handlers; } }
		public int SessionTimeout 
		{
			get { return sessionTimeout; }
			set { sessionTimeout = value; CleanUpSessions(); }
		}
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

		public HttpServer(Server s)
		{

			this.s = s; 
			s.Connect += new ClientEvent(ClientConnect);
			handlers.Add(new FallbackHandler());
		}
		
		bool ClientConnect(Server s, ClientInfo ci)
		{
			ci.Delimiter = "\r\n\r\n";
			ci.Data = new ClientData(ci);
			ci.OnRead += new ConnectionRead(ClientRead);
			ci.OnReadBytes += new ConnectionReadBytes(ClientReadBytes);
			return true;
		}
		
		void ClientRead(ClientInfo ci, string text)
		{
			// Read header, if in right state
			ClientData data = (ClientData)ci.Data;
			if(data.state != ClientState.Header)
				return; // already done; must be some text in content, which will be handled elsewhere
			
			text = text.Substring(data.headerskip);
			if (Program.ConsoleLog)
			    Console.WriteLine("Read header: "+text+" (skipping first "+data.headerskip+")");
			data.headerskip = 0;
			string[] lines = text.Replace("\r\n", "\n").Split('\n');
			data.req.HeaderText = text;
			// First line: METHOD /path/url HTTP/version
			string[] firstline = lines[0].Split(' ');
			if(firstline.Length != 3)
			{ 
		         SendResponse(ci, data.req, new HttpResponse(400, "Incorrect first header line "+lines[0]), true); return; 
			}
			
			if(firstline[2].Substring(0, 4) != "HTTP")
			{ 
				SendResponse(ci, data.req, new HttpResponse(400, "Unknown protocol "+firstline[2]), true); return; 
			}
			
			data.req.Method = firstline[0];
			data.req.Url = firstline[1];
			data.req.HttpVersion = firstline[2].Substring(5);
			int p;
			for(int i = 1; i < lines.Length; i++)
			{
				p = lines[i].IndexOf(':');
				if(p > 0) 
					data.req.Header[lines[i].Substring(0, p)] = lines[i].Substring(p+2);
				else if (Program.ConsoleLog)
					Console.WriteLine("Warning, incorrect header line "+lines[i]);
			}
			
			// If ? in URL, split out query information
			p = firstline[1].IndexOf('?');
			if(p > 0)
			{
				data.req.Page = data.req.Url.Substring(0, p);
				data.req.QueryString = data.req.Url.Substring(p+1);
			} else 
			{
				data.req.Page = data.req.Url;
				data.req.QueryString = "";
			}

			Console.WriteLine(string.Format("{0}:{01} {2} | {3} ",DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(),data.req.From.ToString(),data.req.Page));
			
			if(data.req.Page.IndexOf("..") >= 0) 
			{ 
				SendResponse(ci, data.req, new HttpResponse(400, "Invalid path"), true); 
				return; 
			}			
			
			if(!data.req.Header.TryGetValue("Host", out data.req.Host))
			{ 
				SendResponse(ci, data.req, new HttpResponse(400, "No Host specified"), true); 
				return; 
			}			
			
			string cookieHeader;
			if(data.req.Header.TryGetValue("Cookie", out cookieHeader))
			{
				string[] cookies = cookieHeader.Split(';');
				foreach(string cookie in cookies)
				{
					p = cookie.IndexOf('=');
					if(p > 0)
					{
						data.req.Cookies[cookie.Substring(0, p).Trim()] = cookie.Substring(p+1);
					} else 
					{
						data.req.Cookies[cookie.Trim()] = "";
					}				
				}
			}
			
			string contentLengthString;
			if(data.req.Header.TryGetValue("Content-Length", out contentLengthString))
				data.req.ContentLength = Int32.Parse(contentLengthString);
			else  
				data.req.ContentLength = 0;
			
			 
			data.state = ClientState.PreContent;
			data.skip = text.Length + 4;
		}

		// allow use of EMDB working directory as virtual image directory for Web Site		
		public string GetMovieImagePath(string page)
        {
			string emdb = ConfigurationManager.AppSettings["ConnectionString"];
			string dir = Path.GetDirectoryName(emdb);
			return dir + page.Replace("/", "\\");
		}

		// Think of this a Server.Map
		public string GetFilename(HttpRequest req)
		{
			string folder = (string)hostmap[req.Host];
			if (folder == null)
				folder = WebRoot;
		
			string rtn = string.Empty;
			

			if (req.Page == "/")
			{
				if (ConfigurationManager.AppSettings["defaultDoc"] == null)
					rtn = Path.Combine(folder, @"\index.htm");
				else
					rtn = Path.Combine(folder, ConfigurationManager.AppSettings["defaultDoc"]);
			}
			else
				rtn = folder + req.Page;

			return rtn.Replace("/", "\\");
		}
		
		// entry point for new client request
		void DoProcess(ClientInfo ci)
		{
            ClientData data = (ClientData)ci.Data;
           

            if (Program.OffLine)
            {
                HttpResponse resp = new HttpResponse();
                resp.Url = data.req.Url;

                string fn = @"503.htm";
                resp.ContentType = "text/html";
                resp.ReturnCode = 503;
                StreamReader sr = new StreamReader(fn);
                resp.Content = sr.ReadToEnd();
                sr.Close();

                SendResponse(ci, data.req, resp, true);

            }


            
			string sessid;
			
			if(data.req.Cookies.TryGetValue("_sessid", out sessid))
				data.req.Session = (Session)sessions[sessid];
			try
			{
				bool closed = Process(ci, data.req);
				data.state = closed ? ClientState.Closed : ClientState.Header;
				data.read = 0;
				HttpRequest oldreq = data.req;
				data.req = new HttpRequest(); // Once processed, the connection will be used for a new request
				data.req.Session = oldreq.Session; // ... but session is persisted
				data.req.From = ((IPEndPoint)ci.Socket.RemoteEndPoint).Address;
			}
			catch
			{ }
		}
		
		void ClientReadBytes(ClientInfo ci, byte[] bytes, int len)
		{
			CleanUpSessions();
			int ofs = 0;
			ClientData data = (ClientData)ci.Data;
			if (Program.ConsoleLog)
				Console.WriteLine("Reading "+len+" bytes of content, in state "+data.state+", skipping "+data.skip+", read "+data.read);
			switch(data.state)
			{
				case ClientState.Content: break;
				case ClientState.PreContent: 
					data.state = ClientState.Content; 					
					if((data.skip - data.read) > len) 
					{ 
						data.skip -= len; 
						return; 
					}
					
					ofs = data.skip - data.read; 
					data.skip = 0;
					break;
				//case ClientState.Header: data.read += len - data.headerskip; return;
				default: data.read += len; return;
			}
			data.req.Content += Encoding.Default.GetString(bytes, ofs, len-ofs);
			data.req.BytesRead += len - ofs;
			data.headerskip += len - ofs;

#if DEBUG
			if (Program.ConsoleLog)
				Console.WriteLine("Reading "+(len-ofs)+" bytes of content. Got "+data.req.BytesRead+" of "+data.req.ContentLength);
#endif
			
			if(data.req.BytesRead >= data.req.ContentLength)
			{
				if(data.req.Method == "POST")
				{
					if(data.req.QueryString == "")data.req.QueryString = data.req.Content;
					else data.req.QueryString += "&" + data.req.Content;
				}
				ParseQuery(data.req);
				DoProcess(ci);
			}
		}
		
		void ParseQuery(HttpRequest req)
		{
			if(req.QueryString == "") 
				 return;
			string[] sections = req.QueryString.Split('&');
			for(int i = 0; i < sections.Length; i++)
			{
				int p = sections[i].IndexOf('=');
				if(p < 0) 
					req.Query[sections[i]] = "";
				else 
					req.Query[sections[i].Substring(0, p)] = URLDecode(sections[i].Substring(p+1));
			}
		}
		
		public static string URLDecode(string input)
		{			
			StringBuilder output = new StringBuilder();
			int p;
			while((p = input.IndexOf('%')) >= 0)
			{
				output.Append(input.Substring(0, p));
				string hexNumber = input.Substring(p + 1, 2);
				input = input.Substring(p + 3);
				output.Append((char)int.Parse(hexNumber, System.Globalization.NumberStyles.HexNumber));
			}
			return output.Append(input).ToString();
		}
		
		// HANDLE request as stripped from client session
		protected virtual bool Process(ClientInfo ci, HttpRequest req)
		{
			HttpResponse resp = new HttpResponse();
			resp.Url = req.Url;

			
			for(int i = handlers.Count - 1; i >= 0; i--)
			{
				IHttpHandler handler = (IHttpHandler)handlers[i];
				if(handler.Process(this, req, resp))
				{
					SendResponse(ci, req, resp, resp.ReturnCode != 200);
					return resp.ReturnCode != 200;
				}
			}
			return true;
		}
		
		enum ClientState { Closed, Header, PreContent, Content };
		class ClientData 
		{
			internal HttpRequest req = new HttpRequest();
			internal ClientState state = ClientState.Header;
			internal int skip, read, headerskip;
			
			internal ClientData(ClientInfo ci)
			{
				req.From = ((IPEndPoint)ci.Socket.RemoteEndPoint).Address;
			}
		}
		
		public Session RequestSession(HttpRequest req)
		{
			if(req.Session != null)
			{
				if(sessions[req.Session.ID] == req.Session) return req.Session;
			}
			
			req.Session = new Session(req.From);
			sessions[req.Session.ID] = req.Session;
			return req.Session;
		}
		
		void CleanUpSessions()
		{
			ICollection keys = sessions.Keys;
			ArrayList toRemove = new ArrayList();
			foreach(string k in keys){
				Session s = (Session)sessions[k];
				int time = (int)((DateTime.Now - s.LastTouched).TotalSeconds);
				if(time > sessionTimeout)
				{
					toRemove.Add(k);
					if (Program.ConsoleLog)
						Console.WriteLine("Removed session "+k);
				}
			}
			foreach(object k in toRemove) sessions.Remove(k);
		}
		
		// Response stuff
		static Hashtable Responses = new Hashtable();
		static HttpServer()
		{
			Responses[200] = "OK";
			Responses[302] = "Found";
			Responses[303] = "See Other";
			Responses[400] = "Bad Request";
			Responses[404] = "Not Found";
			Responses[500] = "Misc Server Error";
			Responses[502] = "Server Busy";
		}
		
		void SendResponse(ClientInfo ci, HttpRequest req, HttpResponse resp, bool close)
		{
#if DEBUG
			if (Program.ConsoleLog)
				Console.WriteLine("Response: "+resp.ReturnCode + Responses[resp.ReturnCode]);
#endif
			ByteBuilder bb = new ByteBuilder();
			bb.Add(Encoding.UTF8.GetBytes("HTTP/1.1 " + resp.ReturnCode + " " + Responses[resp.ReturnCode] + 
			        "\r\nDate: "+DateTime.Now.ToString("R")+
			        "\r\nServer: RedCoronaEmbedded/1.0"+
			        "\r\nConnection: "+(close ? "close" : "Keep-Alive")));
			if(resp.RawContent == null ) 
				bb.Add(Encoding.UTF8.GetBytes("\r\nContent-Encoding: utf-8"+
					"\r\nContent-Length: "+resp.Content.Length));
			else 
				bb.Add(Encoding.UTF8.GetBytes("\r\nContent-Length: "+resp.RawContent.Length));
			if(resp.ContentType != null)
				bb.Add(Encoding.UTF8.GetBytes("\r\nContent-Type: "+resp.ContentType));
			if(req.Session != null) bb.Add(Encoding.UTF8.GetBytes("\r\nSet-Cookie: _sessid="+req.Session.ID+"; path=/"));
			foreach(KeyValuePair<string, string> de in resp.Header) bb.Add(Encoding.UTF8.GetBytes("\r\n" + de.Key + ": " + de.Value));
			bb.Add(Encoding.UTF8.GetBytes("\r\n\r\n")); // End of header
			if(resp.RawContent != null) bb.Add(resp.RawContent);
			else bb.Add(Encoding.UTF8.GetBytes(resp.Content));
			ci.Send(bb.Read(0, bb.Length));
#if DEBUG
			if (Program.ConsoleLog)
				Console.WriteLine("** SENDING\n"+resp.Content);
#endif
			if(close) ci.Close();
		}
		
		class FallbackHandler : IHttpHandler 
		{
			public bool Process(HttpServer server, HttpRequest req, HttpResponse resp)
			{
 				Console.WriteLine("Processing "+req);
 
				server.RequestSession(req);
				StringBuilder sb = new StringBuilder();
				sb.Append("<h3>Session</h3>");
				sb.Append("<p>ID: "+req.Session.ID+"<br>User: "+req.Session.User);
				sb.Append("<h3>Header</h3>");
				sb.Append("Method: "+req.Method+"; URL: '"+req.Url+"'; HTTP version "+req.HttpVersion+"<p>");
				foreach(KeyValuePair<string, string> ide in req.Header) sb.Append(" "+ide.Key +": "+ide.Value+"<br>");
				sb.Append("<h3>Cookies</h3>");
				foreach(KeyValuePair<string, string> ide in req.Cookies) sb.Append(" "+ide.Key +": "+ide.Value+"<br>");
				sb.Append("<h3>Query</h3>");
				foreach(KeyValuePair<string, string> ide in req.Query) sb.Append(" "+ide.Key +": "+ide.Value+"<br>");
				sb.Append("<h3>Content</h3>");
				sb.Append(req.Content);
				resp.Content = sb.ToString();
				return true;
			}
		}
	}
	
	public class HttpRequest 
	{
		public bool GotHeader = false;
		public string Method, Url, Page, HttpVersion, Host, Content, HeaderText, QueryString;
		public IPAddress From;
		//public byte[] RawContent;
		public Dictionary<string, string> Query = new Dictionary<string, string>(), Header = new Dictionary<string, string>(), Cookies = new Dictionary<string, string>();		
		
		public int ContentLength, BytesRead;
		public Session Session;
	}
	
	public class HttpResponse 
	{
		public int ReturnCode = 200;
		public Dictionary<string, string> Header = new Dictionary<string, string>();
		public string Url, Content, ContentType = "text/html";
		public byte[] RawContent = null;
		
		public HttpResponse()
		{
		    //assuming  browser has enabled cache
			Header["Expires"] = DateTime.UtcNow.Add(TimeSpan.FromMinutes(60*4)).ToString("R");
			Header["Cache-Control"] = "max-age=14400"; //four hours
		}

		public HttpResponse(int code, string content)
		{
			Header["Expires"] = DateTime.UtcNow.Add(TimeSpan.FromMinutes(60 * 4)).ToString("R");
			Header["Cache-Control"] = "max-age=14400"; //four hours
			ReturnCode = code; 
			Content = content; 
		}
		
		public void MakeRedirect(string newurl)
		{
			ReturnCode = 303;
			Header["Location"] = newurl;
			Content = "This document is requesting a redirection to <a href="+newurl+">"+newurl+"</a>";
		}
	}
	
	public interface IHttpHandler 
	{
		bool Process(HttpServer server, HttpRequest request, HttpResponse response);
	}	
	
	public class Session 
	{
		string id;
		IPAddress user;
		DateTime lasttouched;
		
		Hashtable data = new Hashtable();
		
		public string ID { get { return id; } }
		public DateTime LastTouched { get { return lasttouched; } }
		public IPAddress User { get { return user; } }
		
		public object this[object key]{
			get { return data[key]; }
			set { data[key] = value; Touch(); }
		}
		
		public Session(IPAddress user)
		{
			this.user = user;
			this.id = Guid.NewGuid().ToString();
			Touch();
		}
		
		public void Touch()
		{ 
			lasttouched = DateTime.Now; 
		}
	}
	
	// our main work horse... 
	// takes the HTML content and performs Text Subsitution on our defined placeholders.
	public class SubstitutingFileReader : IHttpHandler 
	{
		// Reads a file, and substitutes <%x>
		HttpRequest req;
		bool substitute = true;
		
		public bool Substitute { get { return substitute; } set { substitute = value; } }
		
		public static Hashtable MimeTypes;
		
		static SubstitutingFileReader()
		{
			MimeTypes = new Hashtable();
			MimeTypes[".html"] = "text/html";
			MimeTypes[".htm"] = "text/html";
			MimeTypes[".css"] = "text/css";
			MimeTypes[".js"] = "application/x-javascript";
			
			MimeTypes[".png"] = "image/png";
			MimeTypes[".gif"] = "image/gif";
			MimeTypes[".jpg"] = "image/jpeg";
			MimeTypes[".jpeg"] = "image/jpeg";
		}
		
		public virtual bool Process(HttpServer server, HttpRequest request, HttpResponse response)
		{
			Program.MovieDB.CurrentGenre = string.Empty;

			lock (this)
			{
				string fn = server.GetFilename(request);
				string ext = Path.GetExtension(fn);
				string mime = (string)MimeTypes[ext];
				if (mime == null)
					mime = "application/octet-stream";
				response.ContentType = mime;

				if (mime.StartsWith("image") &&
					  (fn.Contains("covers") ||
						fn.Contains("backdrops") ||
						fn.Contains("actors")
					  )
				   )
					fn = server.GetMovieImagePath(request.Page);

				if (!File.Exists(fn))
				{
					string dirName = Path.GetDirectoryName(server.GetFilename(request)).ToLower();
					if (dirName.Contains("covers") || dirName.Contains("backdrops") || dirName.Contains("actors"))
					{
						fn = Path.Combine(dirName, "no-image.png");
					}
					else if (dirName.EndsWith("movie"))
					{
						fn = Path.GetDirectoryName(fn) + ".htm";
					}
					else if (dirName.EndsWith("genres"))
					{

						Program.MovieDB.CurrentGenre = Path.GetFileNameWithoutExtension(fn);
						fn = Path.GetDirectoryName(fn) + ".htm";
					}
					else
					{
						response.ReturnCode = 404;
						response.Content = "File not found.";
						return true;
					}
				}

				try
				{
					if (mime.Substring(0, 5) == "text/")
					{
						// Mime type 'text' is substituted
						StreamReader sr = new StreamReader(fn);
						response.Content = sr.ReadToEnd();
						sr.Close();
						if (substitute)
						{
							// Do substitutions
							lock (this)
							{
								req = request;
								response.Content = DoSubs(response.Content);
							}
						}
					}
					else
					{
						FileStream fs = File.Open(fn, FileMode.Open);
						byte[] buf = new byte[fs.Length];
						fs.Read(buf, 0, buf.Length);
						fs.Close();
						response.RawContent = buf;
					}
				}
				catch (Exception e)
				{
					response.ReturnCode = 500;
					response.Content = "Error reading file: " + e;
					return true;
				}
			}
			return true;
		}	
		
		public virtual string GetValue(HttpRequest req, string tag)
		{
			return "<span class=error>Unknown substitution: "+tag+"</span>";
		}
		
		protected string DoSubs(string template  )
		{
			 
			Movie movie = null;
			int movieID = -1;
			if (req.Query.Count > 0)
			{
				if (req.Query.ContainsKey("m"))
				{
					movieID = Convert.ToInt32(req.Query["m"]);
					movie =  Program.MovieDB.Movies.Where(m=> m.ID == movieID).FirstOrDefault();
				}
				else if (req.Query.ContainsKey("g"))
				{
					Program.MovieDB.CurrentGenre = req.Query["g"];
					template = template.Replace("#CURRENT_GENRE#", Program.MovieDB.CurrentGenre);
					template = template.Replace("#GENRE_MOVIE_LIST#", Program.MovieDB.GenreMovieList);
				}
			}


			template = template.Replace("#DATE#", Program.MovieDB.LastUpdate.ToShortDateString());
			template = template.Replace("#TOTAL#", Program.MovieDB.Movies.Count.ToString());
			template = template.Replace("#STREAMS#", Program.MovieDB.Streams.Count.ToString());
			template = template.Replace("#GENRES_MENU#", Program.MovieDB.GenresHtml);
			template = template.Replace("#YEARS_MENU#", Program.MovieDB.YearsHtml);
			template = template.Replace("#MOVIES_THUMB#", Program.MovieDB.MoviesHtml);
			template = template.Replace("#COPYRIGHT#", ConfigurationManager.AppSettings["copyright"]);
			template = template.Replace("#YEAR_SECTIONS#", Program.MovieDB.YearSectionsHtml);
			

			if (movie != null)
			{
				template = template.Replace("#PAGE_TITLE#", movie.Title);
				template = template.Replace("#LINK_CURRENT_MOVIE#", req.Url);
				template = template.Replace("#TITLE#", movie.Title);
				template = template.Replace("#ALTERNATIVE_TITLE#", movie.AKA);
				template = template.Replace("#YEAR#", movie.Year.ToString());
				template = template.Replace("#MOVIE_ID#", movie.ID.ToString());
				template = template.Replace("#COVER#", movie.CoverimageKey);
				template = template.Replace("#IMDB_LINK#", movie.TrailerLink);
				template = template.Replace("#COUNTRY#", movie.Country);
				template = template.Replace("#DIRECTOR#", (movie.Director >=0) ? Program.MovieDB.Directors[movie.Director]:string.Empty);
				template = template.Replace("#DURATION#", movie.Duration.ToString());
				template = template.Replace("#GENRES#", movie.Genres);
				template = template.Replace("#PLOTOUTLINE#", movie.Plot);
				template = template.Replace("#CAST_LIST#", movie.CastHtml);
				template = template.Replace("#LOCATION#", movie.Location);
				template = template.Replace("#VOD_LINK#", movie.VodLink); // movie.StreamLink);

				template = template.Replace("#STREAMLINK#", movie.StreamLink);
				template = template.Replace("#SOURCENAME#", movie.SourceName);
				template = template.Replace("#POSTERLINK#", movie.PosterLink);
				template = template.Replace("#PLAYERLINK#", movie.PlayerLink);
			}

			return template;

		}
	}
}
