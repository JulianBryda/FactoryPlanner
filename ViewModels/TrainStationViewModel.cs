using DynamicData;
using FactoryPlanner.Assets;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using log4net;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

            foreach (var stationIdentifier in GetTrainStationIdentifiers())
            {
                string name = GetTrainStationName(stationIdentifier);

                TrainStations.Add(new TrainStation()
                {
                    Name = name,
                    DockingStations = []
                });
            }

            //Task.Run(() =>
            //{
            //    LoadTrainStations(0, 20);
            //});
        }

        void LoadTrainStations(int startIndex, int count)
        {
            var stationIdentifiers = GetTrainStationIdentifiers();

            for (int i = startIndex; i < startIndex + count && i < stationIdentifiers.Count; i++)
            {
                ActorObject stationIdentifier = stationIdentifiers[i];
                ActorObject station = GetStationFromIdentifier(stationIdentifier);

                string name = GetTrainStationName(stationIdentifier);
                List<DockingStation> dockingStations = LoadConnectedDockingStations(station);

                TrainStations.Add(new TrainStation()
                {
                    Name = name,
                    DockingStations = dockingStations
                });

                _log.Info($"Loaded Train Station \"{name}\" with 2T/{dockingStations.Count}W!");
            }

            _log.Info("Finished loading Train Stations!");
        }

        private readonly List<string> _loadedDockingStations = [];
        List<DockingStation> LoadConnectedDockingStations(ActorObject station)
        {
            List<DockingStation> dockingStations = [];

            int componentIndex = 3;
            if (GetPropertyByName(station, "mIsOrientationReversed") != null)
            {
                componentIndex = 4;
            }

            if (_loadedDockingStations.Contains(station.Components[componentIndex].PathName)) return dockingStations;

            _loadedDockingStations.Add(station.Components[componentIndex].PathName);

            ComponentObject? compObject = (ComponentObject?)_saveFileReader.GetActCompObject(station.Components[componentIndex].PathName);
            if (compObject == null) return dockingStations;

            PropertyListEntry? connectedToEntry = compObject.Properties.FirstOrDefault(o => o.Name == "mConnectedTo");
            if (connectedToEntry == null) return dockingStations;

            ObjectProperty? connectedTo = (ObjectProperty?)connectedToEntry.Property;
            if (connectedTo == null) return dockingStations;

            string pathName = connectedTo.Reference.PathName[..connectedTo.Reference.PathName.LastIndexOf('.')];
            ActorObject? dockingStation = (ActorObject?)_saveFileReader.GetActCompObject(pathName);
            if (dockingStation == null) return dockingStations;

            if (LoadDockingStation(dockingStation, pathName) is DockingStation value)
            {
                dockingStations.Add(value);
            }

            dockingStations.AddRange(LoadConnectedDockingStations(dockingStation));

            return dockingStations;
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
            PropertyListEntry? propertyEntry = GetPropertyByName(dockingStation, "mInventory");
            if (propertyEntry == null) return null; // ActorObjet is not a docking station

            var neededItems = GetDockingStationItemsByPort(dockingStation, pathName, PortType.Output);
            var outgoingItems = GetDockingStationItemsByPort(dockingStation, pathName, PortType.Input);

            return new DockingStation()
            {
                IncomingItems = [],
                OutgoingItems = outgoingItems,
                NeededItems = neededItems
            };
        }

        List<DockingStation.Item> GetDockingStationItemsByPort(ActorObject dockingStation, string pathName, PortType portType)
        {
            List<ActorObject> buildings = GetConnectedBuildings(dockingStation, pathName, portType);

            List<DockingStation.Item> items = [];
            foreach (ActorObject building in buildings)
            {
                if (GetPropertyByName(building, "mCurrentRecipe")?.Property is ObjectProperty itemProperty)
                {
                    string itemPathName = itemProperty.Reference.PathName;

                    Recipe? recipe = AssetManager.GetRecipe(itemPathName);
                    if (recipe == null) continue;

                    FloatProperty? currentPotential = (FloatProperty?)GetPropertyByName(building, "mCurrentPotential")?.Property;

                    float prodRate = 60f / recipe.Time * recipe.Products.First().Amount;
                    if (currentPotential != null) prodRate *= currentPotential.Value;

                    if (items.Find(o => o.ItemPathName == itemPathName) is DockingStation.Item item)
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

                        items.Add(new DockingStation.Item()
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
                PropertyListEntry? conComponent = GetPropertyByName(port, "mConnectedComponent");
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

        private List<ActorObject> GetTrainStationIdentifiers()
        {
            ActorObject railRoadSubSystem = (ActorObject?)_saveFileReader.GetActCompObject("Persistent_Level:PersistentLevel.RailroadSubsystem") ?? throw new NullReferenceException("Failed to load railSubSystem!");
            PropertyListEntry identifierEntry = railRoadSubSystem.Properties.FirstOrDefault() ?? throw new NullReferenceException("Failed to load TrainStationIdentifiers!");
            ArrayProperty identifiersProperty = (ArrayProperty?)identifierEntry.Property ?? throw new NullReferenceException("Failed to load IdentifierProperty!");

            List<ActorObject> identifiers = [];
            foreach (SimpleObjectProperty property in identifiersProperty.Properties.Cast<SimpleObjectProperty>())
            {
                ActorObject identifier = (ActorObject?)_saveFileReader.GetActCompObject(property.Value.PathName) ?? throw new NullReferenceException("This should not fail!");
                identifiers.Add(identifier);
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

        private static PropertyListEntry? GetPropertyByName(ActCompObject obj, string name)
        {
            foreach (var entry in obj.Properties)
            {
                if (entry.Name == name)
                    return entry;
            }

            return null;
        }

        private static string GetItemName(string itemPath)
        {
            string trimed = itemPath[(itemPath.LastIndexOf('.') + 1)..].Replace("Recipe_", "").Replace("Alternate_", "");
            return trimed[..trimed.IndexOf('_')];
        }


        private enum PortType
        {
            Input,
            Output
        }


    }
}
