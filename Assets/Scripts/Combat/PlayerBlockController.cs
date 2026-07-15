using UnityEngine;
using SkyfallArena.Multiplayer;

/// <summary>
/// Block: giữ I / Numpad5 — giảm damage + knockback, không đánh/di chuyển khi đang block.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerBlockController : MonoBehaviour
{
    [Header("Block")]
    [SerializeField, Range(0f, 1f)] float damageTakenWhileBlocking = 0.5f;
    [SerializeField, Range(0f, 1f)] float knockbackMultiplierWhileBlocking = 0.35f;

    [Header("References")]
    [SerializeField] CharacterAnimationSync animationSync;
    [SerializeField] LocalPlayerInputSource inputSource;
    [SerializeField] PlayerController playerController;
    [SerializeField] CharacterType characterType;

    bool isBlocking;

    public bool IsBlocking => isBlocking;
    public float DamageTakenWhileBlocking => damageTakenWhileBlocking;
    public float KnockbackMultiplierWhileBlocking => knockbackMultiplierWhileBlocking;

    void Awake()
    {
        if (animationSync == null)
            animationSync = GetComponent<CharacterAnimationSync>();
        if (inputSource == null)
            inputSource = GetComponent<LocalPlayerInputSource>();
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        if (characterType == null)
            characterType = GetComponent<CharacterType>();
    }

    void Update()
    {
        bool wantsBlock = ReadBlockHeld();
        bool canBlock = CanBlock();
        SetBlocking(wantsBlock && canBlock);
    }

    bool CanBlock()
    {
        return true;
    }

    bool ReadBlockHeld()
    {
        if (playerController == null)
            return false;

        if (playerController.playerType == PlayerController.PlayerType.Player1)
            return Input.GetKey(KeyCode.I);

        return Input.GetKey(KeyCode.Keypad5);
    }

    void SetBlocking(bool value)
    {
        if (isBlocking == value)
            return;

        isBlocking = value;
        if (animationSync != null)
            animationSync.SetBlocking(isBlocking);

        if (isBlocking)
            SkyfallArena.Audio.GameAudio.Instance?.PlayBlock();
    }
}
