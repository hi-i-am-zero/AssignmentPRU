using UnityEngine;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Snapshot of one player's input for a single frame.
    /// </summary>
    public struct LocalPlayerInputFrame
    {
        public int PlayerId;
        public Vector2 Move;

        public bool JumpPressed;
        public bool JumpHeld;
        public bool AttackPressed;
        public bool AttackHeld;
        public bool InteractPressed;
        public bool CrouchHeld;
        public bool SprintHeld;

        public bool SubmitPressed;
        public bool CancelPressed;

        public string DeviceName;
    }
}
