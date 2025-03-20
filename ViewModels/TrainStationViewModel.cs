using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
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

            LoadTrainStation();
        }

        void LoadTrainStation()
        {
            var stations = _saveFileReader.GetActCompObjects(TypePaths.TrainDockingStation);
            foreach (ActorObject station in stations.Cast<ActorObject>())
            {
                var prop = GetPropertyByName(station, "mInventory");
                if (prop == null) continue;

                ObjectProperty objProp = (ObjectProperty)prop.Property;
                var inventory = _saveFileReader.GetActCompObject(objProp.Reference.PathName);
                string tesT = "";
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
