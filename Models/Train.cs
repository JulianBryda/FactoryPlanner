using FactoryPlanner.FileReader.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Models
{
    public class Train
    {
        public required string Name { get; set; }
        public required string Layout { get; set; }
        public required bool IsReversed { get; set; }
        public required List<ActorObject> Locomotives { get; set; }
        public required List<FreightWagon> FreightWagons { get; set; }
        public TimeTable? TimeTable { get; set; }
    }
}
