using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Server
{
    public class Server
    {
        List<Client> clients = new List<Client>();

        internal void AddConnection(Client client)
        {
            clients.Add(client);
        }

        protected internal void RemoveConnection(string id)
        {
            //Get closed connection by ID
            Client client = clients.FirstOrDefault(c => c.id == id);
            //And delete it
            if (client != null)
            {
                clients.Remove(client);
            }
        }

        //Listening input connections
        public async void RunServerAsync()
        {
            var listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            try
            {
                while (true)
                    await Accept(await listener.AcceptTcpClientAsync());
            }
            finally { listener.Stop(); }
        }


        async Task Accept(TcpClient tcpClient)
        {
            //Возврат управления вызывающему коду
            //await Task.Yield();
            try
            {
                Client client = new Client(tcpClient, this);
                await Task.Run(() => client.ProcessTask());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //Forward message to clients
        protected internal void BroadcastMessage(Message message, string id)
        {
            byte[] data = message.GetPacket();
            for (int i = 0; i < clients.Count; i++)
            {
                //If client's ID <> sender ID
                if (clients[i].id != id)
                {
                    //Transfer data
                    clients[i].nStream.Write(data, 0, data.Length);
                }
            }
        }

        //Disconnect all clients
        protected internal void Disconnect()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                //Disconnect the client
                clients[i].Close();
            }
            //Finish the process
            Environment.Exit(0);
        }
    }
}