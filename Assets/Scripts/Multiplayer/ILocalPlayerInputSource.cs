using System;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Interface đọc input 1 player local (LatestFrame + sự kiện mỗi frame).
    /// </summary>
    public interface ILocalPlayerInputSource
    {
        int PlayerId { get; }
        LocalPlayerInputFrame LatestFrame { get; }
        event Action<LocalPlayerInputFrame> InputFrameReceived;
    }

    /// <summary>
    /// Interface nhận input (nếu hệ thống muốn subscribe kiểu sink).
    /// </summary>
    public interface ILocalPlayerInputSink
    {
        void OnInputFrame(LocalPlayerInputFrame frame);
    }
}
