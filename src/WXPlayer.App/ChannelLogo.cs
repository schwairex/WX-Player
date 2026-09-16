using System.Collections.Concurrent;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WXPlayer.App;

public sealed class ChannelLogo : Grid
{
    public static readonly DependencyProperty UrlProperty=DependencyProperty.Register(nameof(Url),typeof(string),typeof(ChannelLogo),new PropertyMetadata("",Changed));
    public static readonly DependencyProperty InitialsProperty=DependencyProperty.Register(nameof(Initials),typeof(string),typeof(ChannelLogo),new PropertyMetadata("",Changed));
    public string Url {get=>(string)GetValue(UrlProperty);set=>SetValue(UrlProperty,value);}
    public string Initials {get=>(string)GetValue(InitialsProperty);set=>SetValue(InitialsProperty,value);}
    public static readonly DependencyProperty DecodeWidthProperty=DependencyProperty.Register(nameof(DecodeWidth),typeof(int),typeof(ChannelLogo),new PropertyMetadata(96,Changed));
    public int DecodeWidth {get=>(int)GetValue(DecodeWidthProperty);set=>SetValue(DecodeWidthProperty,value);}
    public Stretch ImageStretch {get=>_image.Stretch;set=>_image.Stretch=value;}
    public Thickness ImagePadding {get=>_image.Margin;set=>_image.Margin=value;}
    private readonly Image _image=new(){Stretch=Stretch.Uniform,Margin=new Thickness(4)};
    private readonly TextBlock _letters=new(){HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,FontWeight=FontWeights.SemiBold,FontSize=13,Foreground=Brushes.LightSteelBlue};
    private int _version;
    public ChannelLogo(){Children.Add(_letters);Children.Add(_image);Loaded+=(_,_)=>Refresh();Unloaded+=(_,_)=>_version++;}
    private static void Changed(DependencyObject obj,DependencyPropertyChangedEventArgs args){var logo=(ChannelLogo)obj;logo._letters.Text=logo.Initials;if(logo.IsLoaded)logo.Refresh();}
    private async void Refresh()
    {
        int version=++_version;_image.Source=null;_letters.Visibility=Visibility.Visible;
        if(!ArtworkCache.HasAddress(Url)&&DataContext is WXPlayer.Core.ContentItem item)
        {await ArtworkService.PopulateAsync(item);if(version!=_version||!IsLoaded)return;}
        string url=Url;
        if(DataContext is WXPlayer.Core.ContentItem credit&&credit.ArtworkCredit.Length>0)ToolTip=credit.ArtworkCredit;
        var bitmap=await ArtworkCache.GetAsync(url,DecodeWidth);
        if(version!=_version||!IsLoaded)return;_image.Source=bitmap;_letters.Visibility=bitmap is null?Visibility.Visible:Visibility.Collapsed;
    }
    internal bool HasImage=>_image.Source is not null;
}

