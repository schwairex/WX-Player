using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using WXPlayer.Core;

namespace WXPlayer.App;

// Read-only display values. Raw labels, item identities and collection order stay intact.
internal static class LibraryDisplay
{
    private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
    private static readonly Regex YearSuffix=new(@"\s*\(((?:19|20)\d{2})\)\s*$");
    private static readonly Regex Notes=new(@"\[([^\]]+)\]");
    internal static string Title(string raw)
    {
        string text=YearSuffix.Replace(Notes.Replace(raw,""),"").Trim();
        if(text.Length==0)return raw;
        if(text.Any(char.IsLetter)&&text==text.ToUpper(Turkish))text=Turkish.TextInfo.ToTitleCase(text.ToLower(Turkish));
        return text;
    }
    internal static string Year(string raw)=>YearSuffix.Match(Notes.Replace(raw,"").Trim()).Groups[1].Value;
    internal static string Note(string raw)=>string.Join(" · ",Notes.Matches(raw).Cast<Match>().Select(m=>
    {string s=m.Groups[1].Value.Trim();return s.Length==0?s:char.ToUpper(s[0],Turkish)+s[1..].ToLower(Turkish);}));
    internal static string Group(string raw)
    {
        int split=raw.IndexOf('✨');if(split<0)return "";
        string prefix=raw[..split].Trim();return prefix switch{"TR"=>"TÜRKİYE","Poland"=>"POLONYA",_=>prefix.ToUpper(Turkish)};
    }
    internal static string Category(string raw){int split=raw.IndexOf('✨');return split<0?raw:raw[(split+1)..].Trim();}
    internal static string Elapsed(string raw)=>raw=="BAĞLANIYOR"?"Bağlanıyor…":raw.Split(" / ")[0];
    internal static string Total(string raw){var parts=raw.Split(" / ");return parts.Length==2?parts[1]:"";}
}

public sealed class LibraryTextConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
    {
        string raw=value?.ToString()??"";return parameter?.ToString() switch
        {"Title"=>LibraryDisplay.Title(raw),"Year"=>LibraryDisplay.Year(raw),"Note"=>LibraryDisplay.Note(raw),"Group"=>LibraryDisplay.Group(raw),"Category"=>LibraryDisplay.Category(raw),"Elapsed"=>LibraryDisplay.Elapsed(raw),"Total"=>LibraryDisplay.Total(raw),_=>raw};
    }
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class LibraryPlayingConverter : IMultiValueConverter
{
    public object Convert(object[] values,Type targetType,object parameter,CultureInfo culture)=>values.Length==2&&values[0] is ContentItem a&&values[1] is ContentItem b&&a.Equals(b);
    public object[] ConvertBack(object value,Type[] targetTypes,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public sealed class LibraryGroupHeaderConverter : IMultiValueConverter
{
    public object Convert(object[] values,Type targetType,object parameter,CultureInfo culture)
    {
        if(values.Length!=2||values[0] is not string raw||values[1] is not IEnumerable<string> names)return "";
        string group=LibraryDisplay.Group(raw),previous="";
        foreach(string name in names){if(name==raw)return group!=previous?group:"";previous=LibraryDisplay.Group(name);}
        return "";
    }
    public object[] ConvertBack(object value,Type[] targetTypes,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public static class LibraryPresentation
{
    public static readonly DependencyProperty CategoryDetailsVisibleProperty=DependencyProperty.RegisterAttached("CategoryDetailsVisible",typeof(bool),typeof(LibraryPresentation),new FrameworkPropertyMetadata(true,FrameworkPropertyMetadataOptions.Inherits));
    public static bool GetCategoryDetailsVisible(DependencyObject target)=>(bool)target.GetValue(CategoryDetailsVisibleProperty);
    public static void SetCategoryDetailsVisible(DependencyObject target,bool value)=>target.SetValue(CategoryDetailsVisibleProperty,value);
    public static readonly DependencyProperty PlayingItemProperty=DependencyProperty.RegisterAttached("PlayingItem",typeof(ContentItem),typeof(LibraryPresentation),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.Inherits));
    public static ContentItem? GetPlayingItem(DependencyObject target)=>(ContentItem?)target.GetValue(PlayingItemProperty);
    public static void SetPlayingItem(DependencyObject target,ContentItem? value)=>target.SetValue(PlayingItemProperty,value);
}
