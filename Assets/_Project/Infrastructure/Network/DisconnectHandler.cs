using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Infrastructure.Network
{
    public class DisconnectHandler : MonoBehaviour
    {
        [Inject] private NetworkGameManager _network;

        void Start()
        {
            _network.OnOpponentDisconnected += OnOpponentDisconnected;
        }

        void OnDestroy()
        {
            if (_network != null)
                _network.OnOpponentDisconnected -= OnOpponentDisconnected;
        }

        private void OnOpponentDisconnected()
        {
            HandleDisconnect().Forget();
        }

        private async UniTaskVoid HandleDisconnect()
        {
            NetworkManager.Singleton.Shutdown();
            await UniTask.Delay(500);
            SceneManager.LoadScene("Lobby");
        }
    }
}