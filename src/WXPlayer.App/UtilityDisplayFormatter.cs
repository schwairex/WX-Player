using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace WXPlayer.App;

// Presentation only: source metadata, track IDs and persisted fields are never changed.
internal static class UtilityDisplayFormatter
{
    internal static string State(string value) => value switch
    {
        "Playing" => "Oynuyor", "Paused" => "Duraklatıldı", "Buffering" => "Arabelleğe alınıyor",
        "Opening" => "Açılıyor", "Stopped" => "Durduruldu", "Ended" => "Bitti",
        "Error" => "Hata", "NothingSpecial" => "Boşta", _ => value
    };
    internal static string Output(string value) => value switch
    { "direct3d11" => "Direct3D 11", "direct3d9" => "Direct3D 9", "any" => "Otomatik", _ => value };

    internal static (string Title, string Quality) MiniTitle(string raw)
    {
        // Only recognized country prefixes and a trailing quality token are safe to strip.
        string title = Regex.Replace(raw, @"^(?:TR|DE|UK)\s*[•:\-]\s*", "", RegexOptions.IgnoreCase).Trim();
        var match = Regex.Match(title, @"(?:\s+|^)(UHD|4K|FHD|HD|SD)\s*$", RegexOptions.IgnoreCase);
        string quality = match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
        if (match.Success) title = title[..match.Index].Trim();
        if (title.Length == 0) return (raw, "");
        return (title, quality == "UHD" ? "4K" : quality);
    }
    internal static (string Name, string Track) TrackLabel(string raw)
    {
        var m = Regex.Match(raw, @"^Track\s+(\d+)\s*-\s*\[(.+)\]$", RegexOptions.IgnoreCase);
        if (!m.Success) return (raw, "");
        string? name = m.Groups[2].Value.ToLowerInvariant() switch
        {
            "turkish" => "Türkçe", "english" => "İngilizce", "german" => "Almanca", "french" => "Fransızca",
            "spanish" => "İspanyolca", "italian" => "İtalyanca", "arabic" => "Arapça", "russian" => "Rusça",
            "persian" => "Farsça", "japanese" => "Japonca", _ => null
        };
        return name is null ? (raw, "") : (name, "Parça " + m.Groups[1].Value);
    }
}

public sealed class TrackLabelDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var display = UtilityDisplayFormatter.TrackLabel(value?.ToString() ?? "");
        return parameter?.ToString() == "track" ? display.Track : display.Name;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
