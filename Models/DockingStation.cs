using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace FactoryPlanner.Models
{
    public class DockingStation
    {
        public int Id { get; set; }
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
        // don't specify SolidColorBrush in a variable or else you will need to run the constructor in the UI Thread
        public SolidColorBrush IncomingTextBrush
        {
            get
            {
                return (IncomingRate >= NeededRate) ? new(Color.FromUInt32(0xFF4A90E2)) : new(Color.FromUInt32(0xFFE67E22));
            }
        }
        public SolidColorBrush OutgoingTextBrush
        {
            get
            {
                return (OutgoingRate >= NeededRate) ? new(Color.FromUInt32(0xFF4A90E2)) : new(Color.FromUInt32(0xFFE67E22));
            }
        }



        public class Item
        {
            public Bitmap? Icon { get; set; }
            public required string ItemPathName { get; set; }
            public required float Rate { get; set; }
        }
    }
}
