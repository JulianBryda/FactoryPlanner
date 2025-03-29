using Avalonia.Platform;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.Models;
using FactoryPlanner.ViewModels;
using log4net;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FactoryPlanner.Helper;
using System.Threading.Tasks.Dataflow;
using System.Collections.Specialized;

namespace FactoryPlanner.ViewModels
{
    public class TrainViewModel : ViewModelBase
    {
        public List<Train> Trains { get; set; }

        private readonly SaveFileReader _saveFileReader;
        private readonly ILog _log = LogManager.GetLogger(typeof(TrainViewModel));


        public TrainViewModel(IScreen screen) : base(screen)
        {
            _saveFileReader = SaveFileReader.LoadedSaveFile;

            Trains = LoadTrains(0, 5);
        }

        private List<Train> LoadTrains(int startIndex, int count)
        {
            ActorObject subsystem = TrainHelper.GetRailroadSubsystem();
            PropertyListEntry trainEntry = SaveFileReader.GetPropertyByName(subsystem, "mTrains") ?? throw new NullReferenceException("Failed to get mTrains from Railroadsubsytem!");
            ArrayProperty trainProperty = (ArrayProperty?)trainEntry.Property ?? throw new NullReferenceException("Failed to get trainProperty!");

            List<Train> trains = [];
            for (int i = startIndex; i < startIndex + count && i < trainProperty.Properties.Length; i++)
            {
                SimpleObjectProperty trainReference = (SimpleObjectProperty)trainProperty.Properties[i];

                int trainIndex = _saveFileReader.GetIndexOf(trainReference.Value.PathName);
                Train? train = TrainHelper.GetTrain(trainIndex, 20);
                if (train == null) continue;

                train.Name = $"Locomotives: {train.Locomotives.Count} | FreightWagons: {train.FreightWagons.Count}";
                trains.Add(train);
            }

            return trains;
        }
    }
}