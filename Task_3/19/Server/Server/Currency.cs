using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{

    class Currency
    {
        public string name;
        public List<Rate> rates;

        public Currency(string name)
        {
            this.name = name;
            this.rates = new List<Rate>();
        }

        public void AddRate(string name, DateTime dateTime, int value)
        {
            Rate rate = GetRate(name);
            if (rate == null)
            {
                rates.Add(new Rate(name, dateTime, value));
            }
            else
            {
                rate.AddHistory(dateTime, value);
            }
        }

        private Rate GetRate(string name)
        {
            return rates.FirstOrDefault(r => r.name == name);
        }
    }
}
