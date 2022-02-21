using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SNMP_Client
{

    public enum CommandPDU : byte
    {
        GET_RQ = 0,
        GET_NEXT_RQ = 1,
        SET_RQ = 2,
        GET_RP = 3,
        TRAP = 4,
        GET_BULK_RQ = 5,
        INFORM_RQ = 6,
        TRAP_V3 = 7,
        REPORT = 8
    }

    public enum Error : byte
    {
        NO_ERR = 0,
        TOO_BIG = 1,
        NO_SUCH_ITEM = 2,
        BAD_VALUE = 3,
        READ_ONLY = 4,
        GEN_ERR = 5
    }

    class Program
    {
        static int CLIENT_PORT = 162;
        static int SERVER_PORT = 161;

        static void Main(string[] args)
        {
            UdpClient udpClient = new UdpClient(CLIENT_PORT);
            
            byte[] ipAddress = { 25, 0, 127, 235 };
            IPAddress serverAddress = new IPAddress(ipAddress);
            IPEndPoint serverSocket = new IPEndPoint(serverAddress, SERVER_PORT);

            for (int i = 1; i < 25; i++)
            {
                byte[] data = GetRequest($"3.6.1.2.1.25.6.3.1.2.{i}", "Home");
                udpClient.Send(data, data.Length, serverSocket);
                Console.WriteLine("SNMP Request send");

                data = udpClient.Receive(ref serverSocket);
                //Console.WriteLine();
                ProceedData(data);
            }

            Console.ReadLine();
        }

        private static void ProceedData(byte[] data)
        {
            if (data[0] != 48)
            {
                Console.WriteLine("Input SNMP is not of sequence type");
                return;
            }
            byte message_length = data[1];
            if (message_length != data.Length - 2)
            {
                Console.WriteLine("SNMP message is corrupted");
                return;
            }
            if (data[2] != 2 || data[3] != 1 || data[4] != 0)
            {
                Console.WriteLine("Unsupported SNMP version");
                return;
            }
            if (data[5] != 4)
            {
                Console.WriteLine("Unsupported community string type");
                return;
            }
            byte community_string_length = data[6];
            byte[] community_name_byte = new byte[community_string_length];
            Array.Copy(data, 7, community_name_byte, 0, community_string_length);
            string community_name = GetCommunity(community_name_byte);

            //SNMP PDU type (2)

            //Request ID (3)

            //SNMP error (3)
            if (data[community_string_length + 12] != 02 ||
                data[community_string_length + 13] != 01 ||
                data[community_string_length + 14] != 00)
            {
                Console.WriteLine("SNMP error");
                return;
            }
            //Error index (3)

            //Varbind list (2)

            //Varbind type (2)

            //Object ID
            byte oid_length = data[community_string_length + 23];

            //Value
            byte value_type = data[community_string_length + oid_length + 24];
            if (value_type != 4)
            {
                Console.WriteLine("Unsupported value type");
                return;
            }
            byte value_length = data[community_string_length + oid_length + 25];
            if (value_length == 0)
            {
                Console.WriteLine("Empty value");
                return;
            }
            byte[] value_bytes = new byte[value_length];
            Array.Copy(data, community_string_length + oid_length + 26, value_bytes, 0, value_length);
            string value = GetValue(value_bytes);

            Console.WriteLine($"\nSNMP reply recieved:\nCommunity: {community_name}\nValue: {value}\n");
        }

        private static string GetValue(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        private static string GetCommunity(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        private static byte[] GetCommunity(string communityName)
        {
            return Encoding.UTF8.GetBytes(communityName);
        }

        static byte[] GetRequest(string oid, string communityName)
        {
            byte oid_length = 11;
            byte message_length = (byte)(oid_length + communityName.Length + 24);
            byte pdu_length = (byte)(oid_length + 17);
            byte varbind_length = (byte)(oid_length + 4);
            byte varbind_list_length = (byte)(oid_length + 6);

            byte value_type = 5;
            byte value_length = 0;
            List<byte> result = new List<byte>
            {
                48,
                message_length, //SNMP message length
                2,
                1,
                0,
                4,  //SNMP Community string type
                (byte)communityName.Length,  //SNMP Community string length
            };
            result.AddRange(GetCommunity(communityName));
            result.AddRange(new byte[] {
                160,
                pdu_length,
                2,
                1,
                1,
                2,
                1,
                0,
                2,
                1,
                0,
                48,
                varbind_list_length,
                48,
                varbind_length,
                6,  //OID type
                oid_length
            });
            //Write OID to result
            result.AddRange(GetOID(oid));
            //Add an empty value
            result.Add(value_type);
            result.Add(value_length);

            return result.ToArray();
        }

        private static byte[] GetOID(string oid)
        {
            var result = oid.Split('.').Select(b => Convert.ToByte(b)).ToArray();
            result[0] += 40;
            return result;
        }
    }
}
