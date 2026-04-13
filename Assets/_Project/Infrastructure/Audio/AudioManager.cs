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

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool isMenuOrLobby = scene.name == "Menu" || scene.name == "Lobby";

            if (isMenuOrLobby)
                PlayMusic();
            else
                StopMusic();
        }

        // ── Music ─────────────────────────────────────────────

        private void PlayMusic()
        {
            if (_musicSource.clip == _menuMusic && _musicSource.isPlaying) return;

            _musicSource.clip = _menuMusic;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        private void StopMusic()
        {
            _musicSource.Stop();
        }

        // ── SFX ───────────────────────────────────────────────

        public static void PlayButtonClick()  => Instance?._sfxSource.PlayOneShot(Instance._buttonClick);
        public static void PlayFruitSwap()    => Instance?._sfxSource.PlayOneShot(Instance._fruitSwap);
        public static void PlayFruitDestroy() => Instance?._sfxSource.PlayOneShot(Instance._fruitDestroy);
    }
}