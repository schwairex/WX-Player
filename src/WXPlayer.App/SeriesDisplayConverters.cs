using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class SeriesDisplay
{
    private static readonly Regex Numbered=new(@"\bS(?<s>[0-9]{1,3})[ ._\-]*E(?<e>[0-9]{1,4})\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static readonly Regex Named=new(@"(?:^|[\s\-·])(?<e>[0-9]{1,4})\.\s*Bölüm\s*$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    internal static string Title(ContentItem item)=>item.SeriesName.Length>0?LibraryDisplay.Title(item.SeriesName):item.Name;
    internal static string Code(ContentItem item)=>item.Episode>0?$"S{item.Season:00}E{item.Episode:00}":"";
    internal static string Caption(ContentItem item)=>item.Episode>0?item.EpisodeLabel:"";
    internal static string EpisodeTitle(ContentItem item)
    {
        if(item.Episode<=0)return item.Name;
        var match=Numbered.Match(item.Name);
        if(match.Success)
            return int.TryParse(match.Groups["s"].Value,NumberStyles.None,CultureInfo.InvariantCulture,out int season)&&int.TryParse(match.Groups["e"].Value,NumberStyles.None,CultureInfo.InvariantCulture,out int episode)&&season==item.Season&&episode==item.Episode?$"{item.Episode}. Bölüm":item.Name;
        match=Named.Match(item.Name);
        return match.Success&&int.TryParse(match.Groups["e"].Value,NumberStyles.None,CultureInfo.InvariantCulture,out int number)&&number==item.Episode?$"{item.Episode}. Bölüm":item.Name;
    }
    internal static string Summary(string rawState,int season,int count)=>rawState.StartsWith("Şimdi oynatılıyor:",StringComparison.Ordinal)?(season==0?"Özel bölümler":$"Sezon {season}")+$" · {count} bölüm":rawState;
}
public sealed class SeriesTextConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is ContentItem item?parameter?.ToString() switch{"Title"=>SeriesDisplay.Title(item),"Code"=>SeriesDisplay.Code(item),"Caption"=>SeriesDisplay.Caption(item),"EpisodeTitle"=>SeriesDisplay.EpisodeTitle(item),_=>item.Name}:"";
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class SeriesCardProgressConverter : IMultiValueConverter
{
    public object Convert(object[] values,Type targetType,object parameter,CultureInfo culture)=>values.Length==3&&values[0] is ContentItem item?values[1] is ContentItem playing&&item.Equals(playing)&&values[2] is double position?position:item.Progress?.Fraction??0:0d;
    public object[] ConvertBack(object value,Type[] targetTypes,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
