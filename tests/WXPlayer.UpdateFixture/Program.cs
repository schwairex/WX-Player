// Isolated future-version test executable, never included in a production payload.
using System.Diagnostics;
using System.Text.Json;
using WXPlayer.Core;

int index=Array.IndexOf(args,"--data-dir");
if(index<0||index+1>=args.Length)return 2;
string data=Path.GetFullPath(args[index+1]);
bool previousExited=true;
if(int.TryParse(Environment.GetEnvironmentVariable("WXPLAYER_TEST_PARENT"),out var pid))try{using var parent=Process.GetProcessById(pid);previousExited=parent.HasExited;}catch(ArgumentException){}
if(!previousExited)return 3;
string? pending=Environment.GetEnvironmentVariable("WXPLAYER_PENDING_VERSION");
if(pending is not null)
{
    var update=new PreparedUpdate(Version.Parse(pending),Environment.GetEnvironmentVariable("WXPLAYER_PENDING_EXE")!,Environment.GetEnvironmentVariable("WXPLAYER_PENDING_HASH")!);
    await UpdateActivation.ActivateAsync(data,update,new Version(1,4,0));
}
string counter=Path.Combine(data,"fixture-launch-count.txt");int runs=File.Exists(counter)?int.Parse(File.ReadAllText(counter)):0;await File.WriteAllTextAsync(counter,(runs+1).ToString());
await File.WriteAllTextAsync(Path.Combine(data,"fixture-activated.json"),JsonSerializer.Serialize(new{previousExited,version="1.4.0",runs=runs+1,pointer=File.Exists(UpdateActivation.Pointer(data)),dataPreserved=File.Exists(Path.Combine(data,"library.db"))}));
return 0;

