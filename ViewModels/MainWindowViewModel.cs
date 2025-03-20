using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Text.Json.Nodes;
using System.Threading;

namespace FactoryPlanner.ViewModels
{
    public partial class MainWindowViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
        public ReactiveCommand<string, Unit> Navigate { get; }


        public MainWindowViewModel()
        {
            Navigate = ReactiveCommand.Create<string>(NavigateToView);

            // preload SaveFile 
            _ = new SaveFileReader();
        }

        private void NavigateToView(string viewName)
        {
            switch (viewName)
            {
                case "TrainStation":
                    Router.Navigate.Execute(new TrainStationViewModel(this));
                    break;
                default:
                    throw new KeyNotFoundException($"No view found with name \"{viewName}\"!");
            }
        }

    }
}
