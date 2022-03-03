using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static string userName;
        static string userPassword;
        static UserType userType;
        static TcpClient client;
        static NetworkStream stream;
        static Bet clientBet;
        static bool registered = false;

        static void Main(string[] args)
        {
            Console.SetWindowSize(60, 15);


            var socket = GetSocket();
            userType = GetUserType();
            userName = GetUserName();

            client = new TcpClient();
            try
            {
                //Connecting the client
                client.Connect(socket.Item1, socket.Item2);
                stream = client.GetStream();

                //Thread for recieving data
                Thread receiveThread = new Thread(new ThreadStart(RecieveContent));
                receiveThread.Start();

                do
                {
                    userPassword = GetUserPassword();
                    Message message = new Message(userType, userName, userPassword);
                    byte[] data = message.data;
                    stream.Write(data, 0, data.Length);
                    Thread.Sleep(100);
                }
                while (!registered);

                Console.WriteLine("Welcome, {0}", userName);

                SendContent();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Disconnect();
            }

            Console.WriteLine($"\nFinished.\nType [Yes] to exit");
            while (Console.ReadLine() != "Yes")
            {
                Thread.Sleep(100);
            };
        }

        private static void SendContent()
        {
            try
            {
                while (true)
                {
                    Message message;
                    switch (userType)
                    {
                        case UserType.CROUPIER:
                            message = GetCroupierContent();
                            break;
                        case UserType.PLAYER:
                            message = GetPlayerContent();
                            break;
                        default:
                            throw new ArgumentException("Illegal user");
                    }
                    byte[] buffer = message.data;

                    stream.Write(buffer, 0, buffer.Length);
                    if (message.type == MessageType.DISCONNECT)
                    {
                        Disconnect();
                        break;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Some errors occured during session. Restarting...");
                SendContent();
            }
        }

        private static Message GetPlayerContent()
        {
            try
            {
                Console.Write("Type [B/D] to make a bet [B] or disconnect [D]: ");
                string userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "B":
                        Bet bet = GetPlayerBet();
                        return new Message(bet);
                    case "D":
                        return new Message(MessageType.DISCONNECT);
                    default:
                        throw new ArgumentException("Illegal action");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return GetPlayerContent();
            }
        }

        private static Bet GetPlayerBet()
        {
            try
            {
                Console.Write("Make your bet:\nType (Even[E]/Odd[O]/Number[N]: ");
                string userInput = Console.ReadLine();
                BetType betType;
                switch (userInput)
                {
                    case "N":
                        betType = BetType.NUMBER;
                        break;
                    case "E":
                        betType = BetType.EVEN;
                        break;
                    case "O":
                        betType = BetType.ODD;
                        break;
                    default:
                        throw new ArgumentException("Illegal bet type");
                }

                Console.Write("Insert bet sum: ");
                userInput = Console.ReadLine();
                int sum;
                if (!int.TryParse(userInput, out sum))
                {
                    throw new ArgumentException("Incorrect sum");
                }

                if (betType == BetType.NUMBER)
                {
                    Console.Write("Insert stake number: ");
                    userInput = Console.ReadLine();
                    int number;
                    if (!int.TryParse(userInput, out number))
                    {
                        throw new ArgumentException("Incorrect number");
                    }
                    return new Bet(betType, number, sum);
                }

                return new Bet(betType, sum);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return GetPlayerBet();
            }
        }

        private static Message GetCroupierContent()
        {
            Console.WriteLine("Type [Yes] to start drawing");
            while (Console.ReadLine() != "Yes") { };
            return new Message(MessageType.DRAWING);
        }

        private static void RecieveContent()
        {
            try
            {
                while (true)
                {
                    Message message = GetMessage();

                    switch (message.type)
                    {
                        case MessageType.BETS_ANNOUNCEMENT:
                            RecieveAnnouncement(message);
                            break;
                        case MessageType.INFORMING_CLIENT:
                            RecieveInfo(message);
                            break;
                        case MessageType.ACK:
                            registered = true;
                            break;
                        default:
                            Console.WriteLine("[Debug] Incorrect type");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection lost!");
                Disconnect();
                Console.WriteLine(e.Message);
            }
        }

        private static void RecieveAnnouncement(Message message)
        {
            Console.WriteLine("Bets:");
            List<User> bets = message.GetAllBets();
            foreach (User user in bets)
            {
                Console.WriteLine($"[{user.GetName()}] {user.GetBet()}");
            }
        }

        private static void RecieveInfo(Message message)
        {
            int winNumber = message.GetWinNumber();
            switch (userType)
            {
                case UserType.CROUPIER:
                    Console.WriteLine($"Number {winNumber} wins!");
                    break;
                case UserType.PLAYER:
                    RecieveInfoPlayer(winNumber);
                    break;
                default:
                    throw new ArgumentException("Illegal user type");
            }
        }

        private static void RecieveInfoPlayer(int n)
        {
            Console.WriteLine($"Number {n} wins\nYour bet: {clientBet.GetInfo()}");
            if (clientBet.CheckWin(n))
            {
                Console.WriteLine("You win!");
            }
            else
            {
                Console.WriteLine("You lose (*sad music*)");
            }
        }

        private static Tuple<string, int> GetSocket()
        {
            const int PORT = 451;
            const string HOST = "26.118.51.73";

            Console.Write($"Insert server ID (defaults to {HOST}): ");
            string host = Console.ReadLine();
            if (host == "")
            {
                host = HOST;
            }

            Console.Write($"Insert server port (defaults to {PORT})");
            int port;
            if (!int.TryParse(Console.ReadLine(), out port))
            {
                port = PORT;
            }

            return new Tuple<string, int>(host, port);
        }

        private static UserType GetUserType()
        {
            Console.Write("Choose whether you want to be a croupier or player ([C]/[P]): ");
            
            while (true)
            {
                string role = Console.ReadLine();
                switch (role)
                {
                    case "C":
                        return UserType.CROUPIER;
                    case "P":
                        return UserType.PLAYER;
                }
                Console.Write("Incorrect input.Try again");
            }
        }

        private static string GetUserName()
        {
            Console.Write("Insert your name: ");

            while (true)
            {
                string name = Console.ReadLine();
                if (name.Length > 0 && name.Length <= User.MAX_NAME_LENGTH)
                {
                    return name;
                }
                Console.Write("Incorrect input.Try again");
            }
        }

        private static string GetUserPassword()
        {
            Console.Write("Password: ");

            while (true)
            {
                string password = Console.ReadLine();
                if (password.Length > 0 && password.Length <= User.MAX_PASSWORD_LENGTH)
                {
                    return password;
                }
                Console.Write("Incorrect input.Try again");
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
                while (stopWatch.Elapsed.TotalMinutes < Message.TIMEOUT && buffer.Count < Message.SIZE_FIELD_LENGTH)
                {
                    //Wait for next byte
                    while (!stream.DataAvailable) { };
                    int b = stream.ReadByte();
                    buffer.Enqueue((byte)b);
                }

                //Recieve expected length
                byte[] lengthBytes = new byte[Message.SIZE_FIELD_LENGTH];
                for (int i = 0; i < Message.SIZE_FIELD_LENGTH; i++)
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

        private static void Disconnect()
        {
            if (stream != null)
                //Disconnect thread
                stream.Close();
            if (client != null)
                //Disconnect client
                client.Close();
        }
    }
}
