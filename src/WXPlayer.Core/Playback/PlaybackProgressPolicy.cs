namespace WXPlayer.Core;

public static class PlaybackProgressPolicy
{
    // A terminal VLC state can be caused by a stalled or expired stream. Only
    // observed playback close to the actual end marks a film or episode watched.
    public static bool NaturalEnd(long observedPositionMs,long durationMs) =>
        durationMs>0 && observedPositionMs>=Math.Max(durationMs*.95,durationMs-15000);
}
