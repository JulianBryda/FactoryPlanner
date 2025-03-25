using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using FactoryPlanner.Controls;
using FactoryPlanner.Models;

namespace FactoryPlanner.Controls;

public class TrainStationCard : TemplatedControl
{

    public static readonly StyledProperty<TrainStation> TrainStationProperty =
        AvaloniaProperty.Register<IconText, TrainStation>(nameof(TrainStation));

    public TrainStation TrainStation
    {
        get => GetValue(TrainStationProperty);
        set => SetValue(TrainStationProperty, value);
    }
}