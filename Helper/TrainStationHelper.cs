using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FactoryPlanner.FileReader;
using log4net;
using FactoryPlanner.Assets;
using static FactoryPlanner.Models.DockingStation;
using System.IO;

namespace FactoryPlanner.Helper
{
    internal class TrainStationHelper
    {
        private static readonly SaveFileReader s_saveFileReader = SaveFileReader.LoadedSaveFile;
        private static readonly ILog s_log = LogManager.GetLogger(typeof(TrainStationHelper));

        private static readonly Dictionary<int, TrainStation> s_trainStationCache = [];

        public static TrainStation GetTrainStation(ActorObject station, string name, string stationIdentifierPathName, bool setIncomingItems = true)
        {
            if (s_trainStationCache.TryGetValue(stationIdentifierPathName.GetHashCode(), out TrainStation? outStation))
            {
                if (setIncomingItems && !outStation.IncomingItemsSet)
                {
                    SetIncomingItemsForStation(outStation, stationIdentifierPathName);
                    outStation.IncomingItemsSet = true;
                }

                return outStation;
            }

            bool flipped = false;

            // get first TrainStation of whole Train-/DockingStation complex
            if (LoadConnectedStation(station, StationConnection.Front, out string? pathName, ref flipped) is ActorObject tempStation && pathName != null && TrainHelper.IsTrainStation(pathName))
            {
                return GetTrainStation(tempStation, name, stationIdentifierPathName, setIncomingItems);
            }

            TrainStation trainStation = new()
            {
                Name = name,
                TrainStationCount = 1,
                IncomingItemsSet = setIncomingItems,
                DockingStations = []
            };

            while (LoadConnectedStation(station, StationConnection.Back, out pathName, ref flipped) is ActorObject connectedStation && pathName != null)
            {
                if (TrainHelper.IsDockingStation(pathName))
                {
                    if (LoadDockingStation(connectedStation, pathName) is DockingStation value)
                    {
                        trainStation.DockingStations.Add(value);
                    }
                }
                else
                {
                    trainStation.TrainStationCount++;
                }

                station = connectedStation;
            }

            s_trainStationCache.Add(stationIdentifierPathName.GetHashCode(), trainStation);

            if (setIncomingItems)
                SetIncomingItemsForStation(trainStation, stationIdentifierPathName);

            return trainStation;
        }

        private static void SetIncomingItemsForStation(TrainStation trainStation, string stationIdentifierPathName)
        {
            var trains = TrainHelper.GetTrainsByStop(stationIdentifierPathName);

            foreach (Train train in trains)
            {
                if (train.TimeTable == null) return;

                foreach (TimeTableStop stop in train.TimeTable.Stops)
                {
                    if (stop.StationIdentifierPathName != stationIdentifierPathName) continue;

                    bool isTrainReversed = TrainHelper.IsTrainReversedAtStation(train, stop.StationIdentifierPathName);

                    for (int i = 0; i < trainStation.DockingStations.Count && i < train.FreightWagons.Count; i++)
                    {
                        int freightIndex = i;
                        if (isTrainReversed)
                            freightIndex = train.FreightWagons.Count - 1 - i;

                        var dockingStation = trainStation.DockingStations[i];
                        var freightWagon = train.FreightWagons[freightIndex];

                        SetIncomingItems(dockingStation, freightWagon, stop);
                    }
                }
            }
        }

        private static void SetIncomingItems(DockingStation dockingStation, FreightWagon freightWagon, TimeTableStop stop)
        {
            foreach (var item in dockingStation.NeededItems)
            {
                if (stop.UnloadFilter.Count > 0 &&
                    !stop.UnloadFilter.Any(o => o[(o.LastIndexOf('.') + 1)..] == item.ItemPathName)) continue;

                if (freightWagon.Items.Find(o => o.ItemPathName == item.ItemPathName) is Item foundItem)
                {
                    // docking station
                    if (dockingStation.IncomingItems.Find(o => o.ItemPathName == item.ItemPathName) is Item dockingItem)
                        dockingItem.Rate += foundItem.Rate;
                    else
                        dockingStation.IncomingItems.Add(new Item()
                        {
                            Icon = foundItem.Icon,
                            ItemPathName = foundItem.ItemPathName,
                            Rate = foundItem.Rate
                        });

                    s_log.Info($"Incoming Item: {foundItem.ItemPathName} Amount: {foundItem.Rate}");

                    // freight wagon
                    foundItem.Rate -= item.Rate;
                }
            }
        }

        /// <summary>
        /// get's the connected station
        /// </summary>
        /// <param name="station">ActorOnject of station</param>
        /// <param name="connection">Front or Back</param>
        /// <returns></returns>
        private static ActorObject? LoadConnectedStation(ActorObject station, StationConnection connection, out string? pathName, ref bool flipped)
        {
            if (flipped == true && TrainHelper.IsTrainStation(station.Components.First().PathName))
            {
                flipped = false;
            }

            if (SaveFileReader.GetPropertyByName(station, "mIsOrientationReversed") != null && !flipped)
            {
                connection = (connection == StationConnection.Front) ? StationConnection.Back : StationConnection.Front;
            }
            int componentIndex = 2 + (int)connection;
            pathName = null;

            ComponentObject? test = (ComponentObject?)s_saveFileReader.GetActCompObject(station.Components[componentIndex - 1].PathName);

            ComponentObject? compObject = (ComponentObject?)s_saveFileReader.GetActCompObject(station.Components[componentIndex].PathName);
            if (compObject != null && SaveFileReader.GetPropertyByName(compObject, "platformOwner") != null)
            {
                // connected station is empty platform, take other connection because empty platform are stupid
                // also invert mIsOrientationReversed cause empty platform changes the orientation for connected stations (stupid again)
                flipped = SaveFileReader.GetPropertyByName(compObject, "mComponentDirection") != null;
                componentIndex = (componentIndex == 3) ? 4 : 3;
                compObject = (ComponentObject?)s_saveFileReader.GetActCompObject(station.Components[componentIndex].PathName);

                s_log.Info("Used other connection and flipped!");
            }
            if (compObject == null) return null;

            PropertyListEntry? connectedToEntry = compObject.Properties.FirstOrDefault(o => o.Name == "mConnectedTo");
            if (connectedToEntry == null) return null;

            ObjectProperty? connectedTo = (ObjectProperty?)connectedToEntry.Property;
            if (connectedTo == null) return null;

            string stationPathName = connectedTo.Reference.PathName[..connectedTo.Reference.PathName.LastIndexOf('.')];
            pathName = stationPathName;

            return (ActorObject?)s_saveFileReader.GetActCompObject(stationPathName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="station">ActorObject of a docking station</param>
        /// <param name="pathName">PathName of the docking station</param>
        /// <returns>DockingStation, if wrong ActorObject returns null</returns>
        /// <exception cref="ArgumentException"></exception>
        private static DockingStation? LoadDockingStation(ActorObject dockingStation, string pathName)
        {
            PropertyListEntry? propertyEntry = SaveFileReader.GetPropertyByName(dockingStation, "mInventory");
            if (propertyEntry == null) return null; // ActorObjet is not a docking station

            var neededItems = GetDockingStationItemsByPort(dockingStation, pathName, PortType.Output);
            var outgoingItems = GetDockingStationItemsByPort(dockingStation, pathName, PortType.Input);
            //var incomingItems = GetIncomingToDockingStation(dockingStation, pathName);

            return new DockingStation()
            {
                IncomingItems = [],
                OutgoingItems = outgoingItems,
                NeededItems = neededItems
            };
        }

        private static List<Item> GetDockingStationItemsByPort(ActorObject dockingStation, string pathName, PortType portType)
        {
            List<ActorObject> buildings = GetConnectedBuildings(dockingStation, [pathName], portType);

            List<Item> items = [];
            foreach (ActorObject building in buildings)
            {
                if (SaveFileReader.GetPropertyByName(building, "mCurrentRecipe")?.Property is ObjectProperty itemProperty)
                {
                    string itemPathName = itemProperty.Reference.PathName;

                    Recipe? recipe = AssetManager.GetRecipe(itemPathName);
                    if (recipe == null) continue;

                    FloatProperty? currentPotential = (FloatProperty?)SaveFileReader.GetPropertyByName(building, "mCurrentPotential")?.Property;

                    if (portType == PortType.Input)
                    {
                        GetItemsByProducts(recipe.Products, recipe.Time, currentPotential?.Value ?? 1f, ref items);
                    }
                    else
                    {
                        GetItemsByProducts(recipe.Ingredients, recipe.Time, currentPotential?.Value ?? 1f, ref items);
                    }
                }
                else if (SaveFileReader.GetPropertyByName(building, "mExtractableResource")?.Property is ObjectProperty ressourceProperty)
                {
                    RessourceNode? ressourceNode = AssetManager.GetRessourceNode(ressourceProperty.Reference.PathName);
                    if (ressourceNode == null) continue;

                    string minerPathName = building.Components.First().PathName;
                    int minerLevel = minerPathName.Contains("MinerMk1") ? 1 : minerPathName.Contains("MinerMk2") ? 2 : 3;
                    FloatProperty? currentPotential = (FloatProperty?)SaveFileReader.GetPropertyByName(building, "mCurrentPotential")?.Property;

                    GetItemsByProducts([new Product() { Item = ressourceNode.ItemPathName, Amount = ressourceNode.ItemsExtracted(minerLevel) }], 60f, currentPotential?.Value ?? 1f, ref items);
                }
            }

            return items;
        }

        /// <summary>
        /// Takes in the Products/Ingredients of a recipe and outputs a list of Items
        /// </summary>
        /// <param name="products">products or ingredients of recipe</param>
        /// <param name="recipeTime">recipe time</param>
        /// <param name="currentPotential">current potential of building hosting recipe</param>
        /// <returns>a list of Items based on the products</returns>
        private static void GetItemsByProducts(Product[] products, float recipeTime, float currentPotential, ref List<Item> items)
        {
            foreach (var product in products)
            {
                float prodRate = 60f / recipeTime * product.Amount;
                prodRate *= currentPotential;

                if (items.Find(o => o.ItemPathName == product.Item) is Item item)
                {
                    item.Rate += prodRate;
                }
                else
                {
                    string itemName = product.Item[5..^2];
                    string iconPath = $".\\Assets\\Icons\\Items\\{itemName}.png";
                    if (!Path.Exists(iconPath))
                    {
                        iconPath = ".\\Assets\\Missing.png";
                        s_log.Warn($"Icon for item \"{itemName}\" is missing!");
                    }

                    items.Add(new Item()
                    {
                        Icon = new(iconPath),
                        ItemPathName = product.Item,
                        Rate = prodRate,
                    });
                }
            }
        }

        private static List<ActorObject> GetConnectedBuildings(ActorObject building, List<string> buildingPathNames, PortType portType)
        {
            List<ActorObject> connectedBuildings = [];
            List<ActCompObject> ports;
            if (portType == PortType.Input)
                ports = GetInPorts(building);
            else
                ports = GetOutPorts(building);

            foreach (var port in ports)
            {
                PropertyListEntry? conComponent = SaveFileReader.GetPropertyByName(port, "mConnectedComponent");
                if (conComponent == null) continue;

                ObjectProperty? objProperty = (ObjectProperty?)conComponent.Property;
                if (objProperty == null) continue;

                string conveyorPathName = objProperty.Reference.PathName[..objProperty.Reference.PathName.LastIndexOf('.')];
                ActorObject? conveyorObject = (ActorObject?)s_saveFileReader.GetActCompObject(conveyorPathName);
                if (conveyorObject == null) continue;

                ActorObject? connectedBuilding = GetConnectedBuildingFromConveyor(conveyorObject, buildingPathNames);
                if (connectedBuilding == null || connectedBuilding.ComponentCount == 0) continue;

                string connectedBuildingPathName = connectedBuilding.Components.First().PathName;
                connectedBuildingPathName = connectedBuildingPathName[..connectedBuildingPathName.LastIndexOf('.')];

                bool isConveyorAttachment = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_ConveyorAttachment");
                bool isStorageContainer = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_StorageContainer");
                bool isConveyor = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_Conveyor"); // also true when ConveyorAttachment

                buildingPathNames.Add(connectedBuildingPathName);

                if (isConveyorAttachment || isStorageContainer)
                {
                    connectedBuildings.AddRange(GetConnectedBuildings(connectedBuilding, buildingPathNames, portType));
                }
                else if (isConveyor)
                {
                    var value = GetConnectedBuildingFromConveyor(connectedBuilding, buildingPathNames);
                    if (value != null)
                        connectedBuildings.Add(value);
                }
                else
                {
                    connectedBuildings.Add(connectedBuilding);
                }
            }


            return connectedBuildings;
        }

        private static List<ActCompObject> GetInPorts(ActorObject obj)
        {
            return GetPorts(obj, "Input");
        }

        private static List<ActCompObject> GetOutPorts(ActorObject obj)
        {
            return GetPorts(obj, "Output");
        }

        private static List<ActCompObject> GetPorts(ActorObject obj, string type)
        {
            List<ActCompObject> objects = [];
            foreach (var component in obj.Components)
            {
                string pathName = component.PathName[component.PathName.LastIndexOf('.')..];
                if (pathName.Contains(type))
                {
                    ActCompObject? inOut = s_saveFileReader.GetActCompObject(component.PathName);
                    if (inOut != null)
                    {
                        objects.Add(inOut);
                    }
                }
            }

            return objects;
        }

        public static Dictionary<string, ActorObject> GetTrainStationIdentifiers()
        {
            ActorObject railRoadSubSystem = TrainHelper.GetRailroadSubsystem();
            PropertyListEntry identifierEntry = railRoadSubSystem.Properties.FirstOrDefault() ?? throw new NullReferenceException("Failed to load TrainStationIdentifiers!");
            ArrayProperty identifiersProperty = (ArrayProperty?)identifierEntry.Property ?? throw new NullReferenceException("Failed to load IdentifierProperty!");

            Dictionary<string, ActorObject> identifiers = [];
            foreach (SimpleObjectProperty property in identifiersProperty.Properties.Cast<SimpleObjectProperty>())
            {
                ActorObject identifier = (ActorObject?)s_saveFileReader.GetActCompObject(property.Value.PathName) ?? throw new NullReferenceException("This should not fail!");
                identifiers.Add(property.Value.PathName, identifier);
            }

            return identifiers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conveyor">An ActorObject of a Conveyor Belt</param>
        /// <param name="filter">A pathName for the building to ignore</param>
        /// <returns>An ActorObject to the connected building, null when not connected or wrong ActorObject passed</returns>
        private static ActorObject? GetConnectedBuildingFromConveyor(ActorObject conveyor, List<string> filter)
        {
            foreach (var item in conveyor.Components)
            {
                ComponentObject? conveyorComponent = (ComponentObject?)s_saveFileReader.GetActCompObject(item.PathName);
                if (conveyorComponent != null &&
                    conveyorComponent.Properties.Length == 2 &&
                    conveyorComponent.Properties[1].Property is ObjectProperty property)
                {
                    string pathName = property.Reference.PathName;
                    pathName = pathName[..pathName.LastIndexOf('.')];

                    bool isConveyorAttachment = pathName.Contains("Persistent_Level:PersistentLevel.Build_ConveyorAttachment");
                    bool isConveyor = pathName.Contains("Persistent_Level:PersistentLevel.Build_Conveyor"); // also true when ConveyorAttachment

                    if (!filter.Contains(pathName))
                    {
                        if (!isConveyorAttachment && isConveyor)
                        {
                            if (s_saveFileReader.GetActCompObject(pathName) is not ActorObject newConveyor) return null;
                            string newFilter = item.PathName[..item.PathName.LastIndexOf('.')];
                            filter.Add(newFilter);

                            return GetConnectedBuildingFromConveyor(newConveyor, filter);
                        }
                        else
                        {
                            return s_saveFileReader.GetActCompObject(pathName) as ActorObject;
                        }
                    }
                }
            }

            return null;
        }

        public static string GetTrainStationName(ActorObject stationIdentifier)
        {
            PropertyListEntry stationNameProperty = stationIdentifier.Properties.Last();
            TextProperty textProperty = (TextProperty?)stationNameProperty.Property ?? throw new NullReferenceException("StationIdentifier has no StationName!");

            return textProperty.Value;
        }

        public static List<string> GetFilterDescriptors(StructProperty dockingRuleSetProperty, string descriptorName)
        {
            List<string> descriptors = [];

            PropertyListEntry? entry = (PropertyListEntry?)dockingRuleSetProperty.Properties.FirstOrDefault(o => ((PropertyListEntry)o).Name == descriptorName);
            if (entry == null) return descriptors;

            ArrayProperty? property = (ArrayProperty?)entry.Property;
            if (property == null) return descriptors;

            foreach (SimpleObjectProperty item in property.Properties.Cast<SimpleObjectProperty>())
            {
                descriptors.Add(item.Value.PathName);
            }

            return descriptors;
        }

        public static ActorObject GetStationFromIdentifier(ActorObject stationIdentifier)
        {
            ObjectProperty stationProperty = (ObjectProperty?)stationIdentifier.Properties.First().Property ?? throw new NullReferenceException("Objectproperty mStation of StationIdentifier is null!");
            string stationPathName = stationProperty.Reference.PathName;
            ActorObject station = (ActorObject?)s_saveFileReader.GetActCompObject(stationPathName) ?? throw new NullReferenceException("Stationidentifier has no Station!");

            return station;
        }

        public static bool IsTrainStationDeadEnd(string stationIdentifierPathName) =>
    IsTrainStationDeadEnd(GetStationFromIdentifier((ActorObject?)s_saveFileReader.GetActCompObject(stationIdentifierPathName) ?? throw new Exception("Could not get Station Identifier!")));
        public static bool IsTrainStationDeadEnd(ActorObject trainStation)
        {
            PropertyListEntry track = SaveFileReader.GetPropertyByName(trainStation, "mRailroadTrack") ?? throw new NullReferenceException("Station has no integrated track!"); // if this error get's triggered i am cooked
            ObjectProperty trackProperty = (ObjectProperty?)track.Property ?? throw new NullReferenceException("Track Property is null!"); // if this error get's triggered i am cooked too
            ActorObject integratedTrack = (ActorObject?)s_saveFileReader.GetActCompObject(trackProperty.Reference.PathName) ?? throw new NullReferenceException("Failed to get integrated track!"); // if this error get's triggered i am cooked too

            bool front = TraceRailroad(integratedTrack, 0, 5);
            bool back = TraceRailroad(integratedTrack, 1, 5);

            return front == false || back == false;
        }

        public static bool TraceRailroad(ActorObject track, int direction, int depth)
        {
            if (depth == 0) return true;

            ObjectReference? connectedReference = track.Components.FirstOrDefault(o => o.PathName.Contains($"TrackConnection{direction}"));
            if (connectedReference == null) return false;

            ComponentObject? connected = (ComponentObject?)s_saveFileReader.GetActCompObject(connectedReference.PathName);
            if (connected == null) return false;

            PropertyListEntry? connectedComponents = SaveFileReader.GetPropertyByName(connected, "mConnectedComponents");
            if (connectedComponents == null) return false;

            ArrayProperty? connectedComponentsProperty = (ArrayProperty?)connectedComponents.Property;
            if (connectedComponentsProperty == null) return false;

            foreach (SimpleObjectProperty item in connectedComponentsProperty.Properties.Cast<SimpleObjectProperty>())
            {
                ActorObject? nextTrack = (ActorObject?)s_saveFileReader.GetActCompObject(item.Value.PathName[..item.Value.PathName.LastIndexOf('.')]);
                if (nextTrack == null) return false;

                return TraceRailroad(nextTrack, direction, depth - 1);
            }

            return false;
        }

        private enum PortType
        {
            Input,
            Output
        }

        private enum StationConnection
        {
            Front = 2,
            Back = 1
        }
    }
}
