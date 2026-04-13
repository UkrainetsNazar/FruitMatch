using Infrastructure.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Presentation.Canvas
{
    public class MainMenuUI : MonoBehaviour
    {
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