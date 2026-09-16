using WXPlayer.Core;

namespace WXPlayer.App;

internal static class ArtworkService
{
    internal static ArtworkDiscovery Discovery {get;set;}=new(Path.Combine(App.DataDirectory,"artwork-discovery"));
    internal static bool Enabled {get;set;}=true;
    internal static async Task PopulateAsync(ContentItem item,CancellationToken ct=default)
    {
        if(!Enabled||ArtworkCache.HasAddress(item.Logo))return;
        try
        {
            var result=await Discovery.ResolveAsync(item,ct);
            if(result is not null&&Enabled&&!ct.IsCancellationRequested&&!ArtworkCache.HasAddress(item.Logo))
            {item.ArtworkCredit="Görsel: "+result.Provider+"\n"+result.PageUrl;item.Logo=result.Url;}
        }
        catch(OperationCanceledException){}
        catch { /* Metadata availability must never prevent browsing or playback. */ }
    }
}
