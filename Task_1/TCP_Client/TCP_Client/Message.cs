using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TCP_Client
{
    public class Message
    {
        public static int TIMEOUT = 15;
        public const byte TEXT_MSG = 0;
        public const byte FILE_MSG = 1;
        static int CODE_SIZE = 2; // (2 - Unicode)

        static char FILE_SEPARATOR = '/';
        static int DATE_TIME_SIZE = 7;
        static int USER_NAME_SIZE = 255; //255 or less
        static int FILE_NAME_SIZE = 255; //255 or less
        public static int MESSAGE_LENGTH_SIZE = 5;
        public static int MAX_LENGTH = (int)Math.Pow(byte.MaxValue + 1, MESSAGE_LENGTH_SIZE) - 1;
        static int HEADER_LENGTH =
            DATE_TIME_SIZE +                //Time info
            USER_NAME_SIZE * CODE_SIZE + 1 +//User info
            FILE_NAME_SIZE * CODE_SIZE + 1 +//File info
            1;                              //Content type

        byte[] length = new byte[MESSAGE_LENGTH_SIZE];
        byte[] dateTime = new byte[DATE_TIME_SIZE];//DMYYhms
        byte userNameLength = 0;
        byte[] userName = new byte[USER_NAME_SIZE * CODE_SIZE];
        byte fileNameLength = 0;
        byte[] fileName = new byte[FILE_NAME_SIZE * CODE_SIZE];
        public byte contentType { get; }
        byte[] content;

        private Message(byte[] dateTime, byte userNameLength, byte[] userName, byte fileNameLength, byte[] fileName, byte contentType, byte[] content)
        {
            if (dateTime.Length != DATE_TIME_SIZE)
            {
                throw new Exception("Wrong date/time format");
            }
            if (userName.Length != USER_NAME_SIZE * CODE_SIZE)
            {
                throw new Exception("Wrong user name format");
            }
            if (contentType != TEXT_MSG && contentType != FILE_MSG)
            {
                throw new Exception("Wrong content type");
            }
            if (content.Length < 1)
            {
                throw new Exception("Empty message");
            }
            if (fileName.Length != FILE_NAME_SIZE * CODE_SIZE)
            {
                throw new Exception("Wrong file name format");
            }
            this.length = GetMessageLength(content.Length);
            this.dateTime = dateTime;
            this.userNameLength = userNameLength;
            this.userName = userName;
            this.fileNameLength = fileNameLength;
            this.fileName = fileName;
            this.contentType = contentType;
            this.content = content;
        }

        //Packing message
        public byte[] GetPacket()
        {
            List<byte> result = new List<byte>();
            result.AddRange(length);
            result.AddRange(dateTime);
            result.Add(userNameLength);
            result.AddRange(userName);
            result.Add(fileNameLength);
            result.AddRange(fileName);
            result.Add(contentType);
            result.AddRange(content);

            return result.ToArray();
        }

        //Unpacking message
        public Message(byte[] packet)
        {
            length = GetMessageLength(packet.Length - HEADER_LENGTH);

            int sourceIndex = 0;
            Array.Copy(packet, sourceIndex, dateTime, 0, DATE_TIME_SIZE);
            sourceIndex += DATE_TIME_SIZE;

            userNameLength = packet[sourceIndex];
            sourceIndex++;

            Array.Copy(packet, sourceIndex, userName, 0, USER_NAME_SIZE);
            sourceIndex += USER_NAME_SIZE * CODE_SIZE;

            fileNameLength = packet[sourceIndex];
            sourceIndex++;

            Array.Copy(packet, sourceIndex, fileName, 0, FILE_NAME_SIZE);
            sourceIndex += FILE_NAME_SIZE * CODE_SIZE;

            contentType = packet[sourceIndex];
            sourceIndex++;

            content = new byte[packet.Length - sourceIndex];
            Array.Copy(packet, sourceIndex, content, 0, packet.Length - sourceIndex);
        }

        //Client sending text message
        public static Message GetUserTextMessage(string text)
        {
            return new Message
                (
                    new byte[DATE_TIME_SIZE],
                    0,
                    new byte[USER_NAME_SIZE * CODE_SIZE],
                    0,
                    new byte[FILE_NAME_SIZE * CODE_SIZE],
                    TEXT_MSG,
                    Encoding.Unicode.GetBytes(text)
                );
        }

        //Server translating text message
        public static Message GetServerTextMessage(string text, string userName)
        {
            Message message = new Message
                (
                    new byte[DATE_TIME_SIZE],
                    (byte)userName.Length,
                    StringToByte(userName, USER_NAME_SIZE * CODE_SIZE),
                    0,
                    new byte[FILE_NAME_SIZE * CODE_SIZE],
                    TEXT_MSG,
                    Encoding.Unicode.GetBytes(text)
                );
            message.SetDateTime();
            return message;
        }

        //Server translating file message
        public static Message GetServerFileMessage(string fileName, string userName, byte[] file)
        {
            Message message = new Message
                (
                    new byte[DATE_TIME_SIZE],
                    (byte)userName.Length,
                    StringToByte(userName, USER_NAME_SIZE * CODE_SIZE),
                    (byte)fileName.Length,
                    StringToByte(fileName, USER_NAME_SIZE * CODE_SIZE),
                    FILE_MSG,
                    file
                );
            message.SetDateTime();
            return message;
        }

        //Client sending file message
        public static Message GetUserFileMessage(string filePath)
        {
            string[] t = filePath.Split(FILE_SEPARATOR);
            string fileName = t.Last();

            if (fileName.Length > FILE_NAME_SIZE)
            {
                throw new Exception("File name is too long to handle");
            }

            string appPath = Directory.GetCurrentDirectory();
            string fullPath = Path.Combine(appPath, filePath);

            if (!File.Exists(fullPath))
            {
                throw new Exception("Wrong file name");
            }
            else
            {
                return new Message
                (
                    new byte[DATE_TIME_SIZE],
                    0,
                    new byte[USER_NAME_SIZE * CODE_SIZE],
                    (byte)fileName.Length,
                    StringToByte(fileName, FILE_NAME_SIZE * CODE_SIZE),
                    FILE_MSG,
                    File.ReadAllBytes(fullPath)
                );
            }
        }

        public static Message GetUserGreeting(string userName, string greeting)
        {
            if (userName.Length > USER_NAME_SIZE || userName.Length == 0)
            {
                throw new Exception("User name incorrect");
            }

            if (greeting.Length == 0)
            {
                greeting = "*prefers to keep an air of mystery around him/her*";
            }

            return new Message
                (
                    new byte[DATE_TIME_SIZE],
                    (byte)userName.Length,
                    StringToByte(userName, USER_NAME_SIZE * CODE_SIZE),
                    0,
                    new byte[FILE_NAME_SIZE * CODE_SIZE],
                    TEXT_MSG,
                    Encoding.Unicode.GetBytes(greeting)
                );
        }

        //Converts sender name to string format
        public string GetSenderName()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Encoding.Unicode.GetString(userName, 0, userNameLength * CODE_SIZE));

            return builder.ToString();
        }

        //Converts file name to string format
        public string GetFileName()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Encoding.Unicode.GetString(fileName, 0, fileNameLength * CODE_SIZE));

            return builder.ToString();
        }

        //Converts message to string format
        public string GetText()
        {
            if (contentType != TEXT_MSG)
            {
                throw new Exception("Can't convert to string");
            }
            StringBuilder builder = new StringBuilder();
            builder.Append(Encoding.Unicode.GetString(content, 0, content.Length));

            return builder.ToString();
        }

        //Converts file to byte array
        public byte[] GetFile()
        {
            if (contentType != FILE_MSG)
            {
                throw new Exception("Can't convert to file");
            }

            return content;
        }

        //Converts message date/time data to local date/time
        public DateTime GetDateTime()
        {
            byte day = dateTime[0];
            byte month = dateTime[1];
            int year = dateTime[2] * (byte.MaxValue + 1) + dateTime[3];
            byte hour = dateTime[4];
            byte minute = dateTime[5];
            byte second = dateTime[6];

            return new DateTime(year, month, day, hour, minute, second).ToLocalTime();
        }

        //Encodes universal date/time to message
        public void SetDateTime()
        {
            DateTime currDT = DateTime.Now.ToUniversalTime();

            dateTime[0] = (byte)currDT.Day;
            dateTime[1] = (byte)currDT.Month;

            dateTime[2] = (byte)(currDT.Year / (byte.MaxValue + 1));
            dateTime[3] = (byte)(currDT.Year % (byte.MaxValue + 1));

            dateTime[4] = (byte)currDT.Hour;
            dateTime[5] = (byte)currDT.Minute;
            dateTime[6] = (byte)currDT.Second;
        }

        //Packs string to fixed length byte array
        private static byte[] StringToByte(string s, int length)
        {
            if (s.Length * CODE_SIZE > length)
            {
                throw new Exception("String argument was too long");
            }

            byte[] result = new byte[length];
            byte[] source = Encoding.Unicode.GetBytes(s);
            Array.Copy(source, result, s.Length * CODE_SIZE);

            return result;
        }

        //Returns a string to represent date/time in message header
        public string FormatDate()
        {
            DateTime dt = this.GetDateTime();
            return $"{dt.Hour.ToString("00")}:{dt.Minute.ToString("00")}:{dt.Second.ToString("00")}";
        }

        //Summarizes message length (except length field itself)
        private static byte[] GetMessageLength(int contentLength)
        {
            int length = HEADER_LENGTH + contentLength;
            int _base = byte.MaxValue + 1;
            byte[] result = new byte[MESSAGE_LENGTH_SIZE];

            if (length > MAX_LENGTH)
            {
                throw new Exception("Message was too large");
            }

            for (int i = 0; i < MESSAGE_LENGTH_SIZE; i++)
            {
                int p = MESSAGE_LENGTH_SIZE - i - 1;
                int quotient = length / (int)Math.Pow(_base, p);
                result[i] = (byte)quotient;
                length = length % (int)Math.Pow(_base, p);
            }

            return result;
        }

        //Summarizes message length (except length field itself)
        public static int GetMessageLength(byte[] length)
        {
            int _base = byte.MaxValue + 1;
            int m = 1;
            int result = 0;
            for (int i = 1; i <= MESSAGE_LENGTH_SIZE; i++)
            {
                result += m * length[MESSAGE_LENGTH_SIZE - i];
                m *= _base;
            }
            return result;
        }
    }
}