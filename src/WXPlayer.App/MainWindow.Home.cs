using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private HomeView _home=null!;
    private CancellationTokenSource? _homeLoad;
    private int _homeVersion;
    private bool SidebarExpanded=>_settings.SidebarExpanded??(ActualWidth>=1180&&ActualHeight>=780);
    private void InitializeHome()
    {
        _home=new HomeView(async item=>await SafeAsync(()=>OpenHomeItemAsync(item)),async item=>await SafeAsync(async()=>{await _store.FavoriteAsync(item.Id,!item.IsFavorite);await RefreshViewAsync();}),async section=>await SafeAsync(()=>BrowseSectionAsync(section)),()=>AddSource_Click(this,new RoutedEventArgs()),async()=>await SafeAsync(()=>BrowseSectionAsync(_current?.Kind switch{ContentKind.Movie=>"movie",ContentKind.Series or ContentKind.Episode=>"series",_=>"live"})));
        _home.Search.SetBinding(TextBox.TextProperty,new Binding("Text"){Source=SearchBox,Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});HomeHost.Content=_home;
    }
    private void ApplyPageLayout()
    {
        bool home=_section=="home"&&!_fullscreen;HomeHost.Visibility=home?Visibility.Visible:Visibility.Collapsed;ContentGrid.Visibility=home?Visibility.Collapsed:Visibility.Visible;
        _home?.NowPlaying(_current?.Name);
    }
    private async Task RefreshHomeAsync()
    {
        if(!_ready||_section!="home"||_fullscreen)return;
        _homeLoad?.Cancel();_homeLoad?.Dispose();var cts=_homeLoad=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);int version=++_homeVersion;
        string? source=SelectedSource?.Id;string name=SelectedSource?.Name??"";string search=SearchBox.Text.Trim();_home.Loading();
        try
        {
            var definitions=new[]{("Son izlenenler","recent",(ContentKind?)null,false,true),("Favorilerin","favorites",(ContentKind?)null,true,false),("Film gecesi","movie",(ContentKind?)ContentKind.Movie,false,false),("Bir sonraki dizin","series",(ContentKind?)ContentKind.Series,false,false),("Şimdi canlı","live",(ContentKind?)ContentKind.Live,false,false)};
            var pages=await Task.WhenAll(definitions.Select(d=>_store.QueryAsync(source,d.Item3,null,search,d.Item4,d.Item5,0,12,cts.Token)));
            if(cts.IsCancellationRequested||version!=_homeVersion||source!=SelectedSource?.Id||_section!="home"||_fullscreen)return;
            _home.Render(name.Length==0?null:name,definitions.Select((d,i)=>new HomeShelf(d.Item1,d.Item2,pages[i])).ToArray(),search);_home.NowPlaying(_current?.Name);
        }catch(OperationCanceledException){}catch(Exception) when(cts.IsCancellationRequested){}
    }
    private async Task BrowseSectionAsync(string section)
    {
        _section=section;_offset=0;_suppress=true;CategoryPicker.SelectedIndex=0;_suppress=false;SetNav();ApplyLayout();await RefreshViewAsync();
    }
    private async Task OpenHomeItemAsync(ContentItem item)
    {
        await BrowseSectionAsync(item.Kind switch{ContentKind.Movie=>"movie",ContentKind.Series or ContentKind.Episode=>"series",_=>"live"});await PlayItemAsync(item);
    }
    private void SidebarToggle_Click(object sender,RoutedEventArgs e)
    { _settings.SidebarExpanded=!SidebarExpanded;App.SaveSettings(_settings);if(!_fullscreen)ApplyLayout(); }
    internal HomeView SmokeHome=>_home;
    internal Task SmokeBrowseAsync(string section)=>BrowseSectionAsync(section);
    internal Task SmokeOpenHomeAsync(ContentItem item)=>OpenHomeItemAsync(item);
    internal void SmokeSidebar()=>SidebarToggle_Click(this,new RoutedEventArgs());
}
