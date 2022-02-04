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
    enum TypeCode : byte
    {
        READ_REQ = 1,
        WRITE_REQ = 2,
        DATA = 3,
        ACK = 4,
        ERR = 5
    }

    enum ErrorCode : byte
    {
        UNDEFINED = 0,
        FILE_NOT_FOUND = 1,
        ACCESS_DENIED = 2,
        NO_FREE_SPACE = 3,
        ILLEGAL_OP = 4,
        EXCHANGE_ID_UNKNOWN = 5
    }

    class Client
    {
        static int PACKET_SIZE = 512;
        static int RETRIES = 50;
        protected internal string id { get; private set; }
        private IPEndPoint socket;
        private Server server;
        private List<byte> data = new List<byte>();
        private string fileName;
        private int blockNumber = 0;
        private UdpClient serverCl;
        private bool sent = false;

        public Client(UdpReceiveResult receiveResult, Server server)
        {
            //Add new client
            socket = receiveResult.RemoteEndPoint;
            id = GetId(socket);
            this.server = server;
            server.AddClient(this);
            //Initiate server UDP client for sending replies to client
            serverCl = new UdpClient();
            //And proceed message
            Process(receiveResult);
        }

        public static string GetId(UdpReceiveResult receiveResult)
        {
            IPEndPoint socket = receiveResult.RemoteEndPoint;
            return GetId(socket);
        }

        private static string GetId(IPEndPoint socket)
        {
            return socket.Address.ToString() + '|' + socket.Port.ToString();
        }

        public void Process(UdpReceiveResult receiveResult)
        {
            //Now we know that the previous message was sent succesfully
            sent = true; 
            byte[] buffer = receiveResult.Buffer;
            var subcommand = buffer[1];
            switch (subcommand)
            {
                case (byte)TypeCode.READ_REQ: //Read request
                    break;
                case (byte)TypeCode.WRITE_REQ: //Write request
                    blockNumber = 0;
                    ProceedWrite(buffer);
                    break;
                case (byte)TypeCode.DATA: //Data
                    GetData(buffer);
                    break;
                case (byte)TypeCode.ACK: //Acknowledgement
                    break;
                case (byte)TypeCode.ERR: //Error
                    PrintError(buffer[3]);
                    break;
                default:
                    throw new Exception("Wrong subcommand TFTP code");
            }
        }

        private void ProceedWrite(byte[] buffer)
        {
            const int HEADER_LENGTH = 2;
            int l = buffer.Length - HEADER_LENGTH;
            byte[] dataBytes = new byte[l];
            Array.Copy(buffer, HEADER_LENGTH, dataBytes, 0, l);

            string[] data = Encoding.UTF8.GetString(dataBytes).Split('\0');
            string mode = data[1];
            fileName = data[0];

            switch (mode)
            {
                case "netascii":
                    WriteNetAscii(fileName);
                    break;
                default:
                    throw new NotImplementedException("MODE not supported");
            }
            //SendError(ErrorCode.ILLEGAL_OP, "Test error");
            SendAcknowledgement(false);            
        }

        private void SendAcknowledgement(bool isLast)
        {
            byte[] data = new byte[4];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
            //Fill command type field
            data[1] = (byte)TypeCode.ACK;

            //Fill block number field
            var blockNumber = ConvertNumber(this.blockNumber);
            data[2] = blockNumber.Item1;
            data[3] = blockNumber.Item2;

            //Send packet
            SendPacket(data, isLast);

            this.blockNumber++;
        }

        private void SendError(ErrorCode code, string message)
        {
            const int HEADER_LENGTH = 4;

            byte[] data = new byte[HEADER_LENGTH + message.Length + 1];
            //Last byte - 0
            data[data.Length - 1] = 0;
            //Write subcommand code
            data[0] = 0;
            data[1] = (byte)TypeCode.ERR;
            //Write error code
            data[2] = 0;
            data[3] = (byte)code;
            //Write message
            Array.Copy(Encoding.UTF8.GetBytes(message), 0, data, HEADER_LENGTH, message.Length);

            //Send packet
            SendPacket(data, true);
        }

        private Tuple<byte, byte> ConvertNumber(int n)
        {
            int _base = byte.MaxValue + 1;
            return new Tuple<byte, byte>
            (
                (byte)(n / _base), 
                (byte)(n % _base)
            );
        }

        private async void SendPacket(byte[] data, bool isLast)
        {
            sent = isLast;
            int i = 0;

            do
            {
                server.SendPacket(data, this.socket);
                await Task.Delay(300);
                i++;
            }
            while (!sent && i < RETRIES);

            //Disconnect the client after timeout
            if (!sent)
            {
                Console.WriteLine("Error: client disconnected from the server");
                server.RemoveClient(this.id);
            }
        }

        private void WriteNetAscii(string fileName)
        {
            Console.WriteLine($"Request to write file {fileName}");
        }

        //Read data from buffer
        private void GetData(byte[] buffer)
        {
            const int HEADER_LENGTH = 4;
            
            //Read data
            this.data.AddRange(buffer.Skip(HEADER_LENGTH));

            //Check if data packet is the last one
            if (buffer.Length != PACKET_SIZE + HEADER_LENGTH)
            {
                SendAcknowledgement(true);
                SaveFile();
                Console.WriteLine($"Transfer successful ({fileName})");                
                server.RemoveClient(this.id);
            }
            else
            {
                SendAcknowledgement(false);
            }
        }

        private void SaveFile()
        {
            string appPath = Directory.GetCurrentDirectory();
            string fullPath = Path.Combine(appPath, fileName);

            File.WriteAllBytes(fullPath, this.data.ToArray());
        }

        //Display an error message
        private void PrintError(byte code)
        {
            string e;
            switch (code)
            {
                case (byte)ErrorCode.UNDEFINED:
                    e = "undefined";
                    break;
                case (byte)ErrorCode.FILE_NOT_FOUND:
                    e = "file not found";
                    break;
                case (byte)ErrorCode.ACCESS_DENIED:
                    e = "access denied";
                    break;
                case (byte)ErrorCode.NO_FREE_SPACE:
                    e = "no free space";
                    break;
                case (byte)ErrorCode.ILLEGAL_OP:
                    e = "illegal operation";
                    break;
                case (byte)ErrorCode.EXCHANGE_ID_UNKNOWN:
                    e = "unknown exchange identifier";
                    break;
                default:
                    throw new Exception("Wrong TFTP error code");
            }
            Console.WriteLine("TFTP error: " + e);
        }

    }
}
