using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Infrastructure.Network
{
    public class NetworkService
    {
        private readonly LobbyManager _lobbyManager;
        private readonly RelayManager _relayManager;

        public bool IsHost { get; private set; }
        public bool IsClient { get; private set; }

        public NetworkService(LobbyManager lobbyManager, RelayManager relayManager)
        {
            _lobbyManager = lobbyManager;
            _relayManager = relayManager;
        }

        public async UniTask StartHostAsync()
        {
            var joinCode = await _relayManager.CreateRelayAsync();

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            #if UNITY_WEBGL && !UNITY_EDITOR
                transport.UseWebSockets = true;
            #endif

            transport.SetRelayServerData(_relayManager.GetHostRelayData());
            await _lobbyManager.SetRelayCodeAsync(joinCode);
            NetworkManager.Singleton.StartHost();
            IsHost = true;
        }

        public async UniTask StartClientAsync(string joinCode)
        {
            await _relayManager.JoinRelayAsync(joinCode);

            var nm = NetworkManager.Singleton;
            var transport = nm.GetComponent<UnityTransport>();

            #if UNITY_WEBGL && !UNITY_EDITOR
                transport.UseWebSockets = true;
            #endif

            transport.SetRelayServerData(_relayManager.GetClientRelayData());
            await UniTask.Delay(200);
            nm.StartClient();
            IsClient = true;
        }

        public async UniTask Disconnect()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                await UniTask.WaitUntil(() => !NetworkManager.Singleton.IsListening);
            }

            IsHost = false;
            IsClient = false;
        }
    }
}