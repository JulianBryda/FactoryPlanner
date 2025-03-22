using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Models
{
    public class DockingStation
    {
        public required float ProductionRate { get; set; }
        public string ProductionRateText
        {
            get
            {
                return $"{ProductionRate}/min";
            }
        }
    }
}
