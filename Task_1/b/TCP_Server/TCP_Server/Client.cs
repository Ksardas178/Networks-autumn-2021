using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Server
{ 
    class Client
    {

        protected internal string id { get; private set; }
        protected internal NetworkStream nStream { get; private set; }
        string userName;
        TcpClient client;
        Server server;
        Task processTask;

        public Client(TcpClient newClient, Server newServer)
        {
            id = Guid.NewGuid().ToString();
            client = newClient;
            server = newServer;
            newServer.AddConnection(this);
        }

        public void ProcessTask()
        {
            processTask = Task.Run(() => Process());
        }

        public void Process()
        {
            try
            {
                nStream = client.GetStream();
                //Get username
                Message userMessage = GetMessage();
                userName = userMessage.GetSenderName();
                string greeting = userMessage.GetText();

                Message serverMessage = Message.GetServerTextMessage($"joined the chat: {greeting}", userName);
                server.BroadcastMessage(serverMessage, this.id);
                Console.WriteLine($"[{serverMessage.FormatDate()}|{userName}] {serverMessage.GetText()}");

                //Recieving messages
                while (true)
                {
                    try
                    {
                        Message message = GetMessage();
                        
                        switch (message.contentType)
                        {
                            case Message.TEXT_MSG:
                                serverMessage = Message.GetServerTextMessage(message.GetText(), userName);
                                Console.WriteLine($"[{serverMessage.FormatDate()}|{userName}] {serverMessage.GetText()}");
                                server.BroadcastMessage(serverMessage, this.id);
                                break;
                            case Message.FILE_MSG:
                                serverMessage = Message.GetServerFileMessage
                                (
                                    message.GetFileName(),
                                    userName,
                                    message.GetFile()
                                );
                                Console.WriteLine($"[{ serverMessage.FormatDate()}|{userName}] shared a file ({serverMessage.GetFileName()})");
                                server.BroadcastMessage(serverMessage, this.id);
                                break;
                        }                        
                    }
                    catch
                    {
                        serverMessage = Message.GetServerTextMessage("left the chat", userName);
                        Console.WriteLine($"[{serverMessage.FormatDate()}|{userName}] {serverMessage.GetText()}");
                        server.BroadcastMessage(serverMessage, this.id);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                //Free the resources
                server.RemoveConnection(this.id);
                Close();
            }
        }

        //Closing the connection
        protected internal void Close()
        {
            if (nStream != null)
                nStream.Close();
            if (client != null)
                client.Close();
        }

        //Input message recieving
        private Message GetMessage()
        {
            //Input data buffer
            ConcurrentQueue<byte> buffer = new ConcurrentQueue<byte>();

            Stopwatch stopWatch = new Stopwatch();

            //Wait for message
            while (!nStream.DataAvailable)
            {
                Thread.Sleep(20);
                nStream.Write(new byte[Message.MESSAGE_LENGTH_SIZE], 0, Message.MESSAGE_LENGTH_SIZE);
            }

            stopWatch.Start();

            //Read message length
            while (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT && buffer.Count < Message.MESSAGE_LENGTH_SIZE)
            {
                //Wait for next byte
                while (!nStream.DataAvailable) { };
                int b = nStream.ReadByte();
                buffer.Enqueue((byte)b);
            }

            //Recieve expected length
            byte[] lengthBytes = new byte[Message.MESSAGE_LENGTH_SIZE];
            for (int i = 0; i < Message.MESSAGE_LENGTH_SIZE; i++)
            {
                buffer.TryDequeue(out lengthBytes[i]);
            }
            int length = Message.GetMessageLength(lengthBytes);

            //Read message length
            while (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT && buffer.Count != length)
            {
                //Wait for next byte
                while (!nStream.DataAvailable) { };
                int b = nStream.ReadByte();
                buffer.Enqueue((byte)b);
            }

            if (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT)
            {
                return new Message(buffer.ToArray());
            }
            throw new Exception("Timeout error");
        }

    }

}