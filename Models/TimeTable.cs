using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Models
{
    public class TimeTable
    {
        public required int CurrentStopIndex { get; set; }
        public required List<TimeTableStop> Stops { get; set; }
    }

    public class TimeTableStop
    {
        public required string StationIdentifierPathName { get; set; }
        public required string LoadType { get; set; }
        public required List<string> LoadFilter { get; set; }
        public required List<string> UnloadFilter { get; set; }

    }
}
