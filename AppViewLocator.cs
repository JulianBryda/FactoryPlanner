﻿using FactoryPlanner.ViewModels;
using FactoryPlanner.Views;
using ReactiveUI;
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
            TrainStationViewModel context => new TrainStationView { DataContext = context },
            TrainViewModel context => new TrainView { DataContext = context },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }
}
