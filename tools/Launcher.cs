// .NET Framework 4.x bootstrapper. The payload contains the self-contained .NET 10 app.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class Launcher
{
    [STAThread]
    private static void Main(string[] args)
    {
        if(TryRedirect(args))return;
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        var form=new Form {Text="WX Player",Width=430,Height=170,StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(12,15,21),ForeColor=Color.White};
        var title=new Label {Text="WX Player",Font=new Font("Segoe UI",23,FontStyle.Bold),Left=24,Top=18,AutoSize=true,ForeColor=Color.FromArgb(196,245,113)};
        var status=new Label {Text="İlk açılış hazırlanıyor…",Font=new Font("Segoe UI",10),Left=26,Top=70,Width=370};form.Controls.Add(title);form.Controls.Add(status);
        bool completed=false;form.FormClosing+=(s,e)=>{if(!completed)e.Cancel=true;};
        form.Shown+=async(s,e)=>
        {
            try
            {
                await Task.Run((Action)WaitForPrevious);
                string executable=await Task.Run(()=>Prepare(text=>form.BeginInvoke((Action)(()=>status.Text=text))));
                var start=new ProcessStartInfo {FileName=executable,WorkingDirectory=Path.GetDirectoryName(executable),UseShellExecute=false,Arguments=string.Join(" ",args.Select(Quote))};
                // Inherit the environment directly; avoid Framework case-duplicate PATH entries.
                using(var process=Process.Start(start))
                {
                    form.Hide();if(process!=null)await Task.Run(()=>process.WaitForExit());
                }
            }
            catch(Exception ex){if(args.Contains("--smoke")){int at=Array.IndexOf(args,"--data-dir");if(at>=0&&at+1<args.Length){Directory.CreateDirectory(args[at+1]);File.WriteAllText(Path.Combine(args[at+1],"launcher-error.log"),ex.ToString());}Environment.ExitCode=1;}else MessageBox.Show("WX Player açılamadı. Disk alanını ve klasör izinlerini kontrol edin.\n\n"+ex.Message,"WX Player",MessageBoxButtons.OK,MessageBoxIcon.Error);Environment.ExitCode=1;}
            finally{completed=true;form.Close();}
        };
        Application.Run(form);
    }
    private static string Prepare(Action<string> progress)
    {
        string overrideRoot=Environment.GetEnvironmentVariable("WXPLAYER_APP_ROOT");
        string root=Path.GetFullPath(string.IsNullOrEmpty(overrideRoot)?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WXPlayer","application"):overrideRoot);
        Directory.CreateDirectory(root);
        string destination=Path.Combine(root,PayloadInfo.Version+"-"+PayloadInfo.Sha256.Substring(0,12));
        string exe=Path.Combine(destination,"WXPlayer.exe");
        string marker=Path.Combine(destination,".complete");
        string mutexId;using(var hash=SHA256.Create())mutexId=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(root))).Replace("-","").Substring(0,24);
        using(var mutex=new Mutex(false,"Local\\WXPlayer.Extract."+mutexId))
        {
            bool locked=false;
            try
            {
                try{locked=mutex.WaitOne(TimeSpan.FromMinutes(5));}catch(AbandonedMutexException){locked=true;}
                if(!locked)throw new IOException("Başka bir WX Player açılışı tamamlanamadı.");
                if(File.Exists(exe)&&File.Exists(marker)&&File.ReadAllText(marker)==PayloadInfo.Sha256)return exe;
                string stage=Path.Combine(root,".extract-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
                try
                {
                    using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("WXPlayer.Payload.zip"))
                    {
                        if(resource==null)throw new IOException("Uygulama paketi bulunamadı.");
                        using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(resource)).Replace("-","").ToLowerInvariant()!=PayloadInfo.Sha256)throw new IOException("Paket doğrulaması başarısız.");
                        resource.Position=0;
                        using(var zip=new ZipArchive(resource,ZipArchiveMode.Read))
                        {
                            int done=0;foreach(var entry in zip.Entries)
                            {
                                string target=Path.GetFullPath(Path.Combine(stage,entry.FullName));
                                if(!target.StartsWith(stage+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Geçersiz paket yolu.");
                                if(string.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(target);continue;}
                                Directory.CreateDirectory(Path.GetDirectoryName(target));using(var input=entry.Open())using(var output=File.Create(target))input.CopyTo(output);
                                if(++done%30==0)progress("Bileşenler hazırlanıyor… %"+(done*100/zip.Entries.Count));
                            }
                        }
                    }
                    File.WriteAllText(Path.Combine(stage,".complete"),PayloadInfo.Sha256);
                    if(Directory.Exists(destination))
                    {
                        // Only a failed previous extraction of this exact embedded version is replaced.
                        string resolved=Path.GetFullPath(destination);
                        if(!resolved.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Geçersiz hedef.");
                        Directory.Delete(resolved,true);
                    }
                    Directory.Move(stage,destination);return exe;
                }
                finally{if(Directory.Exists(stage)&&Path.GetFullPath(stage).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(stage,true);}
            }
            finally{if(locked)mutex.ReleaseMutex();}
        }
    }
    private static void WaitForPrevious()
    {
        int pid;long ticks;
        if(int.TryParse(Environment.GetEnvironmentVariable("WXPLAYER_WAIT_PID"),out pid)&&long.TryParse(Environment.GetEnvironmentVariable("WXPLAYER_WAIT_TICKS"),out ticks))
        {
            try{using(var previous=Process.GetProcessById(pid)){if(previous.StartTime.ToUniversalTime().Ticks==ticks&&!previous.WaitForExit(120000))throw new IOException("Önceki WX Player kapanmadı. Güncelleme durduruldu.");}}
            catch(ArgumentException){}
        }
        Environment.SetEnvironmentVariable("WXPLAYER_WAIT_PID",null);Environment.SetEnvironmentVariable("WXPLAYER_WAIT_TICKS",null);
    }
    private static bool TryRedirect(string[] args)
    {
        if(args.Contains("--smoke")||Environment.GetEnvironmentVariable("WXPLAYER_PENDING_VERSION")!=null)return false;
        try
        {
            string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WXPlayer");int index=Array.IndexOf(args,"--data-dir");if(index>=0&&index+1<args.Length)data=Path.GetFullPath(args[index+1]);
            string pointer=Path.Combine(data,"active-update.txt");if(!File.Exists(pointer))return false;
            string[] lines=File.ReadAllLines(pointer);Version version;
            if(lines.Length!=3||!Version.TryParse(lines[0],out version)||version<=new Version(PayloadInfo.Version))return false;
            string file=Path.GetFullPath(lines[2]),root=Path.GetFullPath(Path.Combine(data,"updates"))+Path.DirectorySeparatorChar;
            if(!file.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!file.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||file==Assembly.GetExecutingAssembly().Location)return false;
            using(var stream=File.OpenRead(file))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=lines[1])return false;
            Process.Start(new ProcessStartInfo{FileName=file,UseShellExecute=false,Arguments=string.Join(" ",args.Select(Quote))});return true;
        }
        catch{return false;}
    }
    private static string Quote(string argument)
    {
        var b=new StringBuilder("\"");int slashes=0;
        foreach(char c in argument){if(c=='\\'){slashes++;continue;}if(c=='\"'){b.Append('\\',slashes*2+1);b.Append(c);slashes=0;continue;}b.Append('\\',slashes);slashes=0;b.Append(c);}b.Append('\\',slashes*2);b.Append('"');return b.ToString();
    }
}




