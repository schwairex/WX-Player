namespace WXPlayer.Core;

public static class RecordingStartPolicy
{
    public static int StartAt(ContentKind kind, bool seekable, long positionMs) =>
        seekable && kind is ContentKind.Movie or ContentKind.Episode
            ? (int)Math.Clamp(positionMs / 1000, 0, int.MaxValue)
            : 0;
}

public enum ChannelHealthState { None, Checking, Healthy, Unavailable }

public sealed class ChannelHealthMonitor(TimeSpan timeout)
{
    private int _generation;
    private DateTimeOffset _started;
    public ChannelHealthState State { get; private set; }
    public int CurrentGeneration => _generation;
    public int Begin(DateTimeOffset now)
    {
        _started = now;
        State = ChannelHealthState.Checking;
        return ++_generation;
    }
    public void MarkPlaying(int generation)
    {
        if (generation == _generation && State != ChannelHealthState.None)
            State = ChannelHealthState.Healthy;
    }
    public void MarkFailed(int generation)
    {
        if (generation == _generation && State != ChannelHealthState.None)
            State = ChannelHealthState.Unavailable;
    }
    public void Tick(DateTimeOffset now)
    {
        if (State == ChannelHealthState.Checking && now - _started >= timeout)
            State = ChannelHealthState.Unavailable;
    }
    public void Stop() { ++_generation; State = ChannelHealthState.None; }
}
