using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FactoryPlanner.Models
{
    public class TrainStation
    {
        public required List<DockingStation> DockingStations {  get; set; }
    }
}