using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TFTP_Server
{
    class Server
    {
        static int SERVER_PORT = 69;
        List<Client> clients = new List<Client>();
        UdpClient listener = new UdpClient(SERVER_PORT);

        public async void Start()
        {
            Console.WriteLine("Server is running");

            while (true)
            {
                try
                {
                    await Recieve(await listener.ReceiveAsync());
                }
                catch { }
            }
        }

        internal void AddClient(Client client)
        {
            clients.Add(client);
        }

        internal Client SearchClient(string id)
        {
            //Get connection by ID
            Client client = clients.FirstOrDefault(c => c.id == id);
            return client;
        }

        protected internal void RemoveClient(string id)
        {
            //Get closed connection by ID
            Client client = clients.FirstOrDefault(c => c.id == id);
            //And delete it
            if (client != null)
            {
                clients.Remove(client);
            }
        }

        async Task Recieve(UdpReceiveResult receiveResult)
        {
            await Task.Yield();
            Client client = SearchClient(Client.GetId(receiveResult));
            if (client == null)
            {
                new Client(receiveResult, this);
            }
            else
            {
                /*Task recieveTask = Task.Run(() => */
                client.Process(receiveResult);
            }
        }

        public async void SendPacket(byte[] data, IPEndPoint clientSocket)
        {
            await listener.SendAsync(data, data.Length, clientSocket);
        }

    }
}
