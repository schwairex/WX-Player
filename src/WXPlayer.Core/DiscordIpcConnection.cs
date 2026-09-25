using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace WXPlayer.Core;

// Windows RPC transport, implemented against Discord's documented IPC framing.
// No network sockets, authentication tokens, URI registration or external processes.
public sealed class DiscordIpcConnection:IDiscordConnection
{
    private readonly NamedPipeClientStream _pipe;
    private readonly CancellationTokenSource _life=new();
    private readonly SemaphoreSlim _write=new(1,1);
    private readonly object _sync=new();
    private readonly TaskCompletionSource _ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task _reader=Task.CompletedTask;
    private TaskCompletionSource? _reply;
    private string? _nonce;
    private volatile bool _connected;
    private Task? _dispose;
    public bool IsConnected=>_connected;
    private DiscordIpcConnection(NamedPipeClientStream pipe)=>_pipe=pipe;
    public static async Task<DiscordIpcConnection> ConnectAsync(string applicationId,CancellationToken ct,IReadOnlyList<string>? pipeNames=null)
    {
        if(applicationId.Length is <17 or >20||!applicationId.All(char.IsAsciiDigit))throw new ArgumentException("Invalid Discord application ID.");
        foreach(string name in pipeNames??Enumerable.Range(0,10).Select(i=>"discord-ipc-"+i).ToArray())
        {
            ct.ThrowIfCancellationRequested();
            var pipe=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.Asynchronous);
            DiscordIpcConnection? connection=null;
            try
            {
                await pipe.ConnectAsync(150,ct);
                connection=new(pipe);connection._reader=connection.ReadLoopAsync();
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(3));
                await connection.WriteFrameAsync(0,JsonSerializer.SerializeToUtf8Bytes(new{v=1,client_id=applicationId}),timeout.Token);
                await connection._ready.Task.WaitAsync(timeout.Token);
                return connection;
            }
            catch
            {
                if(connection is not null)await connection.DisposeAsync();else pipe.Dispose();
                ct.ThrowIfCancellationRequested();
            }
        }
        throw new IOException("Discord IPC unavailable.");
    }
    public static string SerializeActivity(DiscordActivity? activity,string nonce,int pid)
    {
        Dictionary<string,object?>? wire=null;
        if(activity is not null)
        {
            wire=new(){["type"]=3,["details"]=PresenceMedia.PublicText(activity.Details),["state"]=PresenceMedia.PublicText(activity.State)};
            if(activity.Start is >0&&activity.End>activity.Start)wire["timestamps"]=new{start=activity.Start,end=activity.End};
            if(PresenceMedia.PublicImage(activity.Image??"") is {} image)wire["assets"]=new{large_image=image,large_text=PresenceMedia.PublicText(activity.Details)};
        }
        return JsonSerializer.Serialize(new{cmd="SET_ACTIVITY",args=new{pid,activity=wire},nonce});
    }
    public async Task SetActivityAsync(DiscordActivity? activity,CancellationToken ct)
    {
        if(!IsConnected)throw new IOException("Discord disconnected.");
        var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string nonce=Guid.NewGuid().ToString("N");
        lock(_sync)
        {
            if(_reply is not null)throw new InvalidOperationException("Only one Discord command may be pending.");
            _reply=completion;_nonce=nonce;
        }
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct,_life.Token);timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await WriteFrameAsync(1,Encoding.UTF8.GetBytes(SerializeActivity(activity,nonce,Environment.ProcessId)),timeout.Token);
            await completion.Task.WaitAsync(timeout.Token);
        }
        finally {lock(_sync){if(ReferenceEquals(_reply,completion)){_reply=null;_nonce=null;}}}
    }
    private async Task WriteFrameAsync(int opcode,byte[] payload,CancellationToken ct)
    {
        await _write.WaitAsync(ct);
        try
        {
            byte[] frame=new byte[payload.Length+8];BinaryPrimitives.WriteInt32LittleEndian(frame,opcode);
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4),payload.Length);payload.CopyTo(frame,8);
            await _pipe.WriteAsync(frame,ct);await _pipe.FlushAsync(ct);
        }
        finally {_write.Release();}
    }
    private async Task ReadLoopAsync()
    {
        try
        {
            byte[] header=new byte[8];
            while(!_life.IsCancellationRequested)
            {
                await _pipe.ReadExactlyAsync(header,_life.Token);
                int opcode=BinaryPrimitives.ReadInt32LittleEndian(header),length=BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
                if(length<0||length>1048576)throw new IOException("Invalid Discord frame.");
                byte[] payload=new byte[length];await _pipe.ReadExactlyAsync(payload,_life.Token);
                if(opcode==2)throw new IOException("Discord closed.");
                if(opcode==3){await WriteFrameAsync(4,payload,_life.Token);continue;}
                if(opcode==4)continue;
                if(opcode!=1)throw new IOException("Unexpected Discord opcode.");
                using var json=JsonDocument.Parse(payload);var root=json.RootElement;
                string? evt=root.TryGetProperty("evt",out var e)?e.GetString():null;
                if(evt=="READY"){_connected=true;_ready.TrySetResult();continue;}
                string? nonce=root.TryGetProperty("nonce",out var n)?n.GetString():null;
                lock(_sync)
                {
                    if(nonce is not null&&nonce==_nonce&&_reply is not null)
                    {
                        if(evt=="ERROR")_reply.TrySetException(new DiscordCommandException());
                        else if(root.TryGetProperty("cmd",out var command)&&command.GetString()=="SET_ACTIVITY")_reply.TrySetResult();
                    }
                }
            }
        }
        catch(Exception) { /* IPC failure is contained here, never on a playback thread. */ }
        finally
        {
            _connected=false;_ready.TrySetCanceled();
            lock(_sync)_reply?.TrySetException(new IOException("Discord disconnected."));
        }
    }
    public ValueTask DisposeAsync()
    {
        lock(_sync)return new ValueTask(_dispose??=CloseAsync());
    }
    private async Task CloseAsync()
    {
        _connected=false;_life.Cancel();_pipe.Dispose();
        await _reader.ConfigureAwait(false);
        // Write operations are awaited by the single service owner before disposal.
        _life.Dispose();
    }
}
