using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WXPlayer.App;

// Loopback-only fixtures, created exclusively by --smoke.
internal sealed class SmokeHttpServer : IAsyncDisposable
{
    private readonly TcpListener _listener=new(IPAddress.Loopback,0);
    private readonly CancellationTokenSource _stop=new();
    private readonly List<Task> _clients=[];
    private readonly Task _accept;
    private readonly byte[] _bytes;
    private int _disposed; private readonly bool _paced; private readonly int _delayMs;
    internal string Url {get;}
    internal int Connections;
    internal SmokeHttpServer(byte[] bytes,bool paced=false,int delayMs=0)
    {
        _bytes=bytes;_paced=paced;_delayMs=delayMs;_listener.Start();Url=$"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/fixture."+(paced?"ts":"ico");
        _accept=Task.Run(async()=>{try{while(!_stop.IsCancellationRequested){var client=await _listener.AcceptTcpClientAsync(_stop.Token);lock(_clients)_clients.Add(Serve(client));}}catch(OperationCanceledException){}catch(SocketException){}});
    }
    private async Task Serve(TcpClient client)
    {
        using(client)try
        {
            var ct=_stop.Token;await using var stream=client.GetStream();byte[] request=new byte[8192];int count=0;
            while(count<request.Length){int n=await stream.ReadAsync(request.AsMemory(count),ct);if(n==0)return;count+=n;if(Encoding.ASCII.GetString(request,0,count).Contains("\r\n\r\n"))break;}
            Interlocked.Increment(ref Connections);if(_delayMs>0)await Task.Delay(_delayMs,ct);
            string header="HTTP/1.1 200 OK\r\nConnection: close\r\nContent-Type: "+(_paced?"video/mp2t":"image/x-icon")+"\r\n"+(_paced?"":"Content-Length: "+_bytes.Length+"\r\n")+"\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(header),ct);
            if(!_paced){await stream.WriteAsync(_bytes,ct);return;}
            int cycle=0;long step=TransportDuration(_bytes);var continuity=new Dictionary<int,int>();
            do{byte[] loop=ShiftTransport(_bytes,step*cycle++,continuity);for(int offset=0;offset<loop.Length;offset+=1316){await stream.WriteAsync(loop.AsMemory(offset,Math.Min(1316,loop.Length-offset)),ct);await Task.Delay(10,ct);}}while(!ct.IsCancellationRequested);
        }catch(IOException){}catch(OperationCanceledException){}catch(SocketException){}
    }
    private static long ReadPcr(byte[] b,int p)=>((long)b[p]<<25)|((long)b[p+1]<<17)|((long)b[p+2]<<9)|((long)b[p+3]<<1)|((long)b[p+4]>>7);
    private static long TransportDuration(byte[] b)
    {
        long first=-1,last=0;for(int p=0;p+188<=b.Length;p+=188)if(b[p]==0x47&&(b[p+3]&0x20)!=0&&b[p+4]>=7&&(b[p+5]&0x10)!=0){long value=ReadPcr(b,p+6);if(first<0)first=value;last=value;}
        return Math.Max(90000,last-first+3600);
    }
    private static byte[] ShiftTransport(byte[] input,long shift,Dictionary<int,int> counters)
    {
        var b=(byte[])input.Clone();const long mask=(1L<<33)-1;
        void Timestamp(int p){long value=(((long)b[p]>>1)&7)<<30|((long)b[p+1]<<22)|(((long)b[p+2]>>1)<<15)|((long)b[p+3]<<7)|((long)b[p+4]>>1);value=(value+shift)&mask;b[p]=(byte)(((long)(byte)(b[p]&0xF1))|((value>>29)&14));b[p+1]=(byte)(value>>22);b[p+2]=(byte)(((value>>14)&254)|1);b[p+3]=(byte)(value>>7);b[p+4]=(byte)((value<<1)|1);}
        for(int p=0;p+188<=b.Length;p+=188)
        {
            if(b[p]!=0x47)continue;int pid=((b[p+1]&31)<<8)|b[p+2];int payload=p+4;
            bool hasPayload=(b[p+3]&0x10)!=0;if(hasPayload){int cc=counters.GetValueOrDefault(pid,b[p+3]&15);b[p+3]=(byte)((b[p+3]&240)|(cc&15));counters[pid]=(cc+1)&15;}
            if((b[p+3]&0x20)!=0)
            {
                if(b[p+4]>=7&&(b[p+5]&0x10)!=0){int q=p+6;long value=(ReadPcr(b,q)+shift)&mask;b[q]=(byte)(value>>25);b[q+1]=(byte)(value>>17);b[q+2]=(byte)(value>>9);b[q+3]=(byte)(value>>1);b[q+4]=(byte)(((long)(byte)(b[q+4]&127))|((value&1)<<7));}
                payload+=b[p+4]+1;
            }
            if(hasPayload&&(b[p+1]&64)!=0&&payload+19<=p+188&&b[payload]==0&&b[payload+1]==0&&b[payload+2]==1)
            {if((b[payload+7]&128)!=0)Timestamp(payload+9);if((b[payload+7]&64)!=0)Timestamp(payload+14);}
        }
        return b;
    }
    public async ValueTask DisposeAsync(){if(Interlocked.Exchange(ref _disposed,1)!=0)return;_stop.Cancel();_listener.Stop();await _accept;Task[] tasks;lock(_clients)tasks=_clients.ToArray();await Task.WhenAll(tasks);_stop.Dispose();}
}


