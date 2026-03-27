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

        public async UniTask<string> CreateRelayAsync(int maxConnections = 1)
        {
            try
            {
                _hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(_hostAllocation.AllocationId);

                return joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.Log("CreateRelay failed: " + e.Message);
                throw;
            }
        }

        public async UniTask JoinRelayAsync(string joinCode)
        {
            try
            {
                _clientAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
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

            return new RelayServerData(_hostAllocation, "dtls");
        }

        public RelayServerData GetClientRelayData()
        {
            if (_clientAllocation == null)
                throw new Exception("Client allocation wasn't created");

            return new RelayServerData(_clientAllocation, "dtls");
        }
    }
}