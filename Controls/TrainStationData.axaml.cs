using Avalonia;
using Avalonia.Controls.Primitives;
using ReactiveUI;
using System.Reactive;

namespace FactoryPlanner.Controls;

public class TrainStationData : TemplatedControl
{
    public static readonly StyledProperty<string> InTextProperty =
        AvaloniaProperty.Register<NavigationBar, string>(nameof(InText));
    public static readonly StyledProperty<string> OutTextProperty =
        AvaloniaProperty.Register<NavigationBar, string>(nameof(OutText));

    public string InText { get; set; } = string.Empty;
    public string OutText { get; set; } = string.Empty;
}