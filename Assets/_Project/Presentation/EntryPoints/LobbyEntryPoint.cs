using Infrastructure.Network;
using UnityEngine;
using Zenject;

namespace Presentation.EntryPoints
{
    public class LobbyEntryPoint : MonoBehaviour
    {
        [Inject] private LobbyManager _lobbyManager;

        private void Start()
        {
            var lobbies = _lobbyManager.GetLobbiesAsync();
        }
    }
}