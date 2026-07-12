using System;
using System.Collections.Generic;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Global registry so other systems can discover input sources by player id.
    /// </summary>
    public static class LocalInputSourceRegistry
    {
        static readonly Dictionary<int, ILocalPlayerInputSource> SourcesByPlayerId = new Dictionary<int, ILocalPlayerInputSource>();

        public static event Action<ILocalPlayerInputSource> SourceRegistered;
        public static event Action<int> SourceUnregistered;

        public static bool TryGetSource(int playerId, out ILocalPlayerInputSource source)
        {
            return SourcesByPlayerId.TryGetValue(playerId, out source);
        }

        public static void Register(ILocalPlayerInputSource source)
        {
            if (source == null || source.PlayerId <= 0)
                return;

            SourcesByPlayerId[source.PlayerId] = source;
            SourceRegistered?.Invoke(source);
        }

        public static void Unregister(ILocalPlayerInputSource source)
        {
            if (source == null)
                return;

            Unregister(source.PlayerId);
        }

        public static void Unregister(int playerId)
        {
            if (playerId <= 0)
                return;

            if (SourcesByPlayerId.Remove(playerId))
                SourceUnregistered?.Invoke(playerId);
        }
    }
}
