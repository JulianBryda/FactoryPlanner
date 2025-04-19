using Avalonia.Media.Imaging;
using FactoryPlanner.Assets;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Helper;
using FactoryPlanner.Models;
using log4net;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FactoryPlanner.ViewModels
{
    public class ProductionsViewModel : ViewModelBase
    {

        public ObservableCollection<List<Production>> Productions { get; set; } = [];


        private readonly ILog _log;
        private readonly SaveFileReader _saveFileReader;

        public ProductionsViewModel(IScreen screen) : base(screen)
        {
            _log = LogManager.GetLogger(typeof(ProductionsViewModel));
            _saveFileReader = SaveFileReader.LoadedSaveFile;

            LoadProductions();
        }

        private void LoadProductions()
        {
            List<ActCompObject> buildings = [.._saveFileReader.GetActCompObjects(TypePaths.ConstructorMk1),
                .._saveFileReader.GetActCompObjects(TypePaths.AssemblerMk1),
                .._saveFileReader.GetActCompObjects(TypePaths.Blender),
                .._saveFileReader.GetActCompObjects(TypePaths.OilRefinery),
                .._saveFileReader.GetActCompObjects(TypePaths.ManufacturerMk1),
                .._saveFileReader.GetActCompObjects(TypePaths.SmelterMk1),
                .._saveFileReader.GetActCompObjects(TypePaths.FoundryMk1),
                .._saveFileReader.GetActCompObjects(TypePaths.Packager),
                .._saveFileReader.GetActCompObjects(TypePaths.HadronCollider)];
            buildings = buildings[..5];

            foreach (ActorObject obj in buildings)
            {
                var lastBuildings = GetLastBuildings(obj);
                foreach (var lastBuilding in lastBuildings)
                {
                    var tree = GetProductionTree(lastBuilding);
                    int tadest = 0;
                }

                List<Production> prod = [];
                //foreach (var item in tree)
                //{
                //    prod.Add(GetProduction(item, AssetManager.GetIcon("".GetHashCode())));
                //}
                Productions.Add(prod);

                //GetProduction(obj, AssetManager.GetIcon(TypePaths.ConstructorMk1));
            }

            string test = "";
        }

        private List<ActorObject> GetProductionTree(ActorObject origin) => GetProductionTree([origin]);
        private List<ActorObject> GetProductionTree(List<ActorObject> origins)
        {
            List<ActorObject> objects = [];
            foreach (var origin in origins)
            {
                objects.AddRange(BuildingHelper.GetConnectedBuildings(origin, "Input"));
            }

            if (objects.Count == 0) return objects;

            objects.AddRange(GetProductionTree(objects));

            // remove double entries
            objects = [.. objects.Distinct()];

            return objects;
        }

        private List<ActorObject> GetLastBuildings(ActorObject origin) => GetLastBuildings([origin]);
        private List<ActorObject> GetLastBuildings(List<ActorObject> origins)
        {
            List<ActorObject> lastBuildings = [];

            foreach (var origin in origins)
            {
                List<ActorObject> connected = BuildingHelper.GetConnectedBuildings(origin, "Output");
                if (connected.Count == 0)
                {
                    lastBuildings.Add(origin);
                }
                else
                {
                    lastBuildings.AddRange(GetLastBuildings(connected));
                }
            }

            // remove double entries
            lastBuildings = [.. lastBuildings.Distinct()];

            return lastBuildings;
        }

        private Production GetProduction(ActCompObject obj, Bitmap buildingIcon)
        {
            List<ProductionItem> productionItems = [];

            Recipe? recipe = BuildingHelper.GetRecipe(obj);
            if (recipe != null)
            {
                FloatProperty? currentPotentialProperty = (FloatProperty?)SaveFileReader.GetPropertyByName(obj, "mCurrentPotential")?.Property;
                float currentPotential = currentPotentialProperty?.Value ?? 1f;

                foreach (var product in recipe.Products)
                {
                    string itemName = product.Item[5..^2];

                    productionItems.Add(new()
                    {
                        ItemName = itemName,
                        ItemIcon = AssetManager.GetItemIcon(itemName),
                        ItemsPerMinute = 60f / recipe.Time * product.Amount * currentPotential,
                        TimePerItem = recipe.Time / product.Amount
                    });
                }
            }


            return new Production()
            {
                BuildingIcon = buildingIcon,
                ProductionItems = productionItems,
                Efficiency = 1f
            };
        }
    }
}
