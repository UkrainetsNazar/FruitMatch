using Infrastructure.Audio;
using Presentation.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Presentation.Canvas
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PanelAnimator settingsPanel;
        [SerializeField] private Slider musicVolumeSlider, sfxVolumeSlider;
        [SerializeField] private TMP_Text musicVolumeText, sfxVolumeText;
        [SerializeField] private ButtonAnimator backButton, quitButton;

        void Start()
        {
            VolumeSliderBinder.BindMusic(musicVolumeSlider, musicVolumeText);
            VolumeSliderBinder.BindSfx(sfxVolumeSlider, sfxVolumeText);

            backButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                settingsPanel.Hide();
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
                if (settingsPanel.IsVisible) settingsPanel.Hide(); else settingsPanel.Show();
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