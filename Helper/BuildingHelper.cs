using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using DynamicData;
using FactoryPlanner.Assets;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Helper
{
    public class BuildingHelper
    {
        private static readonly SaveFileReader s_saveFileReader = SaveFileReader.LoadedSaveFile;

        public static Recipe? GetRecipe(ActCompObject obj)
        {
            ObjectProperty? recipeProperty = (ObjectProperty?)SaveFileReader.GetPropertyByName(obj, "mCurrentRecipe")?.Property;
            if (recipeProperty == null) return null;

            string recipePathName = recipeProperty.Reference.PathName;
            Recipe? recipe = AssetManager.GetRecipe(recipePathName);

            return recipe;
        }

        public static List<ActorObject> GetConnectedBuildings(ActorObject obj, string port, string conveyorPort = "")
        {
            List<ActorObject> objects = [];
            foreach (var comp in obj.Components)
            {
                string type = comp.PathName[(comp.PathName.LastIndexOf('.') + 1)..];
                if (!type.Contains(port) && (conveyorPort.Length == 0 || !type.Contains(conveyorPort))) continue;

                ActCompObject? buildingComp = s_saveFileReader.GetActCompObject(comp.PathName);
                if (buildingComp == null) continue;

                ObjectProperty? buildingProperty = (ObjectProperty?)SaveFileReader.GetPropertyByName(buildingComp, "mConnectedComponent")?.Property;
                if (buildingProperty == null) continue;

                string buildingPathName = buildingProperty.Reference.PathName[..buildingProperty.Reference.PathName.LastIndexOf('.')];
                ActorObject? building = (ActorObject?)s_saveFileReader.GetActCompObject(buildingPathName);
                if (building == null) continue;

                if (IsConveyor(buildingPathName))
                {
                    string buildingPort = buildingProperty.Reference.PathName[(buildingProperty.Reference.PathName.LastIndexOf('.') + 1)..];
                    objects.AddRange(GetConnectedBuildings(building, port, GetNextConveyorPort(buildingPort)));
                }
                else if (IsMerger(buildingPathName))
                {
                    objects.AddRange(GetConnectedBuildings(building, port));
                }
                else if (IsSplitter(buildingPathName))
                {
                    objects.AddRange(GetConnectedBuildings(building, port));
                }
                else
                {
                    objects.Add(building);
                }
            }

            return objects;
        }

        private static string GetNextConveyorPort(string port)
        {
            return port == "ConveyorAny0" ? "ConveyorAny1" : "ConveyorAny0";
        }

        private static bool IsConveyor(string pathName)
        {
            string conveyor = "Persistent_Level:PersistentLevel.Build_ConveyorBelt";
            string conveyorLift = "Persistent_Level:PersistentLevel.Build_ConveyorLift";
            return (pathName.Length >= conveyor.Length && pathName[..conveyor.Length] == conveyor) ||
                (pathName.Length >= conveyorLift.Length && pathName[..conveyorLift.Length] == conveyorLift);
        }

        private static bool IsMerger(string pathName)
        {
            string merger = "Persistent_Level:PersistentLevel.Build_ConveyorAttachmentMerger";
            return pathName.Length >= merger.Length && pathName[..merger.Length] == merger;
        }

        private static bool IsSplitter(string pathName)
        {
            string splitter = "Persistent_Level:PersistentLevel.Build_ConveyorAttachmentSplitter";
            return pathName.Length >= splitter.Length && pathName[..splitter.Length] == splitter;
        }
    }
}
