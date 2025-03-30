using FactoryPlanner.FileReader.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Models
{
    public class FreightWagon
    {
        public required ActorObject Wagon { get; set; }
        public required List<DockingStation.Item> Items { get; set; }
    }
}
