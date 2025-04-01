using System.Collections.Generic;

namespace FactoryPlanner.Models
{
    public class TrainStation
    {
        public required string Name { get; set; }
        public required int TrainStationCount { get; set; }
        public required bool IncomingItemsSet { get; set; }
        public required List<DockingStation> DockingStations { get; set; }
    }
}