using System;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Server
{
    class Program
    {
        static Server server;
        //static Thread listenThread;

        static void Main(string[] args)
        {
            Console.SetWindowSize(60, 15);
            try
            {
                server = new Server();
                Task listenTask = new Task( () => server.RunServerAsync());
                listenTask.Start();
                Task.WaitAll(listenTask);
                Console.ReadLine();
            }
            catch (Exception e)
            {                
                Console.WriteLine(e.Message);
                Console.ReadLine();
                server.Disconnect();
            }
        }
    }
}