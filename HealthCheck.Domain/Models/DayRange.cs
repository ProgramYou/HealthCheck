using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheck.Domain.Models
{
    public class DayRange
    {
        public int Start { get; set; }
        public int End { get; set; }

        public int Index { get; set; }

        public DayRange() { }

        public DayRange(int index, int start, int end)
        {
            Index = index;
            Start = start;
            End = end;
        }
    }
}
