using DynamicData;
using FactoryPlanner.Assets;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Helper;
using FactoryPlanner.Models;
using log4net;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using static FactoryPlanner.Models.DockingStation;

namespace FactoryPlanner.ViewModels
{
    public class TrainStationViewModel : ViewModelBase
    {
        public List<TrainStation> TrainStations { get; set; } = [];

        private readonly SaveFileReader _saveFileReader;
        private readonly ILog _log = LogManager.GetLogger(typeof(TrainStationViewModel));

        public TrainStationViewModel(IScreen screen) : base(screen)
        {
            _saveFileReader = SaveFileReader.LoadedSaveFile;

            LoadTrainStations(0, 5);
            //LoadSimpleTrainStations();
        }

        void LoadSimpleTrainStations()
        {
            foreach (var stationIdentifier in GetTrainStationIdentifiers())
            {
                string name = GetTrainStationName(stationIdentifier.Value);

                TrainStations.Add(new TrainStation()
                {
                    Name = name,
                    TrainStationCount = 0,
                    DockingStations = []
                });
            }
        }

        void LoadTrainStations(int startIndex, int count)
        {
            var stationIdentifiers = GetTrainStationIdentifiers();

            for (int i = startIndex; i < startIndex + count && i < stationIdentifiers.Count; i++)
            {
                ActorObject stationIdentifier = stationIdentifiers.ElementAt(i).Value;
                ActorObject station = GetStationFromIdentifier(stationIdentifier);

                string name = GetTrainStationName(stationIdentifier);
                TrainStation trainStation = LoadTrainStation(station, name);
                var trains = TrainHelper.GetTrainsByStop(stationIdentifiers.ElementAt(i).Key);
                var items = CalculateTotalItems(trains, stationIdentifiers.ElementAt(i).Key);

                foreach (var docking in trainStation.DockingStations) 
                {
                    docking.IncomingItems = items;
                    // TODO assign items to freight container, find way to filter double sided trains
                }

                TrainStations.Add(trainStation);

                _log.Info($"Loaded Train Station \"{name}\" with {trainStation.TrainStationCount}T/{trainStation.DockingStations.Count}W!");
            }

            _log.Info("Finished loading Train Stations!");
        }

        TrainStation LoadTrainStation(ActorObject station, string name)
        {
            // get first TrainStation of whole Train-/DockingStation complex
            if (LoadConnectedStation(station, StationConnection.Front, out string? pathName) is ActorObject tempStation && pathName != null && TrainHelper.IsTrainStation(pathName))
            {
                return LoadTrainStation(tempStation, name);
            }

            TrainStation trainStation = new()
            {
                Name = name,
                TrainStationCount = 1,
                DockingStations = []
            };

            while (LoadConnectedStation(station, StationConnection.Back, out pathName) is ActorObject connectedStation && pathName != null)
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

            return trainStation;
        }

        /// <summary>
        /// get's the connected station
        /// </summary>
        /// <param name="station">ActorOnject of station</param>
        /// <param name="connection">Front or Back</param>
        /// <returns></returns>
        bool flipped = false;
        ActorObject? LoadConnectedStation(ActorObject station, StationConnection connection, out string? pathName)
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

            ComponentObject? test = (ComponentObject?)_saveFileReader.GetActCompObject(station.Components[componentIndex - 1].PathName);

            ComponentObject? compObject = (ComponentObject?)_saveFileReader.GetActCompObject(station.Components[componentIndex].PathName);
            if (compObject != null && SaveFileReader.GetPropertyByName(compObject, "platformOwner") != null)
            {
                // connected station is empty platform, take other connection because empty platform are stupid
                // also invert mIsOrientationReversed cause empty platform changes the orientation for connected stations (stupid again)
                flipped = SaveFileReader.GetPropertyByName(compObject, "mComponentDirection") != null;
                componentIndex = (componentIndex == 3) ? 4 : 3;
                compObject = (ComponentObject?)_saveFileReader.GetActCompObject(station.Components[componentIndex].PathName);

                _log.Info("Used other connection and flipped!");
            }
            if (compObject == null) return null;

            PropertyListEntry? connectedToEntry = compObject.Properties.FirstOrDefault(o => o.Name == "mConnectedTo");
            if (connectedToEntry == null) return null;

            ObjectProperty? connectedTo = (ObjectProperty?)connectedToEntry.Property;
            if (connectedTo == null) return null;

            string stationPathName = connectedTo.Reference.PathName[..connectedTo.Reference.PathName.LastIndexOf('.')];
            pathName = stationPathName;

            return (ActorObject?)_saveFileReader.GetActCompObject(stationPathName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="station">ActorObject of a docking station</param>
        /// <param name="pathName">PathName of the docking station</param>
        /// <returns>DockingStation, if wrong ActorObject returns null</returns>
        /// <exception cref="ArgumentException"></exception>
        DockingStation? LoadDockingStation(ActorObject dockingStation, string pathName)
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

        List<Item> GetDockingStationItemsByPort(ActorObject dockingStation, string pathName, PortType portType)
        {
            List<ActorObject> buildings = GetConnectedBuildings(dockingStation, pathName, portType);

            List<Item> items = [];
            foreach (ActorObject building in buildings)
            {
                if (SaveFileReader.GetPropertyByName(building, "mCurrentRecipe")?.Property is ObjectProperty itemProperty)
                {
                    string itemPathName = itemProperty.Reference.PathName;

                    Recipe? recipe = AssetManager.GetRecipe(itemPathName);
                    if (recipe == null) continue;

                    FloatProperty? currentPotential = (FloatProperty?)SaveFileReader.GetPropertyByName(building, "mCurrentPotential")?.Property;

                    float prodRate = 60f / recipe.Time * recipe.Products.First().Amount;
                    if (currentPotential != null) prodRate *= currentPotential.Value;

                    if (items.Find(o => o.ItemPathName == itemPathName) is Item item)
                    {
                        item.Rate += prodRate;
                    }
                    else
                    {
                        string itemName = GetItemName(itemPathName);
                        string iconPath = $".\\Assets\\Icons\\Items\\{itemName}.png";
                        if (!Path.Exists(iconPath))
                        {
                            iconPath = ".\\Assets\\Missing.png";
                            _log.Warn($"Icon for item \"{itemName}\" is missing!");
                        }

                        items.Add(new Item()
                        {
                            Icon = new(iconPath),
                            ItemPathName = itemPathName,
                            Rate = prodRate,
                        });
                    }
                }
            }

            return items;
        }

        List<Item> CalculateTotalItems(List<Train> trains, string stationIdentifierPathName)
        {
            List<Item> items = [];

            foreach (Train train in trains)
            {
                if (IsUnsupportedTrain(train)) continue;

                items.AddRange(CalculateItemsByTrain(train, stationIdentifierPathName));
            }

            return items;
        }

        List<Item> CalculateItemsByTrain(Train train, string stationIdentifierPathName)
        {
            List<Item> items = [];
            if (train.TimeTable == null) return items;

            foreach (TimeTableStop stop in train.TimeTable.Stops)
            {
                if (stop.StationIdentifierPathName == stationIdentifierPathName) continue;
                ActorObject? stationIdentifier = (ActorObject?)_saveFileReader.GetActCompObject(stop.StationIdentifierPathName);
                if (stationIdentifier == null) continue;

                ActorObject actorStation = GetStationFromIdentifier(stationIdentifier);
                TrainStation station = LoadTrainStation(actorStation, "");

                for (int i = 0; i < station.DockingStations.Count && i < train.FreightWagons.Count; i++)
                {
                    var dockingStation = station.DockingStations[i];

                    foreach (var item in dockingStation.NeededItems)
                    {
                        if (stop.UnloadFilter.Count > 0 && !stop.UnloadFilter.Contains(item.ItemPathName)) continue;

                        if (items.Find(o => o.ItemPathName == item.ItemPathName) is Item foundItem)
                        {
                            foundItem.Rate -= item.Rate;
                        }
                        else
                        {
                            items.Add(item);
                        }
                    }

                    foreach (var item in dockingStation.OutgoingItems)
                    {
                        if (stop.LoadFilter.Count > 0 && !stop.LoadFilter.Contains(item.ItemPathName)) continue;

                        if (items.Find(o => o.ItemPathName == item.ItemPathName) is Item foundItem)
                        {
                            foundItem.Rate += item.Rate;
                        }
                        else
                        {
                            items.Add(item);
                        }
                    }
                }
            }

            return items;
        }

        List<ActorObject> GetConnectedBuildings(ActorObject building, string buildingPathName, PortType portType)
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
                ActorObject? conveyorObject = (ActorObject?)_saveFileReader.GetActCompObject(conveyorPathName);
                if (conveyorObject == null) continue;

                ActorObject? connectedBuilding = GetConnectedBuildingFromConveyor(conveyorObject, buildingPathName);
                if (connectedBuilding == null || connectedBuilding.ComponentCount == 0) continue;

                string connectedBuildingPathName = connectedBuilding.Components.First().PathName;
                connectedBuildingPathName = connectedBuildingPathName[..connectedBuildingPathName.LastIndexOf('.')];
                bool isConveyorAttachment = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_ConveyorAttachment");
                bool isConveyor = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_Conveyor"); // also true when ConveyorAttachment
                if (isConveyorAttachment)
                {
                    connectedBuildings.AddRange(GetConnectedBuildings(connectedBuilding, connectedBuildingPathName, PortType.Input));
                }
                else if (isConveyor)
                {
                    var value = GetConnectedBuildingFromConveyor(connectedBuilding, connectedBuildingPathName);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conveyor">An ActorObject of a Conveyor Belt</param>
        /// <param name="filter">A pathName for the building to ignore</param>
        /// <returns>An ActorObject to the connected building, null when not connected or wrong ActorObject passed</returns>
        ActorObject? GetConnectedBuildingFromConveyor(ActorObject conveyor, string filter)
        {
            foreach (var item in conveyor.Components)
            {
                ComponentObject? conveyorComponent = (ComponentObject?)_saveFileReader.GetActCompObject(item.PathName);
                if (conveyorComponent != null &&
                    conveyorComponent.Properties.Length == 2 &&
                    conveyorComponent.Properties[1].Property is ObjectProperty property)
                {
                    string pathName = property.Reference.PathName;
                    pathName = pathName[..pathName.LastIndexOf('.')];

                    bool isConveyorAttachment = pathName.Contains("Persistent_Level:PersistentLevel.Build_ConveyorAttachment");
                    bool isConveyor = pathName.Contains("Persistent_Level:PersistentLevel.Build_Conveyor"); // also true when ConveyorAttachment

                    if (filter != pathName)
                    {
                        if (!isConveyorAttachment && isConveyor)
                        {
                            if (_saveFileReader.GetActCompObject(pathName) is not ActorObject newConveyor) return null;

                            return GetConnectedBuildingFromConveyor(newConveyor, item.PathName[..item.PathName.LastIndexOf('.')]);
                        }
                        else
                        {
                            return _saveFileReader.GetActCompObject(pathName) as ActorObject;
                        }
                    }
                }
            }

            return null;
        }

        private List<ActCompObject> GetInPorts(ActorObject obj)
        {
            return GetPorts(obj, "Input");
        }

        private List<ActCompObject> GetOutPorts(ActorObject obj)
        {
            return GetPorts(obj, "Output");
        }

        private List<ActCompObject> GetPorts(ActorObject obj, string type)
        {
            List<ActCompObject> objects = [];
            foreach (var component in obj.Components)
            {
                string pathName = component.PathName[component.PathName.LastIndexOf('.')..];
                if (pathName.Contains(type))
                {
                    ActCompObject? inOut = _saveFileReader.GetActCompObject(component.PathName);
                    if (inOut != null)
                    {
                        objects.Add(inOut);
                    }
                }
            }

            return objects;
        }

        private Dictionary<string, ActorObject> GetTrainStationIdentifiers()
        {
            ActorObject railRoadSubSystem = TrainHelper.GetRailroadSubsystem();
            PropertyListEntry identifierEntry = railRoadSubSystem.Properties.FirstOrDefault() ?? throw new NullReferenceException("Failed to load TrainStationIdentifiers!");
            ArrayProperty identifiersProperty = (ArrayProperty?)identifierEntry.Property ?? throw new NullReferenceException("Failed to load IdentifierProperty!");

            Dictionary<string, ActorObject> identifiers = [];
            foreach (SimpleObjectProperty property in identifiersProperty.Properties.Cast<SimpleObjectProperty>())
            {
                ActorObject identifier = (ActorObject?)_saveFileReader.GetActCompObject(property.Value.PathName) ?? throw new NullReferenceException("This should not fail!");
                identifiers.Add(property.Value.PathName, identifier);
            }

            return identifiers;
        }

        private ActorObject GetStationFromIdentifier(ActorObject stationIdentifier)
        {
            ObjectProperty stationProperty = (ObjectProperty?)stationIdentifier.Properties.First().Property ?? throw new NullReferenceException("Objectproperty mStation of StationIdentifier is null!");
            string stationPathName = stationProperty.Reference.PathName;
            ActorObject station = (ActorObject?)_saveFileReader.GetActCompObject(stationPathName) ?? throw new NullReferenceException("Stationidentifier has no Station!");

            return station;
        }

        private static string GetTrainStationName(ActorObject stationIdentifier)
        {
            PropertyListEntry stationNameProperty = stationIdentifier.Properties.Last();
            TextProperty textProperty = (TextProperty?)stationNameProperty.Property ?? throw new NullReferenceException("StationIdentifier has no StationName!");

            return textProperty.Value;
        }

        private static string GetItemName(string itemPath)
        {
            string trimed = itemPath[(itemPath.LastIndexOf('.') + 1)..].Replace("Recipe_", "").Replace("Alternate_", "");

            return trimed[..trimed.IndexOf('_')];
        }

        //private static bool IsUnsupportedTrain(Train train)
        //{
        //    if (train.Locomotives.Count == 0) return false;

        //    bool isReversed = SaveFileReader.GetPropertyByName(train.Locomotives.First(), "mIsOrientationReversed") != null;

        //    for (int i = 1; i < train.Locomotives.Count; i++)
        //    {
        //        bool reversed = SaveFileReader.GetPropertyByName(train.Locomotives[i], "mIsOrientationReversed") != null;
        //        if (isReversed != reversed)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        private static bool IsUnsupportedTrain(Train train)
        {
            string[] split = train.Layout.Split(';');

            return split.First().Contains('L') && split.Last().Contains('L');
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
