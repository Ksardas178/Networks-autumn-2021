using System;
using System.Threading;

namespace TCP_Server
{
    class Program
    {
        static Server server;
        static Thread listenThread;
        static void Main(string[] args)
        {
            Console.SetWindowSize(60, 15);
            try
            {
                server = new Server();
                listenThread = new Thread(new ThreadStart(server.Listen));
                listenThread.Start();
            }
            catch (Exception e)
            {
                server.Disconnect();
                Console.WriteLine(e.Message);
            }
        }
    }
}