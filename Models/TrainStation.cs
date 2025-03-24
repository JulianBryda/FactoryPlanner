using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FactoryPlanner.Models
{
    public class TrainStation
    {
        public required string Name { get; set; }
        public required List<DockingStation> DockingStations {  get; set; }
    }
}