using Infrastructure.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Presentation.Canvas
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider musicVolumeSlider, sfxVolumeSlider;
        [SerializeField] private TMP_Text musicVolumeText, sfxVolumeText;
        [SerializeField] private Button backButton, quitButton;

        void Start()
        {
            settingsPanel.SetActive(false);

            VolumeSliderBinder.BindMusic(musicVolumeSlider, musicVolumeText);
            VolumeSliderBinder.BindSfx(sfxVolumeSlider, sfxVolumeText);

            backButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                settingsPanel.SetActive(false);
            });

            quitButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                Application.Quit();
            });
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        public void OnOnlineGameClick()
        {
            AudioManager.PlayButtonClick();
            SceneManager.LoadScene("Lobby");
        }

        public void OnSingleGameClick()
        {
            AudioManager.PlayButtonClick();
            SceneManager.LoadScene("Game");
        }
    }
}