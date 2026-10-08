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
    private int _homeShuffleSeed = Random.Shared.Next();
    private bool _homeVisible;
    private (string? Source,string Search)? _homeContext;
    private readonly Dictionary<string,string> _recommendations=new();
    private bool SidebarExpanded=>_settings.SidebarExpanded??false;
    private void InitializeHome()
    {
        _home=new HomeView(async item=>await SafeAsync(()=>OpenHomeItemAsync(item)),async item=>await SafeAsync(()=>ToggleFavoriteAsync(item)),async section=>await SafeAsync(()=>BrowseSectionAsync(section)),()=>AddSource_Click(this,new RoutedEventArgs()),async()=>await SafeAsync(()=>BrowseSectionAsync(_current?.Kind switch{ContentKind.Movie=>"movie",ContentKind.Series or ContentKind.Episode=>"series",_=>"live"})),item=>SafeAsync(async()=>
        {
            await _store.RemoveRecentAsync(item.Id);
            await RefreshHomeAsync();
            Status("İçerik son izlenenlerden kaldırıldı.");
        }));
        _home.Search.SetBinding(TextBox.TextProperty,new Binding("Text"){Source=SearchBox,Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});HomeHost.Content=_home;
    }
    private void ApplyPageLayout()
    {
        bool home=_section=="home"&&!_fullscreen;
        bool catalog=CatalogVisible;
        HomeHost.Visibility=home?Visibility.Visible:Visibility.Collapsed;
        CatalogHost.Visibility=catalog?Visibility.Visible:Visibility.Collapsed;
        SetCatalogAppearance();
        ContentGrid.Visibility=home||catalog?Visibility.Collapsed:Visibility.Visible;
        CatalogBackButton.Visibility=CatalogSection&&_catalogPlaying&&!_fullscreen?Visibility.Visible:Visibility.Collapsed;
        if(home&&!_homeVisible)_homeShuffleSeed=Random.Shared.Next();
        _homeVisible=home;
        _home?.NowPlaying(_current?.Name);
        _catalog?.NowPlaying(_current?.Name);
    }
    private async Task RefreshHomeAsync()
    {
        if(!_ready||_section!="home"||_fullscreen)return;
        _homeLoad?.Cancel();_homeLoad?.Dispose();var cts=_homeLoad=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);int version=++_homeVersion;
        string? source=SelectedSource?.Id;string name=SelectedSource?.Name??"";string search=SearchBox.Text.Trim();var context=(source,search);_home.Loading(_homeContext!=context);_homeContext=context;
        try
        {
            await SavePlaybackProgressAsync();
            var definitions=new[]{("Son izlenenler","recent",(ContentKind?)null,false,true),("Favorilerin","favorites",(ContentKind?)null,true,false),("Filmler","movie",(ContentKind?)ContentKind.Movie,false,false),("Diziler","series",(ContentKind?)ContentKind.Series,false,false),("Şimdi canlı","live",(ContentKind?)ContentKind.Live,false,false)};
            bool Discovery(string section)=>section is "movie" or "series";
            int seed=_homeShuffleSeed;
            var pages=await Task.WhenAll(definitions.Select(d=>_store.QueryAsync(source,d.Item3,null,search,d.Item4,d.Item5,0,Discovery(d.Item2)?72:24,cts.Token,artworkOnly:true,shuffleSeed:Discovery(d.Item2)?seed:null)));
            if(ArtworkService.Enabled)
            {
                var all=await Task.WhenAll(definitions.Select(d=>_store.QueryAsync(source,d.Item3,null,search,d.Item4,d.Item5,0,Discovery(d.Item2)?72:24,cts.Token,shuffleSeed:Discovery(d.Item2)?seed:null)));
                pages=pages.Select((p,i)=>new WXPlayer.Core.Page(p.Items.Take(Discovery(definitions[i].Item2)?48:12).Concat(all[i].Items).Concat(p.Items).DistinctBy(x=>x.Id).Take(Discovery(definitions[i].Item2)?96:36).ToArray(),all[i].Total)).ToArray();
            }
            if(cts.IsCancellationRequested||version!=_homeVersion||source!=SelectedSource?.Id||_section!="home"||_fullscreen)return;
            ContentItem? recommended=null;
            if(search.Length==0&&source is not null){if(_recommendations.TryGetValue(source,out var id))recommended=await _store.FindAsync(id,cts.Token);if(recommended is not null&&!ArtworkService.Enabled&&!ArtworkCache.HasAddress(recommended.Logo))recommended=null;if(recommended is null){recommended=await _store.RecommendationAsync(source,_settings.LastRecommendation.GetValueOrDefault(source),cts.Token,artworkOnly:!ArtworkService.Enabled);if(recommended is not null&&!cts.IsCancellationRequested){_recommendations[source]=recommended.Id;_settings.LastRecommendation[source]=recommended.Id;App.SaveSettings(_settings);}}}
            if(cts.IsCancellationRequested||version!=_homeVersion||source!=SelectedSource?.Id)return;
            await _home.RenderAsync(name.Length==0?null:name,definitions.Select((d,i)=>new HomeShelf(d.Item1,d.Item2,pages[i])).ToArray(),search,recommended);_home.NowPlaying(_current?.Name);
        }catch(OperationCanceledException){}catch(Exception) when(cts.IsCancellationRequested){}catch{if(version==_homeVersion)_home.Error(()=>_ = SafeAsync(RefreshHomeAsync));throw;}
    }
    private async Task BrowseSectionAsync(string section)
    {
        _section=section;_catalogPlaying=false;_offset=0;_suppress=true;CategoryPicker.SelectedIndex=0;_suppress=false;SetNav();ApplyLayout();await RefreshViewAsync();
    }
    private async Task OpenHomeItemAsync(ContentItem item)
    {
        await BrowseSectionAsync(item.Kind switch{ContentKind.Movie=>"movie",ContentKind.Series or ContentKind.Episode=>"series",_=>"live"});
        if(CatalogSection){_catalogPlaying=true;ApplyPageLayout();}
        await PlayItemAsync(item);
    }
    private void SidebarToggle_Click(object sender,RoutedEventArgs e)
    { _settings.SidebarExpanded=!SidebarExpanded;App.SaveSettings(_settings);if(!_fullscreen)ApplyLayout(); }
    internal HomeView SmokeHome=>_home;
    internal Task SmokeBrowseAsync(string section)=>BrowseSectionAsync(section);
    internal Task SmokeOpenHomeAsync(ContentItem item)=>OpenHomeItemAsync(item);
    internal void SmokeSidebar()=>SidebarToggle_Click(this,new RoutedEventArgs());
}



