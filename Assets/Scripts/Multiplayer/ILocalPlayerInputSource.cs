using System;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Read-only input stream exposed per local player.
    /// </summary>
    public interface ILocalPlayerInputSource
    {
        int PlayerId { get; }
        LocalPlayerInputFrame LatestFrame { get; }
        event Action<LocalPlayerInputFrame> InputFrameReceived;
    }

    /// <summary>
    /// Optional consumer contract for systems that react to local input.
    /// </summary>
    public interface ILocalPlayerInputSink
    {
        void OnInputFrame(LocalPlayerInputFrame frame);
    }
}
