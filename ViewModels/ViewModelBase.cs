using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using System;

namespace FactoryPlanner.ViewModels
{
    public class ViewModelBase : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];

        public IScreen HostScreen { get; }

        public ViewModelBase(IScreen screen) => HostScreen = screen;
    }
}
