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

        /// <summary>Legacy single-attack flag (true if any Attack1-4 pressed).</summary>
        public bool AttackPressed;
        public bool AttackHeld;

        public bool Attack1Pressed;
        public bool Attack2Pressed;
        public bool Attack3Pressed;
        public bool Attack4Pressed;

        public bool BlockHeld;

        public bool InteractPressed;
        public bool CrouchHeld;
        public bool SprintHeld;

        public bool SubmitPressed;
        public bool CancelPressed;

        public string DeviceName;

        public int GetPressedAttackSlot()
        {
            if (Attack1Pressed) return 1;
            if (Attack2Pressed) return 2;
            if (Attack3Pressed) return 3;
            if (Attack4Pressed) return 4;
            return 0;
        }
    }
}
