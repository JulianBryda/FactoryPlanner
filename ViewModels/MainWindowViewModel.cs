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

        public ReactiveCommand<Unit, IRoutableViewModel> GoNext { get; }

        public ReactiveCommand<Unit, IRoutableViewModel> GoBack => Router.NavigateBack;


        public MainWindowViewModel()
        {
            GoNext = ReactiveCommand.CreateFromObservable(
                       () => Router.Navigate.Execute(new TrainStationViewModel(this))
                   );
        }

    }
}
