using GCS;
using System.ServiceProcess;
using System.Threading;

namespace VODMovieWebSite
{
    partial class VodService : ServiceBase
    {
        public VodService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
             
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //Program.Start();  
            }).Start();
        }

        protected override void OnStop()
        {
            Program.OnConsoleClose(2);
        }
    }
}
