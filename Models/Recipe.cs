

namespace FactoryPlanner.Models
{
    public class Recipe
    {
        public required string Slug { get; set; }
        public required string Name { get; set; }
        public required string ClassName { get; set; }
        public required bool Alternate { get; set; }
        public required int Time { get; set; }
        public required float ManualTimeMultiplier { get; set; }
        public required Product[] Ingredients { get; set; }
        public required bool ForBuilding { get; set; }
        public required bool InMachine { get; set; }
        public required bool InHand { get; set; }
        public required bool InWorkshop { get; set; }
        public required Product[] Products { get; set; }
        public required string[] ProducedIn { get; set; }
        public required bool IsVariablePower { get; set; }
    }

    public class Product
    {
        public required string Item { get; set; }
        public required int Amount { get; set; }
    }


}
