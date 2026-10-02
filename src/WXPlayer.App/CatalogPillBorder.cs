using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WXPlayer.App;

/// <summary>CSS border-radius:999px clamps to half the height, rather than WPF's elliptical normalization.</summary>
public sealed class CatalogPillBorder : Border
{
    public static readonly DependencyProperty TransitionSecondsProperty = DependencyProperty.RegisterAttached(
        "TransitionSeconds", typeof(double), typeof(CatalogPillBorder), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.Inherits));
    public static void SetTransitionSeconds(DependencyObject target, double value) => target.SetValue(TransitionSecondsProperty, value);
    public static double GetTransitionSeconds(DependencyObject target) => (double)target.GetValue(TransitionSecondsProperty);
    private bool _changingBrush;
    public CatalogPillBorder() => SizeChanged += (_, _) => CornerRadius = new CornerRadius(ActualHeight / 2);
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        double seconds = GetTransitionSeconds(this);
        if (e.Property != BackgroundProperty || _changingBrush || !IsLoaded || !SystemParameters.ClientAreaAnimation || seconds <= 0 ||
            e.OldValue is not SolidColorBrush oldBrush || e.NewValue is not SolidColorBrush newBrush || oldBrush.Color == newBrush.Color) return;
        var brush = new SolidColorBrush(newBrush.Color);
        _changingBrush = true;
        try { SetCurrentValue(BackgroundProperty, brush); }
        finally { _changingBrush = false; }
        brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(oldBrush.Color, newBrush.Color, TimeSpan.FromSeconds(seconds)));
    }
}

public sealed class CatalogFavoriteIcon : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Equals(value, true) ? "catalog-star-filled" : "catalog-star";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
