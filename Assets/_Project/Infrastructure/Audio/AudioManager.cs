using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infrastructure.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private AudioSource _musicSource;

        [Header("SFX")]
        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _buttonClick;
        [SerializeField] private AudioClip _fruitSwap;
        [SerializeField] private AudioClip _fruitDestroy;
        [SerializeField] private AudioClip _gameWin;
        [SerializeField] private AudioClip _gameLose;

        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private static AudioManager Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            float savedVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            _musicSource.volume = savedVolume;

            float savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            _sfxSource.volume = savedSfxVolume;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Update()
        {
            bool isMenuOrLobby = SceneManager.GetActiveScene().name == "Menu"
                              || SceneManager.GetActiveScene().name == "Lobby";

            if (isMenuOrLobby && !_musicSource.isPlaying)
                PlayMusic();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool isMenuOrLobby = scene.name == "Menu" || scene.name == "Lobby";

            if (isMenuOrLobby) PlayMusic();
            else StopMusic();
        }

        // ── Music ─────────────────────────────────────────────

        private void PlayMusic()
        {
            if (_musicSource.isPlaying) return;

            _musicSource.clip = _menuMusic;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        private void StopMusic()
        {
            _musicSource.Stop();
        }

        public static void SetMusicVolume(float volume)
        {
            if (Instance == null) return;
            Instance._musicSource.volume = volume;
            PlayerPrefs.SetFloat(MusicVolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static float GetMusicVolume() =>
            PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

        public static void SetSfxVolume(float volume)
        {
            if (Instance == null) return;
            Instance._sfxSource.volume = volume;
            PlayerPrefs.SetFloat(SfxVolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static float GetSfxVolume() =>
            PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        // ── SFX ───────────────────────────────────────────────

        public static void PlayButtonClick() => Instance?._sfxSource.PlayOneShot(Instance._buttonClick);
        public static void PlayFruitSwap() => Instance?._sfxSource.PlayOneShot(Instance._fruitSwap);
        public static void PlayFruitDestroy() => Instance?._sfxSource.PlayOneShot(Instance._fruitDestroy);
        public static void PlayWinGame() => Instance?._sfxSource.PlayOneShot(Instance._gameWin);
        public static void PlayLoseGame() => Instance?._sfxSource.PlayOneShot(Instance._gameLose);
    }
}