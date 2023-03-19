using System;
using System.Security.Permissions;
using System.Threading;

namespace GCS
{

    public class Spinner : IDisposable
    {
        private const string Sequence = @"/-\|";
        private int counter = 0;
        private readonly int left;
        private readonly int top;
        private readonly int delay;
        private bool active;
        private readonly Thread thread;

        public Spinner(int left, int top, int delay = 100)
        {
            this.left = left;
            this.top = top;
            this.delay = delay;
            thread = new Thread(Spin);
        }

        public void Start()
        {
            active = true;
            if (!thread.IsAlive)
                thread.Start();
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        public void Stop()
        {
            active = false;
            if (thread != null)
                thread.Abort();

            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("{0,12}"," "));
        }

        private void Spin()
        {
            while (active)
            {
                Turn();
                Thread.Sleep(delay);
            }
        }

        private void Draw(char c)
        {
            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(c);
            //if (Program.AD_Read_Count > 0)
            //    Console.Write(string.Format("{0,10}", Program.AD_Read_Count));
        }

        private void Turn()
        {
            Draw(Sequence[++counter % Sequence.Length]);
        }

       
        public void Dispose()
        {
            if (active)
               Stop();
            
        }
    }

}