using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Lưu lựa chọn P1/P2/map và kết quả trận giữa các scene (DontDestroyOnLoad).
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public const string TitleSceneName = "Title";
        public const string SelectSceneName = "Select";

        static GameSession instance;

        public static GameSession Instance
        {
            get
            {
                if (instance == null)
                    EnsureExists();
                return instance;
            }
        }

        // Lựa chọn trước khi Fight
        public CharacterType.Character Player1Character { get; private set; } = CharacterType.Character.Knight;
        public CharacterType.Character Player2Character { get; private set; } = CharacterType.Character.Ninja;
        public string SelectedMapSceneName { get; private set; } = "Mountain";
        public bool HasMatchSelection { get; private set; }

        // Kết quả trận vừa kết thúc
        public int LastWinnerPlayerId { get; private set; }
        public int LastAliveCount { get; private set; }
        public bool HasLastResult { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            EnsureExists();
        }

        static void EnsureExists()
        {
            if (instance != null)
                return;

            var existing = FindFirstObjectByType<GameSession>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            var go = new GameObject("GameSession");
            instance = go.AddComponent<GameSession>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetPlayer1Character(CharacterType.Character character) => Player1Character = character;
        public void SetPlayer2Character(CharacterType.Character character) => Player2Character = character;
        public void SetSelectedMap(string mapSceneName) => SelectedMapSceneName = mapSceneName;

        // Đánh dấu đã chọn đủ để vào map
        public void ConfirmMatchSelection()
        {
            HasMatchSelection = !string.IsNullOrEmpty(SelectedMapSceneName);
        }

        // Reset lựa chọn (về màn Select)
        public void ClearMatchSelection()
        {
            HasMatchSelection = false;
            Player1Character = CharacterType.Character.Knight;
            Player2Character = CharacterType.Character.Ninja;
            SelectedMapSceneName = "Mountain";
        }

        public void SetLastResult(int winnerPlayerId, int aliveCount)
        {
            LastWinnerPlayerId = winnerPlayerId;
            LastAliveCount = aliveCount;
            HasLastResult = true;
        }

        public void ClearLastResult()
        {
            HasLastResult = false;
            LastWinnerPlayerId = 0;
            LastAliveCount = 0;
        }

        public void LoadTitle() => SceneManager.LoadScene(TitleSceneName);

        // Về chọn nhân vật/map — xóa kết quả + lựa chọn cũ
        public void LoadSelect()
        {
            ClearLastResult();
            ClearMatchSelection();
            SceneManager.LoadScene(SelectSceneName);
        }

        // Load đúng map đã chọn trong Select
        public void LoadSelectedMap()
        {
            ConfirmMatchSelection();
            if (!HasMatchSelection)
            {
                Debug.LogError("[GameSession] Cannot load map: selection incomplete.");
                return;
            }

            SceneManager.LoadScene(SelectedMapSceneName);
        }
    }
}
