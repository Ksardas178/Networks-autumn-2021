using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    enum UserType : byte
    {
        PLAYER = 1,
        CROUPIER = 2
    }

    class User
    {
        public const byte ENCODING_LENGTH = 2;

        public const byte MAX_NAME_LENGTH = byte.MaxValue;
        public const byte MAX_PASSWORD_LENGTH = byte.MaxValue;

        public byte[] name, password;
        public byte nameLength, passwordLength;
        public UserType type;
        public Bet bet;

        public User(UserType type, string name, string password)
        {
            NewUser(type, name, password);
        }

        public User(UserType type, byte[] name, Bet bet)
        {
            this.type = type;
            this.bet = bet;
            this.name = name;
        }

        private void NewUser(UserType type, string name, string password)
        {
            NewUser(type, Encoding.Unicode.GetBytes(name), Encoding.Unicode.GetBytes(password));
        }

        private void NewUser(UserType type, byte[] name, byte[] password)
        {
            this.type = type;
            this.password = password;
            this.passwordLength = (byte)(password.Length / ENCODING_LENGTH);
            this.name = name;
            this.nameLength = (byte)(name.Length / ENCODING_LENGTH);
        }

        public string GetName()
        {
            return Encoding.Unicode.GetString(name);
        }

        public int GetBet()
        {
            return bet.GetSum();
        }

    }
}
