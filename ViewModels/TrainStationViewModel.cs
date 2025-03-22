using DynamicData;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.ViewModels
{
    public class TrainStationViewModel : ViewModelBase
    {
        public List<TrainStation> TrainStations { get; set; } = [];

        private readonly SaveFileReader _saveFileReader;


        public TrainStationViewModel(IScreen screen) : base(screen)
        {
            _saveFileReader = SaveFileReader.LoadedSaveFile;

            LoadTrainStations(0, 5);
        }

        void LoadTrainStations(int startIndex, int count)
        {
            var stations = _saveFileReader.GetActCompObjects(TypePaths.TrainStation);
            for (int i = startIndex; i < startIndex + count && i < stations.Count; i++)
            {
                ActorObject station = (ActorObject)stations[i];
                TrainStations.Add(new TrainStation()
                {
                    DockingStations = LoadConnectedDockingStations(station)
                });
            }
        }

        List<DockingStation> LoadConnectedDockingStations(ActorObject station)
        {
            List<DockingStation> dockingStations = [];

            int componentIndex = 3;
            if (GetPropertyByName(station, "mIsOrientationReversed") != null)
            {
                componentIndex = 4;
            }

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
        DockingStation? LoadDockingStation(ActorObject station, string pathName)
        {
            PropertyListEntry? propertyEntry = GetPropertyByName(station, "mInventory");
            if (propertyEntry == null) return null; // ActorObjet is not a docking station

            List<ActorObject> buildings = GetConnectedBuildings(station, pathName, PortType.Input);
            float combinedProdRate = 0;
            foreach (ActorObject building in buildings)
            {
                if (building.Properties.Length >= 8 && building.Properties[7].Property is FloatProperty floatProperty)
                {
                    float prodRate = 60000 / (floatProperty.Value * 10);

                    combinedProdRate += float.IsInfinity(prodRate) ? 0 : prodRate;
                }
            }

            return new DockingStation()
            {
                ProductionRate = combinedProdRate
            };


            // disabled for now

            //ObjectProperty objProp = (ObjectProperty)propertyEntry.Property;
            //ComponentObject inventory = (ComponentObject)_saveFileReader.GetActCompObject(objProp.Reference.PathName);
            //ArrayProperty arr = (ArrayProperty)inventory.Properties[0].Property;
            //SimpleStructProperty str = (SimpleStructProperty)arr.Properties[0];

            //for (int i = 0; i < str.Data.Length; i += 2)
            //{
            //    PropertyListEntry invEntry = (PropertyListEntry)str.Data[i];
            //    PropertyListEntry amountEntry = (PropertyListEntry)str.Data[i + 1];

            //    StructProperty inv = (StructProperty)invEntry.Property;
            //    IntProperty amount = (IntProperty)amountEntry.Property;

            //    InventoryItem item = (InventoryItem)inv.Properties[0];

            //    //Debug.WriteLine($"{item.Reference.PathName} {amount.Value}");
            //}
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

        private static PropertyListEntry? GetPropertyByName(ActCompObject obj, string name)
        {
            foreach (var entry in obj.Properties)
            {
                if (entry.Name == name)
                    return entry;
            }

            return null;
        }


        private enum PortType
        {
            Input,
            Output
        }


    }
}
