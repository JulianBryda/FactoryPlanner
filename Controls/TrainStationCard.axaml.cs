using Avalonia;
using Avalonia.Controls.Primitives;
using FactoryPlanner.Models;

namespace FactoryPlanner.Controls;

public class TrainStationCard : TemplatedControl
{

    public static readonly StyledProperty<TrainStation> TrainStationProperty =
        AvaloniaProperty.Register<TrainStationCard, TrainStation>(nameof(TrainStation));

    public TrainStation TrainStation
    {
        get => GetValue(TrainStationProperty);
        set => SetValue(TrainStationProperty, value);
    }
}