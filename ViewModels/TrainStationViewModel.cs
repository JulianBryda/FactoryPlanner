using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FactoryPlanner.Assets;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Helper;
using FactoryPlanner.Models;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static FactoryPlanner.Models.DockingStation;

namespace FactoryPlanner.ViewModels
{
    public class TrainStationViewModel : ViewModelBase
    {
        private readonly SaveFileReader _saveFileReader;
        private readonly ILog _log = LogManager.GetLogger(typeof(TrainStationViewModel));

        private bool _searchProgressBarVisible = false;

        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public ObservableCollection<TrainStation> TrainStations { get; set; } = [];
        public string SearchText { get; set; } = string.Empty;

        public bool SearchProgressBarVisible
        {
            get => _searchProgressBarVisible;
            set => this.RaiseAndSetIfChanged(ref _searchProgressBarVisible, value);
        }


        public TrainStationViewModel(IScreen screen) : base(screen)
        {
            _saveFileReader = SaveFileReader.LoadedSaveFile;

            SearchCommand = ReactiveCommand.Create(HandleSearchCommand);

            //LoadTrainStations(0, 5);
            //LoadSimpleTrainStations();
        }

        private void HandleSearchCommand()
        {
            Task.Run(() =>
            {
                TrainStations.Clear();

                SearchProgressBarVisible = true;
                LoadTrainStations(SearchText);
                SearchProgressBarVisible = false;
            });
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
            List<Task> tasks = [];

            for (int i = startIndex; i < startIndex + count && i < stationIdentifiers.Count; i++)
            {
                var identifier = stationIdentifiers.ElementAt(i);

                tasks.Add(Task.Run(() =>
                {
                    ActorObject stationIdentifier = identifier.Value;
                    ActorObject station = GetStationFromIdentifier(stationIdentifier);

                    string name = GetTrainStationName(stationIdentifier);
                    _log.Info($"Loading Train Station \"{name}\"...");
                    TrainStation trainStation = LoadTrainStation(station, name);
                    var trains = TrainHelper.GetTrainsByStop(identifier.Key);
                    CalculateTotalItems(trains, trainStation); // sets the items for the provided trains

                    TrainStations.Add(trainStation);

                    _log.Info($"Loaded Train Station \"{name}\" with {trainStation.TrainStationCount}T/{trainStation.DockingStations.Count}W!");
                }));
            }

            // wait for stations to load
            tasks.ForEach(o => o.Wait());

            _log.Info("Finished loading Train Stations!");
        }

        void LoadTrainStations(string nameFilter)
        {
            var stationIdentifiers = GetTrainStationIdentifiers();
            List<Task> tasks = [];

            foreach (var identifier in stationIdentifiers)
            {
                ActorObject stationIdentifier = identifier.Value;
                ActorObject station = GetStationFromIdentifier(stationIdentifier);

                string name = GetTrainStationName(stationIdentifier);
                if (!name.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase)) continue;

                tasks.Add(Task.Run(() =>
                {
                    _log.Info($"Loading Train Station \"{name}\"...");
                    TrainStation trainStation = LoadTrainStation(station, name);
                    var trains = TrainHelper.GetTrainsByStop(identifier.Key);
                    CalculateTotalItems(trains, trainStation); // sets the items for the provided trains

                    TrainStations.Add(trainStation);

                    _log.Info($"Loaded Train Station \"{name}\" with {trainStation.TrainStationCount}T/{trainStation.DockingStations.Count}W!");
                }));
            }

            // wait for stations to load
            tasks.ForEach(o => o.Wait());

            _log.Info($"Finished loading Train Stations matching the filter \"{nameFilter}\"!");
        }

        TrainStation LoadTrainStation(ActorObject station, string name)
        {
            bool flipped = false;

            // get first TrainStation of whole Train-/DockingStation complex
            if (LoadConnectedStation(station, StationConnection.Front, out string? pathName, ref flipped) is ActorObject tempStation && pathName != null && TrainHelper.IsTrainStation(pathName))
            {
                return LoadTrainStation(tempStation, name);
            }

            TrainStation trainStation = new()
            {
                Name = name,
                TrainStationCount = 1,
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

            return trainStation;
        }

        /// <summary>
        /// get's the connected station
        /// </summary>
        /// <param name="station">ActorOnject of station</param>
        /// <param name="connection">Front or Back</param>
        /// <returns></returns>
        ActorObject? LoadConnectedStation(ActorObject station, StationConnection connection, out string? pathName, ref bool flipped)
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
                        items = GetItemsByProducts(recipe.Products, recipe.Time, currentPotential?.Value ?? 1f);
                    }
                    else
                    {
                        items = GetItemsByProducts(recipe.Ingredients, recipe.Time, currentPotential?.Value ?? 1f);
                    }
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
        List<Item> GetItemsByProducts(Product[] products, float recipeTime, float currentPotential)
        {
            List<Item> items = [];

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
                        _log.Warn($"Icon for item \"{itemName}\" is missing!");
                    }

                    items.Add(new Item()
                    {
                        Icon = new(iconPath),
                        ItemPathName = product.Item,
                        Rate = prodRate,
                    });
                }
            }

            return items;
        }

        void CalculateTotalItems(List<Train> trains, TrainStation trainStation)
        {
            foreach (Train train in trains)
            {
                if (IsUnsupportedTrain(train)) continue;

                CalculateItemsByTrain(train, trainStation);
            }
        }

        void CalculateItemsByTrain(Train train, TrainStation trainStation)
        {
            if (train.TimeTable == null) return;

            foreach (TimeTableStop stop in train.TimeTable.Stops)
            {
                ActorObject? stationIdentifier = (ActorObject?)_saveFileReader.GetActCompObject(stop.StationIdentifierPathName);
                if (stationIdentifier == null) continue;

                string stationName = GetTrainStationName(stationIdentifier);
                ActorObject actorStation = GetStationFromIdentifier(stationIdentifier);
                TrainStation station = (trainStation.Name == stationName) ? trainStation : LoadTrainStation(actorStation, "");

                for (int i = 0; i < station.DockingStations.Count && i < train.FreightWagons.Count; i++)
                {
                    var dockingStation = station.DockingStations[i];
                    var freightWagon = train.FreightWagons[i];

                    foreach (var item in dockingStation.NeededItems)
                    {
                        if (stop.UnloadFilter.Count > 0 && !stop.UnloadFilter.Contains(item.ItemPathName)) continue;

                        if (freightWagon.Items.Find(o => o.ItemPathName == item.ItemPathName) is Item requestedItem)
                        {
                            // docking station
                            if (dockingStation.IncomingItems.Find(o => o.ItemPathName == item.ItemPathName) is Item dockingItem)
                                dockingItem.Rate += (requestedItem.Rate >= item.Rate) ? item.Rate : requestedItem.Rate;
                            else
                                dockingStation.IncomingItems.Add(item);
                        }

                        // freight wagon
                        if (freightWagon.Items.Find(o => o.ItemPathName == item.ItemPathName) is Item freightItem)
                            freightItem.Rate -= item.Rate;
                        else
                        {
                            freightWagon.Items.Add(item);
                            freightWagon.Items.Last().Rate = -item.Rate;
                        }
                        
                    }

                    foreach (var item in dockingStation.OutgoingItems)
                    {
                        if (stop.LoadFilter.Count > 0 && !stop.LoadFilter.Contains(item.ItemPathName)) continue;

                        // freight wagon
                        if (freightWagon.Items.Find(o => o.ItemPathName == item.ItemPathName) is Item foundItem)
                            foundItem.Rate += item.Rate;
                        else
                            freightWagon.Items.Add(item);

                        // i could add a neededItems property to DockingStations for exports in the future
                    }
                }
            }
        }

        List<ActorObject> GetConnectedBuildings(ActorObject building, List<string> buildingPathNames, PortType portType)
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

                ActorObject? connectedBuilding = GetConnectedBuildingFromConveyor(conveyorObject, buildingPathNames);
                if (connectedBuilding == null || connectedBuilding.ComponentCount == 0) continue;

                string connectedBuildingPathName = connectedBuilding.Components.First().PathName;
                connectedBuildingPathName = connectedBuildingPathName[..connectedBuildingPathName.LastIndexOf('.')];

                bool isConveyorAttachment = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_ConveyorAttachment");
                bool isConveyor = connectedBuildingPathName.Contains("Persistent_Level:PersistentLevel.Build_Conveyor"); // also true when ConveyorAttachment

                buildingPathNames.Add(connectedBuildingPathName);

                if (isConveyorAttachment)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conveyor">An ActorObject of a Conveyor Belt</param>
        /// <param name="filter">A pathName for the building to ignore</param>
        /// <returns>An ActorObject to the connected building, null when not connected or wrong ActorObject passed</returns>
        ActorObject? GetConnectedBuildingFromConveyor(ActorObject conveyor, List<string> filter)
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

                    if (!filter.Contains(pathName))
                    {
                        if (!isConveyorAttachment && isConveyor)
                        {
                            if (_saveFileReader.GetActCompObject(pathName) is not ActorObject newConveyor) return null;
                            string newFilter = item.PathName[..item.PathName.LastIndexOf('.')];
                            filter.Add(newFilter);

                            return GetConnectedBuildingFromConveyor(newConveyor, filter);
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
