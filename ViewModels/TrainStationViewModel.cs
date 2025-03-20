using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.ViewModels
{
    public class TrainStationViewModel : ViewModelBase
    {
        public List<TrainStation> TrainStations { get; set; }

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
                CheckConnections(station);
            }
        }

        void LoadDockingStation(string pathName)
        {
            ActorObject? station = (ActorObject?)_saveFileReader.GetActCompObject(pathName);
            if (station == null) throw new ArgumentNullException("Station is null!");

            PropertyListEntry? propertyEntry = GetPropertyByName(station, "mInventory");
            if (propertyEntry == null) return;

            ObjectProperty objProp = (ObjectProperty)propertyEntry.Property;
            ComponentObject inventory = (ComponentObject)_saveFileReader.GetActCompObject(objProp.Reference.PathName);
            ArrayProperty arr = (ArrayProperty)inventory.Properties[0].Property;
            SimpleStructProperty str = (SimpleStructProperty)arr.Properties[0];

            for (int i = 0; i < str.Data.Length; i += 2)
            {
                PropertyListEntry invEntry = (PropertyListEntry)str.Data[i];
                PropertyListEntry amountEntry = (PropertyListEntry)str.Data[i + 1];

                StructProperty inv = (StructProperty)invEntry.Property;
                IntProperty amount = (IntProperty)amountEntry.Property;

                InventoryItem item = (InventoryItem)inv.Properties[0];

                //Debug.WriteLine($"{item.Reference.PathName} {amount.Value}");
            }

            // check for further connected stations
            CheckConnections(station);
        }

        private readonly List<string> _processedStations = [];
        void CheckConnections(ActorObject station)
        {
            foreach (var comp in station.Components)
            {
                ComponentObject? compObject = (ComponentObject?)_saveFileReader.GetActCompObject(comp.PathName);
                if (compObject == null) continue;

                PropertyListEntry? connectedToEntry = compObject.Properties.FirstOrDefault(o => o.Name == "mConnectedTo");
                if (connectedToEntry == null) continue;

                ObjectProperty connectedTo = (ObjectProperty)connectedToEntry.Property;
                string pathName = connectedTo.Reference.PathName;

                if (!_processedStations.Contains(pathName))
                {
                    _processedStations.Add(pathName);
                    LoadDockingStation(pathName[..pathName.LastIndexOf('.')]);
                }
            }
        }

        private PropertyListEntry? GetPropertyByName(ActorObject obj, string name)
        {
            foreach (var entry in obj.Properties)
            {
                if (entry.Name == name)
                    return entry;
            }

            return null;
        }


    }
}
