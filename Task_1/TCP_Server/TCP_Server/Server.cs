using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TCP_Server
{
    public class Server
    {
        static TcpListener tcpListener;
        //Connection list
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

        //Listening input connetions
        protected internal void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                Console.WriteLine("Server started. Waiting for connections...");

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    Client client = new Client(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(client.Process));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Disconnect();
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
            //Stop server
            tcpListener.Stop();

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