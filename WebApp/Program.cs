using RedCorona.Net;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
 


namespace GCS
{
    class Program
    {
        public static bool ConsoleLog = false;
        public static MovieDatabase MovieDB = null;
        private static ConsoleEventDelegate handler;    
        private static Process mistServer = new Process();
        
        // User for MistServer process(s) clean up on Program Shutdown
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        
        public static bool OffLine = true;


        public static void Main(/*string[] args*/)
        {
            if (ConfigurationManager.AppSettings["ConsoleLog"] != null)
                ConsoleLog = bool.Parse(ConfigurationManager.AppSettings["ConsoleLog"]);

            Console.Title = "VOD Web Site ...";  
         
            Console.WriteLine("VOD Movie Database - Launch Loading Movies from EMDB database");

            string drive = Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location);
            

            if (LaunchMist())
            {
                SetupDatabase(/*args*/);

                // start web service
                HttpServer http = new HttpServer(new Server(Convert.ToInt32(ConfigurationManager.AppSettings["MoviePort"])));
                http.Handlers.Add(new SubstitutingFileReader());


                Console.Title = string.Format("{0}:{1}", GetLocalIP(), ConfigurationManager.AppSettings["MoviePort"]);

                Console.WriteLine(
                    string.Format("Video On Demand Site Available at {0}", Console.Title));
                Console.WriteLine("\n help is available...\n\n");

                handler = new ConsoleEventDelegate(OnConsoleClose);
                SetConsoleCtrlHandler(handler, true);

                CommandLoop();  // keep running until told otherwise

                Process.GetProcesses()
                              .Where(x => x.ProcessName.StartsWith("Mist", StringComparison.OrdinalIgnoreCase))
                              .ToList()
                              .ForEach(x => x.Kill());
            }
            else
                Console.WriteLine("Mist Server Launch Failed!");
        }

        static bool SetupDatabase(/*string[] args*/)
        {
            OffLine = true;
            try
            {
                MovieDB = new MovieDatabase();
                MovieDB.Load();

                Console.WriteLine(string.Format("As Of {0}, Movie Count: {1}", MovieDB.LastUpdate.ToShortTimeString(), MovieDB.Movies.Count));
                MovieDB.LoadStreams();
                Console.WriteLine(string.Format("Stream Count: {0}", MovieDB.Streams.Count));

                //if (args != null && args.Length > 0 && (args.Contains("-s") || args.Contains("-S")))
                //{
                //    Console.WriteLine("Using PreRegistered Movies!");
                //
                //}
                //else if (args == null || args.Length == 0)
                {
                    string prompt = @"Registering Movies With Mist Server!";
                    Console.WriteLine(prompt);

                    MovieDB.MistSync();
                    MovieDB.Streams.Clear();
                    MovieDB.Directors.Clear();
                }
            }
            catch (Exception ex)
            {
                //_Spinner.Stop();
                Console.WriteLine(ex.Message);
                return false;
            }

            OffLine = false;
            return true;
        }

        static void CommandLoop()
        {
            string cmd = string.Empty;

            while (cmd.ToLower() != "exit")
            {
                Console.Write(": ");
                cmd = Console.ReadLine().ToLower();
                switch (cmd)
                {
                    case "offline": OffLine = !OffLine; 
                                    Console.WriteLine("Site is now : " + ((OffLine) ? "OffLine" : "OnLine")); 
                                    break;
                    case "config": Console.WriteLine(string.Format("Video On Demand Site Available at {0}", Console.Title)); break;
                    case "movies": Console.WriteLine(string.Format("As Of {0}, Movie Count: {1}", MovieDB.LastUpdate.ToShortTimeString(), MovieDB.Movies.Count)); break;
                    case "streams": MovieDB.LoadStreams(); Console.WriteLine(string.Format("Stream Count: {0}", MovieDB.Streams.Count)); break;
                    case "sync":
                                Console.WriteLine("ReLoad Movies from EMDB database");
                                SetupDatabase(/*null*/);
                                break;
                    case "help": Console.WriteLine("\n\tCommands\n\t\tConfig = Host Endpoint\n\t\tMovies = Movie Available\n\t\tStreams = Streams Available\n\t\tSync = Sync Movies and Streams\n\t\tOffLine = Toggle Web Site\n\t\tExit"); break;
                    case "exit": break;
                    default: Console.WriteLine("Invalid Command:  try help..."); break;
                }
            }
        }

        public static bool OnConsoleClose(int eventType)
        {
            if (eventType == 2)
            {
                if (mistServer != null)
                {
                    try
                    {
                        Console.WriteLine("Shutting Down Mist Server - where possible!");
                        mistServer.Kill();   // kill the controller first
                        Process.GetProcesses()
                             .Where(x => x.ProcessName.StartsWith("Mist", StringComparison.OrdinalIgnoreCase))
                             .ToList()
                             .ForEach(x => x.Kill());
                    }
                    catch { }
                }     
            }
            return false;
        }

        static public void KillMistUtils()
        {
            foreach (var process in Process.GetProcessesByName("Mist*"))
            {
                process.Kill();
            }
        }
        /// <summary>
        /// Using Google DNS get the active NIC on this machine 
        /// </summary>
        /// <returns></returns>
        static public string GetLocalIP()
        {
            string localIP = string.Empty;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }

        /// <summary>
        /// Calculates the lenght in bytes of an object 
        /// and returns the size 
        /// </summary>
        /// <param name="TestObject"></param>
        /// <returns></returns>
        static private int GetObjectSize(object TestObject)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            byte[] Array;
            bf.Serialize(ms, TestObject);
            Array = ms.ToArray();
            return Array.Length;
        }

        static public bool LaunchMist(bool visible=true)
        {
            string mistExe = ConfigurationManager.AppSettings["MistExe"];
            try
            {
                if (Process.GetProcessesByName("MistController*").Length == 0)   // Not found running...
                {
                    Console.WriteLine("Launching Mist Controller");              // so lets attempt to start it

                    mistServer = new Process();
                    mistServer.StartInfo.FileName = mistExe;
                    mistServer.StartInfo.WorkingDirectory = Path.GetDirectoryName(mistExe);
                    mistServer.StartInfo.RedirectStandardOutput = true;
                    mistServer.StartInfo.UseShellExecute = false;
                    mistServer.StartInfo.CreateNoWindow = !visible;
                    mistServer.Start();
                    //while (!mistServer.StandardOutput.EndOfStream)
                    //{
                    //    string line = mistServer.StandardOutput.ReadLine();
                    //    if (line.Contains("Writing config"))
                    //        return true;
                    //}
                    Thread.Sleep(3000);
                    return true;
                }
                else
                    return true;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
            return false;

        }

        
    }
}
