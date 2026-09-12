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
    private static readonly HttpClient Client=new(){Timeout=TimeSpan.FromSeconds(7)};
    private static readonly SemaphoreSlim Slots=new(6);
    private static readonly ConcurrentDictionary<string,Task<BitmapSource?>> Cache=new();
    private int _version;
    public ChannelLogo(){Children.Add(_letters);Children.Add(_image);Loaded+=(_,_)=>Refresh();Unloaded+=(_,_)=>_version++;}
    private static void Changed(DependencyObject obj,DependencyPropertyChangedEventArgs args){var logo=(ChannelLogo)obj;logo._letters.Text=logo.Initials;if(logo.IsLoaded)logo.Refresh();}
    private async void Refresh()
    {
        int version=++_version;_image.Source=null;_letters.Visibility=Visibility.Visible;string url=Url;
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme is not ("http" or "https"))return;
        if(Cache.Count>96)Cache.Clear();int width=Math.Clamp(DecodeWidth,96,960);
        var bitmap=await Cache.GetOrAdd(width+"|"+url,_=>Download(uri,width));
        if(version!=_version||!IsLoaded)return;_image.Source=bitmap;_letters.Visibility=bitmap is null?Visibility.Visible:Visibility.Collapsed;
    }
    private static async Task<BitmapSource?> Download(Uri uri,int width)
    {
        await Slots.WaitAsync();
        try
        {
            using var response=await Client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead);response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>2*1024*1024)return null;
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(7));await using var input=await response.Content.ReadAsStreamAsync(timeout.Token);using var data=new MemoryStream();byte[] bytes=new byte[16384];int count;
            while((count=await input.ReadAsync(bytes,timeout.Token))>0){if(data.Length+count>2*1024*1024)return null;data.Write(bytes,0,count);}
            return await Task.Run(()=>{data.Position=0;var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=width;bitmap.StreamSource=data;bitmap.EndInit();bitmap.Freeze();return (BitmapSource)bitmap;});
        }catch{return null;}finally{Slots.Release();}
    }
    internal bool HasImage=>_image.Source is not null;
}
