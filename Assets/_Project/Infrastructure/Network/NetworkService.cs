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

            await _lobbyManager.SetRelayCodeAsync(joinCode);

            var transport = NetworkManager.Singleton
                .GetComponent<UnityTransport>();

            transport.SetRelayServerData(_relayManager.GetHostRelayData());

            NetworkManager.Singleton.StartHost();
            IsHost = true;

            Debug.Log("Host started");
        }

        public async UniTask StartClientAsync(string joinCode)
        {
            await _relayManager.JoinRelayAsync(joinCode);

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                Debug.LogError("NetworkManager is NULL");
                return;
            }

            var transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport is NULL");
                return;
            }

            var relayData = _relayManager.GetClientRelayData();

            transport.SetRelayServerData(relayData);

            nm.StartClient();
            IsClient = true;

            Debug.Log("Client started");
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