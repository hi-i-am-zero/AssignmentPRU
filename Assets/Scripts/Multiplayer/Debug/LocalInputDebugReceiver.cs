using UnityEngine;

namespace SkyfallArena.Multiplayer.DebugTools
{
    /// <summary>
    /// Simple receiver used to verify that each player's input stream is independent.
    /// Attach this to a Rigidbody2D object and set the player id.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LocalInputDebugReceiver : MonoBehaviour, ILocalPlayerInputSink
    {
        [SerializeField, Min(1)] int playerId = 1;
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float jumpImpulse = 10f;

        Rigidbody2D body;
        ILocalPlayerInputSource source;
        LocalPlayerInputFrame lastFrame;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void OnEnable()
        {
            TryConnect();
            LocalInputSourceRegistry.SourceRegistered += HandleSourceRegistered;
            LocalInputSourceRegistry.SourceUnregistered += HandleSourceUnregistered;
        }

        void OnDisable()
        {
            LocalInputSourceRegistry.SourceRegistered -= HandleSourceRegistered;
            LocalInputSourceRegistry.SourceUnregistered -= HandleSourceUnregistered;
            Disconnect();
        }

        void FixedUpdate()
        {
            body.linearVelocity = new Vector2(lastFrame.Move.x * moveSpeed, body.linearVelocity.y);

            if (lastFrame.JumpPressed)
                body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        }

        public void OnInputFrame(LocalPlayerInputFrame frame)
        {
            lastFrame = frame;
        }

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
            lastFrame = source.LatestFrame;
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
