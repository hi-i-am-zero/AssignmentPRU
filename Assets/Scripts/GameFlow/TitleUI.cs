using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Màn Title: nút Play → Select. UI gắn sẵn trong scene (Inspector).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleUI : MonoBehaviour
    {
        [SerializeField] Button playButton;

        void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError("[TitleUI] Missing Play button reference.", this);
                return;
            }

            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => GameSession.Instance.LoadSelect());
        }
    }
}
