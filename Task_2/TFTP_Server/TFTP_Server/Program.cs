using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTP_Server
{
    class Program
    {
        static Server server;

        static void Main(string[] args)
        {
            Console.SetWindowSize(60, 15);
            try
            {
                server = new Server();
                server.Start();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
