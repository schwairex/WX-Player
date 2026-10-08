using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class UpdateController(PlayerSettings settings,CancellationToken lifetime,GitHubUpdater? client=null) : IDisposable
{
    private readonly GitHubUpdater _updater=client??new();
    private readonly SemaphoreSlim _check=new(1,1);
    public static Version Current=>Assembly.GetExecutingAssembly().GetName().Version??new(1,2,0);
    public string Status {get;private set;}="Güncellemeler GitHub Releases üzerinden alınır.";
    public PreparedUpdate? Ready {get;private set;}
    public event Action? Changed;
    public event Action? Available;
    private void SetStatus(string text){Status=text;Changed?.Invoke();}
    public async Task RunAsync()
    {
        try
        {
            await Task.Delay(3000,lifetime);if(settings.AutoUpdate)await CheckAsync();
            using var timer=new PeriodicTimer(TimeSpan.FromHours(4));
            while(await timer.WaitForNextTickAsync(lifetime))if(settings.AutoUpdate)await CheckAsync();
        }catch(OperationCanceledException){}
    }
    public async Task CheckAsync(bool manual=false)
    {
        if(!await _check.WaitAsync(0))return;
        try
        {
            if(Ready is not null){SetStatus("Güncelleme hazır · v"+Ready.Version);if(manual)Available?.Invoke();return;}
            SetStatus("Yeni sürüm kontrol ediliyor…");
            var update=await _updater.CheckAsync(Current,lifetime,manual);
            if(update is null){SetStatus("Güncelsiniz · v"+Current.ToString(3)+" · "+DateTime.Now.ToString("HH:mm"));return;}
            SetStatus("v"+update.Version+" arka planda indiriliyor…");
            Ready=await _updater.DownloadAsync(update,App.DataDirectory,new Progress<int>(n=>{if(Ready is null)SetStatus($"v{update.Version} indiriliyor · %{n}");}),lifetime);
            SetStatus("v"+Ready.Version+" doğrulandı · Yeniden başlatmaya hazır");Available?.Invoke();
        }
        catch(OperationCanceledException){if(!lifetime.IsCancellationRequested)SetStatus("Güncelleme kontrolü zaman aşımına uğradı; daha sonra yeniden denenecek.");}
        catch(Exception ex){SetStatus(ex is InvalidOperationException?ex.Message:"Güncelleme sunucusuna ulaşılamadı. Mevcut sürümü kullanmaya devam edebilirsiniz.");}
        finally{_check.Release();}
    }
    public async Task RestartAsync()
    {
        var update=Ready??throw new InvalidOperationException("Henüz hazır bir güncelleme yok.");
        if(!UpdateActivation.IsManagedPath(App.DataDirectory,update.Path)||await GitHubUpdater.HashAsync(update.Path,lifetime)!=update.Sha256)throw new InvalidOperationException("Güncelleme yeniden doğrulanamadı.");
        var self=Process.GetCurrentProcess();
        var start=new ProcessStartInfo{FileName=update.Path,UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(update.Path)!};
        foreach(var arg in App.Arguments)start.ArgumentList.Add(arg);
        start.Environment["WXPLAYER_WAIT_PID"]=self.Id.ToString();start.Environment["WXPLAYER_WAIT_TICKS"]=self.StartTime.ToUniversalTime().Ticks.ToString();
        start.Environment["WXPLAYER_PENDING_VERSION"]=update.Version.ToString();start.Environment["WXPLAYER_PENDING_HASH"]=update.Sha256;start.Environment["WXPLAYER_PENDING_EXE"]=update.Path;
        _ = Process.Start(start)??throw new InvalidOperationException("Güncelleme başlatılamadı.");
    }
    public static async Task ActivatePendingAsync()
    {
        string? tag=Environment.GetEnvironmentVariable("WXPLAYER_PENDING_VERSION"),hash=Environment.GetEnvironmentVariable("WXPLAYER_PENDING_HASH"),path=Environment.GetEnvironmentVariable("WXPLAYER_PENDING_EXE");
        if(tag is null||hash is null||path is null)return;
        if(!GitHubUpdater.TryVersion(tag,out var version))throw new InvalidOperationException("Güncelleme sürümü geçerli değil.");
        await UpdateActivation.ActivateAsync(App.DataDirectory,new(version,path,hash),Current);
        foreach(var key in new[]{"WXPLAYER_PENDING_VERSION","WXPLAYER_PENDING_HASH","WXPLAYER_PENDING_EXE"})Environment.SetEnvironmentVariable(key,null);
    }
    public static bool Redirect(string[] args)
    {
        if(args.Contains("--smoke")||Environment.GetEnvironmentVariable("WXPLAYER_PENDING_VERSION") is not null)return false;
        if(UpdateActivation.Read(App.DataDirectory,Current) is not{} active)return false;
        var start=new ProcessStartInfo{FileName=active.Path,UseShellExecute=false};foreach(var arg in args)start.ArgumentList.Add(arg);Process.Start(start);return true;
    }
    public void Dispose()=>_updater.Dispose();
}

internal sealed class UpdateWindow : PremiumWindow
{
    internal UpdateWindow(Window owner,PreparedUpdate update,Func<Task> restart):base(owner,"Yeni bir sürüm mevcut","WX Player v"+update.Version+" indirildi ve doğrulandı.","refresh",580,360)
    {
        MinHeight=330;Height=360;ResizeMode=ResizeMode.NoResize;
        var panel=new StackPanel();Body.Children.Add(panel);panel.Children.Add(Text("Yeni bir sürüm mevcut, güncellemek için uygulamayı yeniden başlatın",18));
        var note=Text("Kaynaklarınız, favorileriniz ve ayarlarınız korunur.",12,"#98A3B6");note.Margin=new(0,16,0,0);panel.Children.Add(note);
        Footer.Children.Add(Action("Daha sonra",Close));
        var button=Action("Güncelle ve yeniden başlat",()=>{},true);button.Click+=async(_,_)=>{button.IsEnabled=false;try{await restart();}catch(InvalidOperationException ex){note.Text=ex.Message;}catch{note.Text="Yeniden başlatma tamamlanamadı. Daha sonra tekrar deneyin.";}finally{button.IsEnabled=true;}};Footer.Children.Add(button);
    }
}


