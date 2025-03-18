using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FactoryPlanner.ViewModels;
using ReactiveUI;

namespace FactoryPlanner;

public partial class TrainStationView : ReactiveUserControl<TrainStationViewModel>
{
    public TrainStationView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}