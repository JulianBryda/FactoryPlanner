using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;
using SkiaSharp;

namespace FactoryPlanner.Controls;

public class IconText : TemplatedControl
{
    public static readonly StyledProperty<Bitmap?> ImageSourceProperty =
        AvaloniaProperty.Register<IconText, Bitmap?>(nameof(ImageSource));

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<IconText, string>(nameof(Text));

    public static readonly StyledProperty<Thickness> ImageMarginProperty =
        AvaloniaProperty.Register<IconText, Thickness>(nameof(ImageMarginProperty));

    public Bitmap? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Thickness ImageMargin
    {
        get => GetValue(ImageMarginProperty);
        set => SetValue(ImageMarginProperty, value);
    }
}