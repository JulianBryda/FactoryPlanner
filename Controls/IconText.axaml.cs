using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;
using SkiaSharp;
using Avalonia.Controls;

namespace FactoryPlanner.Controls;

public class IconText : ContentControl
{
    public static readonly StyledProperty<Bitmap?> ImageSourceProperty =
        AvaloniaProperty.Register<IconText, Bitmap?>(nameof(ImageSource));

    public static readonly StyledProperty<Thickness> ImageMarginProperty =
        AvaloniaProperty.Register<IconText, Thickness>(nameof(ImageMargin));

    public Bitmap? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public Thickness ImageMargin
    {
        get => GetValue(ImageMarginProperty);
        set => SetValue(ImageMarginProperty, value);
    }
}