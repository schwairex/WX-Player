namespace WXPlayer.Core;

public interface IDiscordConnection:IAsyncDisposable
{
    bool IsConnected {get;}
    Task SetActivityAsync(DiscordActivity? activity,CancellationToken ct);
}
public sealed class DiscordCommandException:IOException { }

// Single background owner of the connection. The UI only replaces an immutable mailbox.
public sealed class DiscordPresenceService:IAsyncDisposable
{
    private readonly object _sync=new();
    private readonly CancellationTokenSource _stop=new();
    private readonly Func<CancellationToken,Task<IDiscordConnection>> _connect;
    private readonly TimeSpan _retry,_cadence;
    private readonly bool _configured;
    private readonly Task _worker;
    private bool _enabled,_disposed;
    private DiscordActivity? _desired;
    public DiscordPresenceService(string applicationId,Func<CancellationToken,Task<IDiscordConnection>>? connect=null,TimeSpan? retry=null,TimeSpan? cadence=null)
    {
        _configured=applicationId.Length is >=17 and <=20&&applicationId.All(char.IsAsciiDigit);
        _connect=connect??(async ct=>await DiscordIpcConnection.ConnectAsync(applicationId,ct));
        _retry=retry??TimeSpan.FromSeconds(15);_cadence=cadence??TimeSpan.FromMilliseconds(250);
        _worker=Task.Run(RunAsync);
    }
    public void SetEnabled(bool enabled){lock(_sync){if(!_disposed)_enabled=enabled;}}
    public void Publish(DiscordActivity? activity){lock(_sync){if(!_disposed)_desired=activity;}}
    private async Task RunAsync()
    {
        IDiscordConnection? connection=null;DiscordActivity? sent=null;
        bool sentAny=false,withoutImages=false;DateTimeOffset retryAt=DateTimeOffset.MinValue,sendAt=DateTimeOffset.MinValue;
        var ct=_stop.Token;
        try
        {
            while(!ct.IsCancellationRequested)
            {
                bool enabled;DiscordActivity? desired;
                lock(_sync){enabled=_configured&&_enabled;desired=_desired;}
                if(!enabled)
                {
                    if(connection is not null){await CloseAsync(connection);connection=null;}
                    sent=null;sentAny=false;
                }
                else
                {
                    try
                    {
                        if(connection is not null&&!connection.IsConnected){await connection.DisposeAsync();connection=null;sentAny=false;}
                        if(connection is null&&DateTimeOffset.UtcNow>=retryAt)
                        {
                            connection=await _connect(ct);sent=null;sentAny=false;withoutImages=false;
                            // A selection or OFF may have arrived while connect was pending.
                            lock(_sync){enabled=_enabled&&!_disposed;desired=_desired;}
                        }
                        if(connection is not null&&enabled)
                        {
                            var activity=withoutImages&&desired is not null?desired with {Image=null}:desired;
                            if((!sentAny||activity!=sent)&&(activity is null||DateTimeOffset.UtcNow>=sendAt))
                            {
                                try{await connection.SetActivityAsync(activity,ct);}
                                catch(DiscordCommandException) when(activity?.Image is not null)
                                {withoutImages=true;activity=activity with{Image=null};await connection.SetActivityAsync(activity,ct);}
                                sent=activity;sentAny=true;
                                // Coalesce rapid seeks/channel changes; clears are never rate limited.
                                sendAt=DateTimeOffset.UtcNow+_cadence;
                            }
                        }
                    }
                    catch(OperationCanceledException) when(ct.IsCancellationRequested){break;}
                    catch
                    {
                        if(connection is not null){try{await connection.DisposeAsync();}catch{}connection=null;}
                        sentAny=false;retryAt=DateTimeOffset.UtcNow+_retry;
                    }
                }
                await Task.Delay(_cadence,ct);
            }
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested){}
        finally
        {
            if(connection is not null)await CloseAsync(connection);
            _stop.Dispose();
        }
    }
    private static async Task CloseAsync(IDiscordConnection connection)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromMilliseconds(750));
        try{if(connection.IsConnected)await connection.SetActivityAsync(null,timeout.Token);}catch{}
        try{await connection.DisposeAsync();}catch{}
    }
    public ValueTask DisposeAsync()
    {
        lock(_sync){if(!_disposed){_disposed=true;_enabled=false;_desired=null;_stop.Cancel();}}
        return new ValueTask(_worker);
    }
}
