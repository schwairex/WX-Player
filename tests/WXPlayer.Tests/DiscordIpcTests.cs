using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using WXPlayer.Core;

internal static class DiscordIpcTests
{
    internal static async Task RunAsync(Func<string,Func<Task>,Task> test,Action<bool,string> assert)
    {
        await test("Discord real IPC framing, handshake, ping, nonce, error, clear and disconnect",async()=>
        {
            string name="wx-discord-test-"+Guid.NewGuid().ToString("N");
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var ct=timeout.Token;
            await using var server=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
            var connect=DiscordIpcConnection.ConnectAsync("123456789012345678",ct,[name]);
            await server.WaitForConnectionAsync(ct);
            var hello=await Read(server,ct);
            assert(hello.Opcode==0&&hello.Json.GetProperty("v").GetInt32()==1&&hello.Json.GetProperty("client_id").GetString()=="123456789012345678","own ID handshake");
            await Write(server,1,"{\"cmd\":\"DISPATCH\",\"evt\":\"READY\",\"data\":{}}",ct);
            await using var client=await connect;
            await Write(server,3,"ping payload",ct);
            var pong=await ReadRaw(server,ct);assert(pong.Opcode==4&&pong.Body=="ping payload","ping pong");
            var set=client.SetActivityAsync(new("My Show","S01E02 · Episode",100,200,null),ct);
            var frame=await Read(server,ct);var activity=frame.Json.GetProperty("args").GetProperty("activity");
            assert(frame.Opcode==1&&activity.GetProperty("type").GetInt32()==3&&activity.GetProperty("timestamps").GetProperty("end").GetInt64()==200,"watching and seconds");
            await Write(server,1,"{\"cmd\":\"SET_ACTIVITY\",\"nonce\":\"wrong\"}",ct);
            await Task.Delay(30,ct);assert(!set.IsCompleted,"unrelated nonce ignored");
            string nonce=frame.Json.GetProperty("nonce").GetString()!;
            await Write(server,1,JsonSerializer.Serialize(new{cmd="SET_ACTIVITY",nonce}),ct);await set;
            var rejected=client.SetActivityAsync(new("Title","Movie",null,null,null),ct);
            frame=await Read(server,ct);nonce=frame.Json.GetProperty("nonce").GetString()!;
            await Write(server,1,JsonSerializer.Serialize(new{cmd="SET_ACTIVITY",evt="ERROR",nonce}),ct);
            bool error=false;try{await rejected;}catch(DiscordCommandException){error=true;}assert(error,"RPC ERROR reaches owner safely");
            var clear=client.SetActivityAsync(null,ct);frame=await Read(server,ct);
            assert(frame.Json.GetProperty("args").GetProperty("activity").ValueKind==JsonValueKind.Null,"explicit null activity");
            nonce=frame.Json.GetProperty("nonce").GetString()!;
            await Write(server,1,JsonSerializer.Serialize(new{cmd="SET_ACTIVITY",nonce}),ct);await clear;
            await Write(server,2,"{}",ct);await DiscordPresenceTests.Until(()=>!client.IsConnected);
        });
        await test("Discord IPC cancellation releases silent handshake and pending command",async()=>
        {
            foreach(bool ready in new[]{false,true})
            {
                string name="wx-discord-cancel-"+Guid.NewGuid().ToString("N");
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));var ct=timeout.Token;
                await using var server=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
                var connecting=DiscordIpcConnection.ConnectAsync("123456789012345678",ct,[name]);
                await server.WaitForConnectionAsync(ct);await Read(server,ct);
                if(!ready)
                {timeout.Cancel();bool cancelled=false;try{await connecting;}catch(OperationCanceledException){cancelled=true;}assert(cancelled,"silent handshake cancelled");}
                else
                {
                    await Write(server,1,"{\"evt\":\"READY\"}",ct);await using var client=await connecting;
                    var pending=client.SetActivityAsync(new("Title","Movie",null,null,null),ct);await Read(server,ct);
                    timeout.Cancel();bool cancelled=false;try{await pending;}catch(OperationCanceledException){cancelled=true;}
                    assert(cancelled,"pending command cancelled");await client.DisposeAsync();assert(!client.IsConnected,"disposed connection inactive");
                }
            }
        });
    }
    private static async Task<(int Opcode,string Body)> ReadRaw(Stream stream,CancellationToken ct)
    {
        var header=new byte[8];await stream.ReadExactlyAsync(header,ct);
        int length=BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
        if(length<0||length>1048576)throw new IOException("Invalid test frame");
        var body=new byte[length];await stream.ReadExactlyAsync(body,ct);
        return(BinaryPrimitives.ReadInt32LittleEndian(header),Encoding.UTF8.GetString(body));
    }
    private static async Task<(int Opcode,JsonElement Json)> Read(Stream stream,CancellationToken ct)
    {var frame=await ReadRaw(stream,ct);using var json=JsonDocument.Parse(frame.Body);return(frame.Opcode,json.RootElement.Clone());}
    private static async Task Write(Stream stream,int opcode,string text,CancellationToken ct)
    {
        byte[] payload=Encoding.UTF8.GetBytes(text),frame=new byte[payload.Length+8];
        BinaryPrimitives.WriteInt32LittleEndian(frame,opcode);BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4),payload.Length);payload.CopyTo(frame,8);
        // Deliberately fragmented: pipe reads need not align with protocol frames.
        await stream.WriteAsync(frame.AsMemory(0,3),ct);await stream.WriteAsync(frame.AsMemory(3),ct);await stream.FlushAsync(ct);
    }
}
