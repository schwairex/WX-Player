namespace WXPlayer.Core;

public sealed class EpgService(ProviderClient provider,LibraryStore store,CancellationToken lifetime)
{
    private readonly CancellationTokenSource _stop=CancellationTokenSource.CreateLinkedTokenSource(lifetime);
    private readonly Dictionary<string,Task<string?>> _jobs=[];
    private readonly Dictionary<string,DateTimeOffset> _retryAfter=[];
    public async Task StopAsync(){_stop.Cancel();Task[] jobs;lock(_jobs)jobs=_jobs.Values.ToArray();try{await Task.WhenAll(jobs);}catch(OperationCanceledException){} }
    public Task<string?> RefreshAsync(SourceConfig source,bool force=false)
    {
        lock(_jobs)
        {
            if(_stop.IsCancellationRequested)return Task.FromResult<string?>("Rehber yüklemesi durduruldu.");
            if(_jobs.TryGetValue(source.Id,out var job)&&!job.IsCompleted)return job;
            if(!force&&_retryAfter.GetValueOrDefault(source.Id)>DateTimeOffset.UtcNow)return Task.FromResult<string?>("Rehber bağlantısı daha sonra yeniden denenecek.");
            return _jobs[source.Id]=Task.Run(async()=>
            {
                try
                {
                    var updated=await store.EpgUpdatedAsync(source.Id);
                    if(!force&&updated>DateTimeOffset.UtcNow.AddHours(-6))return null;
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);timeout.CancelAfter(TimeSpan.FromMinutes(3));
                    await provider.LoadEpgAsync(source,store,timeout.Token);await store.SaveSourceAsync(source);lock(_jobs)_retryAfter.Remove(source.Id);return null;
                }
                catch(OperationCanceledException)when(_stop.IsCancellationRequested){return "Rehber yüklemesi durduruldu.";}
                catch(Exception ex)
                {
                    lock(_jobs)_retryAfter[source.Id]=DateTimeOffset.UtcNow.AddMinutes(5);
                    return ex is InvalidOperationException?ex.Message:"Rehber sunucusuna ulaşılamadı. Kayıtlı rehber korunuyor; yeniden denenecek.";
                }
            });
        }
    }
}


