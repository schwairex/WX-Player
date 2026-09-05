using System.IO;
using System.Text.Json;
using System.Windows;

namespace WXPlayer.App;

public partial class App : Application
{
    public static string DataDirectory {get;private set;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WXPlayer");
    public static string[] Arguments {get;private set;}=[];
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);Arguments=e.Args;int index=Array.IndexOf(e.Args,"--data-dir");if(index>=0&&index+1<e.Args.Length)DataDirectory=Path.GetFullPath(e.Args[index+1]);
        Directory.CreateDirectory(DataDirectory);
        DispatcherUnhandledException+=(_,args)=>{File.AppendAllText(Path.Combine(DataDirectory,"errors.log"),$"{DateTime.UtcNow:O} {args.Exception.GetType().Name}\n");MessageBox.Show("İşlem tamamlanamadı. Kaynağı veya dosya erişimini kontrol ederek yeniden deneyin.","WX Player");args.Handled=true;};
        try {var window=new MainWindow();MainWindow=window;window.Show();}
        catch(Exception ex){File.WriteAllText(Path.Combine(DataDirectory,"startup-error.log"),ex.ToString());MessageBox.Show("WX Player başlatılamadı. Uygulama paketini yeniden çıkarın. Ayrıntı: "+ex.GetType().Name,"WX Player");Shutdown(1);}
    }
    public static Core.PlayerSettings ReadSettings()
    {
        try{var s=JsonSerializer.Deserialize<Core.PlayerSettings>(File.ReadAllText(Path.Combine(DataDirectory,"settings.json")))??new();s.NetworkCacheMs=Math.Clamp(s.NetworkCacheMs,200,10000);s.Volume=Math.Clamp(s.Volume,0,100);if(s.VideoOutput is not ("direct3d11" or "direct3d9" or "any"))s.VideoOutput="direct3d11";return s;}catch{return new();}
    }
    public static void SaveSettings(Core.PlayerSettings settings)
    {string path=Path.Combine(DataDirectory,"settings.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));File.Move(path+".tmp",path,true);}
}
