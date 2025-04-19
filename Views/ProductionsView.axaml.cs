using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FactoryPlanner.ViewModels;

namespace FactoryPlanner.Views;

public partial class ProductionsView : ReactiveUserControl<ProductionsViewModel>
{
    public ProductionsView()
    {
        InitializeComponent();
    }
}