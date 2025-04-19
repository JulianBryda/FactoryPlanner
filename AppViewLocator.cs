using FactoryPlanner.ViewModels;
using FactoryPlanner.Views;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner
{
    public class AppViewLocator : IViewLocator
    {
        public IViewFor? ResolveView<T>(T? viewModel, string? contract = null) => viewModel switch
        {
            ProductionsViewModel context => new ProductionsView { DataContext = context },
            DashboardViewModel context => new DashboardView { DataContext = context },
            TrainStationViewModel context => new TrainStationView { DataContext = context },
            TrainViewModel context => new TrainView { DataContext = context },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }
}
