using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace FactoryPlanner.Models
{
    public class Production
    {
        public required Bitmap BuildingIcon { get; set; }
        public required List<ProductionItem> ProductionItems { get; set; }
        public required float Efficiency { get; set; }
    }

    public class ProductionItem
    {
        public required string ItemName { get; set; }
        public required Bitmap ItemIcon { get; set; }
        public required float TimePerItem { get; set; }
        public required float ItemsPerMinute { get; set; }
    }
}
