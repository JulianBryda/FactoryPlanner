using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Models
{
    public class RessourceNode
    {
        public required string NodePathName { get; set; }
        public required string ItemPathName { get; set; }
        public required RessourceNodePurity Purity { get; set; }

        public int ItemsExtracted(int minerLevel)
        {
            int multiplier = (minerLevel == 3) ? 4 : minerLevel;

            return Purity switch
            {
                RessourceNodePurity.Impure => 30 * multiplier,
                RessourceNodePurity.Normal => 60 * multiplier,
                RessourceNodePurity.Pure => 120 * multiplier,
                _ => throw new NotImplementedException(),
            };
        }
    }

    public enum RessourceNodePurity
    {
        Impure,
        Normal,
        Pure
    }
}
