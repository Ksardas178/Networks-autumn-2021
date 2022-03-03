using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    enum BetType : byte
    {
        EVEN = 0,
        ODD = 1,
        NUMBER = 2
    }

    class Bet
    {
        public BetType type;
        public byte number;
        public byte[] sum;

        private const int MIN_SUM = 1;
        private const int MAX_SUM = 50000;
        private const int MAX_NUMBER = 37;

        public Bet(int number, int sum)
        {
            NewBet(BetType.NUMBER, number, sum);
        }

        public Bet(BetType type, int sum)
        {
            if (type == BetType.NUMBER)
            {
                throw new ArgumentException("Wrong constructor for this bet type");
            }
            NewBet(type, 0, sum);
        }

        public Bet(int sum)
        {
            SetSum(sum);
        }

        public Bet(BetType type, int number, int sum)
        {
            NewBet(type, number, sum);
        }

        public Bet(byte[] sum)
        {
            this.sum = sum;
        }

        private void NewBet(BetType type, int number, int sum)
        {
            if (number > MAX_NUMBER || number < 0)
            {
                throw new ArgumentOutOfRangeException($"Input must be in [0, {MAX_NUMBER}]");
            }
            this.number = (byte)number;
            this.type = type;
            SetSum(sum);
        }

        public int GetSum()
        {
            const int BASE = byte.MaxValue + 1;
            return sum[0] * BASE + sum[1];
        }

        private void SetSum(int sum)
        {
            if (sum > MAX_SUM || sum < MIN_SUM)
            {
                throw new ArgumentOutOfRangeException($"Bet sum must be in [{MIN_SUM}, {MAX_SUM}]");
            }
            const int BASE = byte.MaxValue + 1;
            this.sum = new byte[] 
            {
                (byte)(sum / BASE),
                (byte)(sum % BASE)
            };
        }

        public bool CheckWin(int winNumber)
        {
            switch (type)
            {
                case BetType.NUMBER:
                    return number == winNumber;
                case BetType.ODD:
                    return number % 2 == 1;
                case BetType.EVEN:
                    return number % 2 == 0;
                default:
                    throw new ArgumentException("Illegal bet type");
            }
        }

        public string GetInfo()
        {
            string type;
            switch (this.type)
            {
                case BetType.NUMBER:
                    type = "number";
                    break;
                case BetType.ODD:
                    type = "odd";
                    break;
                case BetType.EVEN:
                    type = "even";
                    break;
                default:
                    throw new ArgumentException("Illegal bet type");
            }
            return $"[Type: {type} | Number: {this.number} | Sum: {this.GetSum()}]";
        }
    }
}
