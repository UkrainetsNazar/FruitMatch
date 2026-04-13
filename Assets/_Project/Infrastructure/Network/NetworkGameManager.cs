using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Unity.Netcode;
using UnityEngine;

namespace Infrastructure.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        public event Action<Vector2Int, Vector2Int, string> OnMoveReceived;
        public event Action<int> OnGameSettingsReceived;
        public event Action<List<Vector2Int>, int> OnMatchesProcessed;
        public event Action<List<FruitMovement>> OnGravityApplied;
        public event Action<string, Vector2Int, Vector2Int> OnTurnChanged;
        public event Action OnGameStarted;
        public event Action<int, int> OnBoardDataReceived;
        public event Action<Vector2Int, Vector2Int> OnSwapReceived;
        public event Action<string, int, int> OnStatsReceived;
        public event Action OnClientBoardReady;
        public event Action OnSwapFailed;
        public event Action<List<FruitMovement>> OnShuffleReceived;
        public event Action OnOpponentDisconnected;
        public event Action<string> OnGameEnded;

        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            bool isLocalPlayer = clientId == NetworkManager.Singleton.LocalClientId;

            if (NetworkManager.Singleton.IsHost)
            {
                if (!isLocalPlayer) OnOpponentDisconnected?.Invoke();
            }
            else
            {
                if (isLocalPlayer) OnOpponentDisconnected?.Invoke();
            }
        }

        // ── Клієнт → Хост ────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        public void SendMoveServerRpc(
            Vector2Int from,
            Vector2Int to,
            string senderId)
        {
            OnMoveReceived?.Invoke(from, to, senderId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendBoardReadyServerRpc(string playerId)
        {
            OnClientBoardReady?.Invoke();
        }

        // ── Хост → всі клієнти ───────────────────────────────

        [ClientRpc]
        public void BroadcastMatchesClientRpc(Vector2Int[] destroyed, int score)
        {
            OnMatchesProcessed?.Invoke(new List<Vector2Int>(destroyed), score);
        }

        [ClientRpc]
        public void BroadcastGravityClientRpc(FruitMovementData[] movements) =>
            OnGravityApplied?.Invoke(ToMovementList(movements));

        [ClientRpc]
        public void BroadcastTurnClientRpc(string playerId, Vector2Int hintFrom, Vector2Int hintTo)
        {
            OnTurnChanged?.Invoke(playerId, hintFrom, hintTo);
        }

        [ClientRpc]
        public void BroadcastGameStartedClientRpc()
        {
            OnGameStarted?.Invoke();

        }

        [ClientRpc]
        public void BroadcastBoardDataClientRpc(int shapeIndex, int seed)
        {
            OnBoardDataReceived?.Invoke(shapeIndex, seed);
        }

        [ClientRpc]
        public void BroadcastSwapClientRpc(Vector2Int from, Vector2Int to)
        {
            OnSwapReceived?.Invoke(from, to);
        }

        [ClientRpc]
        public void NotifySwapFailedClientRpc(ClientRpcParams clientRpcParams = default)
        {
            OnSwapFailed?.Invoke();
        }

        [ClientRpc]
        public void UpdatePlayerStatsClientRpc(string playerId, int score, int moves)
        {
            OnStatsReceived?.Invoke(playerId, score, moves);
        }

        [ClientRpc]
        public void BroadcastShuffleClientRpc(FruitMovementData[] movements) =>
            OnShuffleReceived?.Invoke(ToMovementList(movements));

        [ClientRpc]
        public void BroadcastGameEndedClientRpc(string winnerId)
        {
            OnGameEnded?.Invoke(winnerId);
        }

        [ClientRpc]
        public void BroadcastGameSettingsClientRpc(int fruitCount)
        {
            OnGameSettingsReceived?.Invoke(fruitCount);
        }


        private static List<FruitMovement> ToMovementList(FruitMovementData[] data) =>
        data.Select(m => new FruitMovement
        {
            From = m.From,
            To = m.To,
            Path = new List<Vector2Int>(m.Path),
            SyncFruitType = m.NewFruitType
        }).ToList();
    }

    public struct FruitMovementData : INetworkSerializable
    {
        public Vector2Int From;
        public Vector2Int To;
        public int NewFruitType;
        public Vector2Int[] Path;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref From);
            serializer.SerializeValue(ref To);
            serializer.SerializeValue(ref NewFruitType);

            int length = Path?.Length ?? 0;
            serializer.SerializeValue(ref length);

            if (serializer.IsReader)
                Path = new Vector2Int[length];

            for (int i = 0; i < length; i++)
                serializer.SerializeValue(ref Path[i]);
        }
    }
}