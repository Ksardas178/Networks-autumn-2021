using System;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TCP_Client
{
    class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
        private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        static string userName;
        //private const string host = "127.0.0.1";
        private const int port = 8888;
        static TcpClient client;
        static NetworkStream stream;

        static void Main(string[] args)
        {
            // Register the handler
            SetConsoleCtrlHandler(Handler, true);

            Console.SetWindowSize(60, 15);

            Console.Write("Insert server ID (default local machine): ");
            string host = Console.ReadLine();
            if (host == "")
            {
                host = "127.0.0.1";
            }
            Console.Write("Insert your name: ");
            userName = Console.ReadLine();
            Console.Write("Say hello to everyone: ");
            string greeting = Console.ReadLine();

            client = new TcpClient();
            try
            {
                //Connecting the client
                client.Connect(host, port);
                stream = client.GetStream();

                Message message = Message.GetUserGreeting(userName, greeting);

                byte[] data = message.GetPacket();
                stream.Write(data, 0, data.Length);

                //Thread for recieving data
                Thread receiveThread = new Thread(new ThreadStart(RecieveContent));
                receiveThread.Start();
                Console.WriteLine("Welcome, {0}", userName);
                SendContent();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.ReadKey();
                Disconnect();
            }
        }

        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    Console.WriteLine("Closing");
                    // TODO Cleanup resources
                    Disconnect();

                    Environment.Exit(0);
                    return false;

                default:
                    return false;
            }
        }

        static void SendContent()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = GetContent();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                Console.WriteLine("Some errors occured during session. Restarting...");
                SendContent();
            }
        }

        static byte[] GetContent()
        {
            bool recieved = false;
            do
            {
                Console.Write("Choose message type (file [F] | text [T]): ");
                string type = Console.ReadLine();

                switch (type)
                {
                    case "F":
                        recieved = true;
                        return SendFile();
                    case "T":
                        recieved = true;
                        return SendMessage();
                    case "Q":
                        Disconnect();
                        break;
                }
            }
            while (!recieved);
            throw new Exception("GetContent(): Impossible state");
        }

        static byte[] SendMessage()
        {
            Console.WriteLine("Type message: ");
            Message message = Message.GetUserTextMessage(Console.ReadLine());

            return message.GetPacket();         
        }

        static byte[] SendFile()
        {
            Console.WriteLine("Insert file path (relative): ");
            string fileName = Console.ReadLine();
            Message message = Message.GetUserFileMessage(fileName);

            return message.GetPacket();
        }

        //Recieving messages
        static void RecieveContent()
        {
            while (true)
            {
                try
                {
                    Message message = GetMessage();

                    Console.Write($"\n[{message.FormatDate()}|{message.GetSenderName()}] ");

                    switch (message.contentType)
                    {
                        case Message.FILE_MSG:
                            RecieveFile(message);
                            break;
                        case Message.TEXT_MSG:
                            RecieveMessage(message);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connection lost!");
                    Disconnect();
                    Console.WriteLine(e.Message);
                    Console.ReadKey();
                }
            }
        }

        static private Message GetMessage()
        {
            //Input data buffer
            ConcurrentQueue<byte> buffer = new ConcurrentQueue<byte>();

            Stopwatch stopWatch = new Stopwatch();

            //Wait for message
            while (!stream.DataAvailable)
            {
                Thread.Sleep(20);
            }

            stopWatch.Start();

            int length = 0;
            while (length == 0)
            {
                //Read message length
                while (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT && buffer.Count < Message.MESSAGE_LENGTH_SIZE)
                {
                    //Wait for next byte
                    while (!stream.DataAvailable) { };
                    int b = stream.ReadByte();
                    buffer.Enqueue((byte)b);
                }

                //Recieve expected length
                byte[] lengthBytes = new byte[Message.MESSAGE_LENGTH_SIZE];
                for (int i = 0; i < Message.MESSAGE_LENGTH_SIZE; i++)
                {
                    buffer.TryDequeue(out lengthBytes[i]);
                }
                length = Message.GetMessageLength(lengthBytes);
            }

            //Read message length
            while (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT && buffer.Count != length)
            {
                //Wait for next byte
                while (!stream.DataAvailable) { };
                int b = stream.ReadByte();
                buffer.Enqueue((byte)b);
            }

            if (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT)
            {
                return new Message(buffer.ToArray());
            }
            throw new Exception("Timeout error");
        }

        private static void RecieveFile(Message message)
        {
            string appPath = Directory.GetCurrentDirectory();
            string fileName = message.GetFileName();
            string fullPath = Path.Combine(appPath, fileName);

            File.WriteAllBytes(fullPath, message.GetFile());
            Console.WriteLine($"Send a file ({message.GetFileName()})");
        }

        private static void RecieveMessage(Message message)
        {
            Console.WriteLine(message.GetText());
        }

        static void Disconnect()
        {
            if (stream != null)
                //Disconnect thread
                stream.Close();
            if (client != null)
                //Disconnect client
                client.Close();
            //Finish the process
            Environment.Exit(0);
        }
    }
}