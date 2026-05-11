using System;
using Cysharp.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Infrastructure.Network
{
    public class RelayManager
    {
        private Allocation _hostAllocation;
        private JoinAllocation _clientAllocation;

        #if UNITY_WEBGL && !UNITY_EDITOR
            private const string ConnectionType = "udp";
        #else
            private const string ConnectionType = "dtls";
        #endif

        public async UniTask<string> CreateRelayAsync(int maxConnections = 1)
        {
            try
            {
                _hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(_hostAllocation.AllocationId);
                Debug.Log($"[Relay] Created. JoinCode: {joinCode}, Protocol: {ConnectionType}");
                return joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.LogError("CreateRelay failed: " + e.Message);
                throw;
            }
        }

        public async UniTask JoinRelayAsync(string joinCode)
        {
            try
            {
                _clientAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log($"[Relay] Joined. Protocol: {ConnectionType}");
            }
            catch (RelayServiceException e)
            {
                Debug.LogError($"JoinRelay failed: {e.Message}");
                throw;
            }
        }

        public RelayServerData GetHostRelayData()
        {
            if (_hostAllocation == null)
                throw new Exception("Host allocation wasn't created");

            return new RelayServerData(_hostAllocation, ConnectionType);
        }

        public RelayServerData GetClientRelayData()
        {
            if (_clientAllocation == null)
                throw new Exception("Client allocation wasn't created");

            return new RelayServerData(_clientAllocation, ConnectionType);
        }
    }
}