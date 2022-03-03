using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static Server server;
        static Thread listenThread;

        static void Main(string[] args)
        {
            Console.SetWindowSize(60, 15);
            Console.WriteLine("Server started. Listening.");
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
