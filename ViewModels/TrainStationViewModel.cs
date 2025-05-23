﻿using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.Helper;
using FactoryPlanner.Models;
using log4net;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

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

        private void LoadTrainStations(string nameFilter)
        {
            var stationIdentifiers = TrainStationHelper.GetTrainStationIdentifiers();

            foreach (var identifier in stationIdentifiers)
            {
                ActorObject stationIdentifier = identifier.Value;
                ActorObject station = TrainStationHelper.GetStationFromIdentifier(stationIdentifier);

                string name = TrainStationHelper.GetTrainStationName(stationIdentifier);
                if (!name.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase)) continue;

                _log.Info($"Loading Train Station \"{name}\"...");
                TrainStation trainStation = TrainStationHelper.GetTrainStation(station, name, identifier.Key);

                TrainStations.Add(trainStation);

                _log.Info($"Loaded Train Station \"{name}\" with {trainStation.TrainStationCount}T/{trainStation.DockingStations.Count}W!");
            }

            _log.Info($"Finished loading Train Stations matching the filter \"{nameFilter}\"!");
        }

    }
}
