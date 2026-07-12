using UnityEngine;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Base class to help gameplay scripts subscribe to a player input source by player id.
    /// Member 1 can inherit from this and map input frames to movement/combat logic.
    /// </summary>
    public abstract class LocalInputSubscriber : MonoBehaviour, ILocalPlayerInputSink
    {
        [SerializeField, Min(1)] int playerId = 1;

        ILocalPlayerInputSource source;

        protected int PlayerId => playerId;

        protected virtual void OnEnable()
        {
            LocalInputSourceRegistry.SourceRegistered += HandleSourceRegistered;
            LocalInputSourceRegistry.SourceUnregistered += HandleSourceUnregistered;
            TryConnect();
        }

        protected virtual void OnDisable()
        {
            LocalInputSourceRegistry.SourceRegistered -= HandleSourceRegistered;
            LocalInputSourceRegistry.SourceUnregistered -= HandleSourceUnregistered;
            Disconnect();
        }

        public abstract void OnInputFrame(LocalPlayerInputFrame frame);

        void TryConnect()
        {
            if (LocalInputSourceRegistry.TryGetSource(playerId, out var found))
                Connect(found);
        }

        void Connect(ILocalPlayerInputSource newSource)
        {
            if (newSource == null || source == newSource)
                return;

            Disconnect();
            source = newSource;
            source.InputFrameReceived += OnInputFrame;
            OnInputFrame(source.LatestFrame);
        }

        void Disconnect()
        {
            if (source == null)
                return;

            source.InputFrameReceived -= OnInputFrame;
            source = null;
        }

        void HandleSourceRegistered(ILocalPlayerInputSource registeredSource)
        {
            if (registeredSource.PlayerId == playerId)
                Connect(registeredSource);
        }

        void HandleSourceUnregistered(int removedPlayerId)
        {
            if (removedPlayerId == playerId)
                Disconnect();
        }
    }
}
