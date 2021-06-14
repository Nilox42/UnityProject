using System;
using System.Threading;

namespace UnityServer
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            Console.Title = "Server";
            isRunning = true;

            Thread mainthread = new Thread(new ThreadStart(MainThread));
            mainthread.Start();

            Server.Start(50, 26950);
        }


        private static void MainThread()
        {
            Console.WriteLine($"Main Thread starded. Running at {Constants.TICKS_PER_SEC} ticks per second");
            DateTime nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    nextLoop = nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }
}
