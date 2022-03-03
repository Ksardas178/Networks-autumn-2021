using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class User
    {
        public const int ENCODING_LENGTH = 4;

        public const byte MAX_NAME_LENGTH = byte.MaxValue;
        public const byte MAX_PASSWORD_LENGTH = byte.MaxValue;

        private string name, password;
        private byte nameLength, passwordLength;

        public User(string name, string password)
        {
            NewUser(name, password, (byte)name.Length, (byte)password.Length);
        }

        private void NewUser(string name, string password, byte nameLength, byte passwordLength)
        {
            this.name = name;
            this.password = password;
            this.nameLength = nameLength;
            this.passwordLength = passwordLength;
        }

    }
}
