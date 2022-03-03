using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    struct Point
    {
        int value;
        DateTime dateTime;

        public Point(DateTime dateTime, int value)
        {
            this.dateTime = dateTime;
            this.value = value;
        }
    }
    class Rate
    {
        public string name;
        public List<Point> history;

        public Rate(string name, DateTime dateTime, int value) 
        {
            this.name = name;

            history = new List<Point>();
            AddHistory(dateTime, value);
        }

        public void AddHistory(DateTime dateTime, int value)
        {
            history.Add(new Point(dateTime, value));
        }

    }
}
