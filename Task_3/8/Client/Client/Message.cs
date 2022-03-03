using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public enum MessageType: byte
    {
        REGISTRATION = 1,
        BETS_ANNOUNCEMENT = 2,
        MAKING_BET = 3,
        DRAWING = 4,
        INFORMING_CLIENT = 5,
        DISCONNECT = 6,
        ACK = 7
    }

    class Message
    {
        public static byte SIZE_FIELD_LENGTH = 3;
        public static int TIMEOUT = 15;
        private static int MAX_LENGTH = (int)(Math.Pow(byte.MaxValue + 1, SIZE_FIELD_LENGTH));
        public byte[] data;
        private User user;
        private Bet bet;
        public MessageType type;

        public Message(byte[] data)
        {
            this.data = data;
            UnpackMessage();
        }

        public byte[] GetMessage(MessageType messageType, UserType userType)
        {
            throw new NotImplementedException();
        }

        //REGISTRIATION 
        public Message(UserType userType, string userName, string userPassword)
        {
            user = new User(userType, userName, userPassword);
            FormMessage(MessageType.REGISTRATION);
        }

        public Message(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.DISCONNECT:
                    FormMessage(messageType);
                    break;
                case MessageType.DRAWING:
                    FormMessage(messageType);
                    break;
                default:
                    throw new ArgumentException("Unsupported message type for this constructor");
            }
        }

        public Message(Bet bet)
        {
            this.bet = bet;
            FormMessage(MessageType.MAKING_BET);
        }

        public Message(BetType betType, int number, int sum)
        {
            bet = new Bet(betType, number, sum);
            FormMessage(MessageType.MAKING_BET);
        }

        private void FormMessage(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.REGISTRATION:
                    FormRegistrationMessage();
                    break;
                case MessageType.DISCONNECT:
                    FormDisconnectMessage();
                    break;
                case MessageType.MAKING_BET:
                    FormBetMessage();
                    break;
                case MessageType.DRAWING:
                    FormDrawingMessage();
                    break;
                default:
                    throw new ArgumentException("Invalid message type");
            }
        }

        private void UnpackMessage()
        {
            MessageType messageType = (MessageType)data[0];

            if (messageType != MessageType.INFORMING_CLIENT && messageType != MessageType.BETS_ANNOUNCEMENT && messageType != MessageType.ACK)
            {
                throw new ArgumentException("Invalid message type");
            }
            this.type = messageType;
        }

        public byte GetWinNumber()
        {
            if (type == MessageType.INFORMING_CLIENT)
            {
                return data[1];
            }
            throw new Exception("Incorrect message type for this operation");
        }

        public List<User> GetAllBets()
        {
            const int BET_BYTES = 2;
            if (type != MessageType.BETS_ANNOUNCEMENT)
            {
                throw new Exception("Incorrect message type for this operation");
            }
            List<User> result = new List<User>();
            int offset = 2;
            for (byte i = data[1]; i>0; i--)
            {
                byte[] temp = new byte[BET_BYTES];
                Array.Copy(data, offset, temp, 0, BET_BYTES);
                offset += BET_BYTES;
                Bet bet = new Bet(temp);
                int nameLength = data[offset++] * User.ENCODING_LENGTH;
                temp = new byte[nameLength];
                Array.Copy(data, offset, temp, 0, nameLength);
                offset += nameLength;

                User user = new User(UserType.PLAYER, temp, bet);
                result.Add(user);
            }
            return result;
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

        private void FormDisconnectMessage()
        {
            FormHeader(1, MessageType.DISCONNECT);
        }

        private void FormDrawingMessage()
        {
            FormHeader(1, MessageType.DRAWING);
        }

        private void FormBetMessage()
        {
            int i = FormHeader(3 + 2, MessageType.MAKING_BET);
            data[i++] = (byte)bet.type;
            data[i++] = bet.number;
            Array.Copy(bet.sum, 0, data, i, bet.sum.Length);
        }

        private void FormRegistrationMessage()
        {
            int messageLength = 3 + user.name.Length + 1 + user.password.Length;
            int i = FormHeader(messageLength, MessageType.REGISTRATION);
       
            data[i++] = (byte)user.type;
            data[i++] = user.nameLength;
            Array.Copy(user.name, 0, data, i, user.name.Length);
            i += user.name.Length;
            data[i++] = user.passwordLength;
            Array.Copy(user.password, 0, data, i, user.password.Length);
        }

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
    }
}
