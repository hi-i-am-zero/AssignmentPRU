using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyfallArena.Audio
{
    /// <summary>
    /// Audio toàn game: SFX (attack/block/damaged) + ambience theo map.
    /// DontDestroyOnLoad, tự tạo khi vào game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.45f;

        [Header("Clips (để trống = load từ Resources/Audio)")]
        [SerializeField] AudioClip attackClip;
        [SerializeField] AudioClip blockClip;
        [SerializeField] AudioClip damagedClip;
        [SerializeField] AudioClip mountainAmbience;
        [SerializeField] AudioClip skyAmbience;
        [SerializeField] AudioClip volcanoAmbience;
        [SerializeField] AudioClip weatherAmbience;
        [SerializeField] AudioClip testArenaAmbience;
        [SerializeField] AudioClip titleAmbience;

        AudioSource sfxSource;       // one-shot SFX
        AudioSource ambienceSource;  // nhạc nền loop
        string currentAmbienceKey;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null)
                return;

            var go = new GameObject("GameAudio");
            go.AddComponent<GameAudio>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            LoadClipsFromResources();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void Start() => PlayAmbienceForScene(SceneManager.GetActiveScene().name);

        // Đổi ambience mỗi lần đổi scene
        void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => PlayAmbienceForScene(scene.name);

        void EnsureSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f;
            }

            if (ambienceSource == null)
            {
                ambienceSource = gameObject.AddComponent<AudioSource>();
                ambienceSource.playOnAwake = false;
                ambienceSource.loop = true;
                ambienceSource.spatialBlend = 0f;
            }

            sfxSource.volume = sfxVolume;
            ambienceSource.volume = ambienceVolume;
        }

        // Load clip từ Resources/Audio/...
        void LoadClipsFromResources()
        {
            if (attackClip == null)
                attackClip = Resources.Load<AudioClip>("Audio/SFX/attack");
            if (blockClip == null)
                blockClip = Resources.Load<AudioClip>("Audio/SFX/block");
            if (damagedClip == null)
                damagedClip = Resources.Load<AudioClip>("Audio/SFX/damaged");

            if (mountainAmbience == null)
                mountainAmbience = Resources.Load<AudioClip>("Audio/Ambience/mountain");
            if (skyAmbience == null)
                skyAmbience = Resources.Load<AudioClip>("Audio/Ambience/sky");
            if (volcanoAmbience == null)
                volcanoAmbience = Resources.Load<AudioClip>("Audio/Ambience/volcano");

            if (weatherAmbience == null)
                weatherAmbience = skyAmbience != null ? skyAmbience : mountainAmbience;
            if (testArenaAmbience == null)
                testArenaAmbience = mountainAmbience;
            if (titleAmbience == null)
                titleAmbience = skyAmbience != null ? skyAmbience : mountainAmbience;
        }

        public void PlayAttack() => PlaySfx(attackClip);
        public void PlayBlock() => PlaySfx(blockClip, 0.9f);
        public void PlayDamaged() => PlaySfx(damagedClip);

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
                return;

            EnsureSources();
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
        }

        // Phát / đổi nhạc nền theo tên scene
        public void PlayAmbienceForScene(string sceneName)
        {
            EnsureSources();
            LoadClipsFromResources();

            AudioClip clip = ResolveAmbience(sceneName);
            string key = clip != null ? clip.name : string.Empty;
            if (clip == null)
            {
                StopAmbience();
                return;
            }

            if (ambienceSource.isPlaying && currentAmbienceKey == key && ambienceSource.clip == clip)
                return;

            currentAmbienceKey = key;
            ambienceSource.clip = clip;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        public void StopAmbience()
        {
            if (ambienceSource == null)
                return;

            ambienceSource.Stop();
            ambienceSource.clip = null;
            currentAmbienceKey = null;
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        public void SetAmbienceVolume(float volume)
        {
            ambienceVolume = Mathf.Clamp01(volume);
            if (ambienceSource != null)
                ambienceSource.volume = ambienceVolume;
        }

        AudioClip ResolveAmbience(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return mountainAmbience;

            switch (sceneName)
            {
                case "Mountain":
                    return mountainAmbience;
                case "Sky":
                    return skyAmbience;
                case "Volcano":
                    return volcanoAmbience;
                case "Weather":
                    return weatherAmbience != null ? weatherAmbience : skyAmbience;
                case "TestArena":
                    return testArenaAmbience != null ? testArenaAmbience : mountainAmbience;
                case "Title":
                case "Select":
                    return titleAmbience != null ? titleAmbience : skyAmbience;
                default:
                    return mountainAmbience;
            }
        }
    }
}
