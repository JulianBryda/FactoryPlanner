using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI;
using System.Reactive;

namespace FactoryPlanner.Controls;

public class NavigationBar : TemplatedControl
{
    public static readonly StyledProperty<ReactiveCommand<string, Unit>> CommandProperty =
        AvaloniaProperty.Register<NavigationBar, ReactiveCommand<string, Unit>>(nameof(Command));

    public ReactiveCommand<string, Unit> Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}