using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public enum MessageType : byte
    {
        REGISTRATION = 1,
        ADDING_CURRENCY = 2,
        DELETING_CURRENCY = 3,
        CURRENCY_INFO = 4,
        MODIFYING_CURRENCY = 5,
        DELETE_RATE = 6,
        DISCONNECT_USER = 7,
        INCORRECT_SHUTDOWN
    }

    class Message
    {
        public static byte SIZE_FIELD_LENGTH = 4;
        public static int TIMEOUT = 15;

        private static int DATE_TIME_SIZE = 7;
        private static int MAX_LENGTH = int.MaxValue - 1;
        //----------------------------------------------------------

        public byte[] data;

        public Message(byte[] data)
        {
            this.data = data;
        }

        public Message(MessageType messageType)
        {
            if (messageType != MessageType.INCORRECT_SHUTDOWN)
            {
                throw new ArgumentException("Unexpected message type for this constructor");
            }
            FormHeader(1, messageType);
        }

        public string GetCurrencyName()
        {
            MessageType messageType = GetMessageType();
            if 
            (
                messageType != MessageType.DEL
            )
        }

        public MessageType GetMessageType()
        {
            return (MessageType)data[0];
        }

        private int FormHeader(int messageLength, MessageType messageType)
        {
            byte[] messageLengthBytes = GetMessageLength(messageLength);
            data = new byte[SIZE_FIELD_LENGTH + messageLength];
            int i = 0;
            Array.Copy(messageLengthBytes, 0, data, i, SIZE_FIELD_LENGTH);
            i += SIZE_FIELD_LENGTH;
            data[i++] = (byte)messageType;

            return i;
        }

        //Converts length to byte array to it write down in header
        private static byte[] GetMessageLength(int length)
        {
            int _base = byte.MaxValue + 1;
            byte[] result = new byte[SIZE_FIELD_LENGTH];

            if (length >= MAX_LENGTH)
            {
                throw new Exception("Message was too large");
            }

            for (int i = 0; i < SIZE_FIELD_LENGTH; i++)
            {
                int p = SIZE_FIELD_LENGTH - i - 1;
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
            for (int i = 1; i <= SIZE_FIELD_LENGTH; i++)
            {
                result += m * length[SIZE_FIELD_LENGTH - i];
                m *= _base;
            }
            return result;
        }

        //Converts message date/time data to local date/time
        public DateTime GetDateTime(byte[] data)
        {
            byte day = data[0];
            byte month = data[1];
            int year = data[2] * (byte.MaxValue + 1) + data[3];
            byte hour = data[4];
            byte minute = data[5];
            byte second = data[6];

            return new DateTime(year, month, day, hour, minute, second).ToLocalTime();
        }

        //Encodes universal date/time to message
        private byte[] GetDateTime(DateTime dateTime)
        {
            byte[] result = new byte[DATE_TIME_SIZE];
            DateTime currDT = DateTime.Now.ToUniversalTime();

            result[0] = (byte)currDT.Day;
            result[1] = (byte)currDT.Month;

            result[2] = (byte)(currDT.Year / (byte.MaxValue + 1));
            result[3] = (byte)(currDT.Year % (byte.MaxValue + 1));

            result[4] = (byte)currDT.Hour;
            result[5] = (byte)currDT.Minute;
            result[6] = (byte)currDT.Second;

            return result;
        }

        /*
        //Returns a string to represent date/time in message header
        public string FormatDate()
        {
            byte[] dtBytes;
            dtBytes
            DateTime dt = GetDateTime(dtBytes);
            return $"{dt.Hour.ToString("00")}:{dt.Minute.ToString("00")}:{dt.Second.ToString("00")}";
        }*/
    }
}
