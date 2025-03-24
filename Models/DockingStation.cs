using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Models
{
    public class DockingStation
    {
        public float IncomingRate
        {
            get
            {
                return IncomingItems.Sum(i => i.Rate);
            }
        }
        public float OutgoingRate
        {
            get
            {
                return OutgoingItems.Sum(i => i.Rate);
            }
        }
        public float NeededRate
        {
            get
            {
                return NeededItems.Sum(i => i.Rate);
            }
        }
        public required List<Item> IncomingItems { get; set; }
        public required List<Item> OutgoingItems { get; set; }
        public required List<Item> NeededItems { get; set; }

        // colors
        public SolidColorBrush IncomingTextBrush
        {
            get
            {
                return (IncomingRate >= NeededRate) ? SufficientBrush : InsufficientBrush;
            }
        }
        public SolidColorBrush OutgoingTextBrush
        {
            get
            {
                return (OutgoingRate >= NeededRate) ? SufficientBrush : InsufficientBrush;
            }
        }

        private readonly SolidColorBrush SufficientBrush = new(Color.FromUInt32(0xFF4A90E2));
        private readonly SolidColorBrush InsufficientBrush = new(Color.FromUInt32(0xFFE67E22));

        public class Item
        {
            public required Bitmap Icon { get; set; }
            public required string ItemPathName { get; set; }
            public required float Rate { get; set; }
        }
    }
}
