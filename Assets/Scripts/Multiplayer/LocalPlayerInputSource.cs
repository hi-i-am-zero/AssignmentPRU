using System;
using UnityEngine;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Gắn trên player đã spawn: nhận và phát frame input từ LocalMultiplayerManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayerInputSource : MonoBehaviour, ILocalPlayerInputSource
    {
        [SerializeField] int playerId;

        LocalPlayerInputFrame latestFrame;
        bool isRegistered;

        public int PlayerId => playerId;
        public LocalPlayerInputFrame LatestFrame => latestFrame;

        public event Action<LocalPlayerInputFrame> InputFrameReceived;

        public void Initialize(int assignedPlayerId)
        {
            playerId = Mathf.Max(0, assignedPlayerId);
            latestFrame = default;
            latestFrame.PlayerId = playerId;
            TryRegister();
        }

        public void PublishFrame(LocalPlayerInputFrame frame)
        {
            if (playerId <= 0)
                return;

            frame.PlayerId = playerId;
            latestFrame = frame;
            InputFrameReceived?.Invoke(frame);
        }

        void OnEnable()
        {
            TryRegister();
        }

        void OnDisable()
        {
            if (!isRegistered)
                return;

            LocalInputSourceRegistry.Unregister(this);
            isRegistered = false;
        }

        void TryRegister()
        {
            if (isRegistered || playerId <= 0)
                return;

            LocalInputSourceRegistry.Register(this);
            isRegistered = true;
        }
    }
}
