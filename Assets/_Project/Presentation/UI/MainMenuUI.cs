using UnityEngine;
using UnityEngine.SceneManagement;

namespace Presentation.Canvas
{
    public class MainMenuUI : MonoBehaviour
    {
        public void OnOnlineGameClick()
        {
            SceneManager.LoadScene("Lobby");
        }

        public void OnSingleGameClick()
        {
            SceneManager.LoadScene("Game");
        }
    }
}