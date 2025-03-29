using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FactoryPlanner.ViewModels;

namespace FactoryPlanner.Views;

public partial class TrainView : ReactiveUserControl<TrainViewModel>
{
    public TrainView()
    {
        InitializeComponent();
    }
}