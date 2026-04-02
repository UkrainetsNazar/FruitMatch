using System;
using System.Collections.Generic;
using Core.Domain;
using Unity.Netcode;
using UnityEngine;

namespace Infrastructure.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        public event Action<Vector2Int, Vector2Int, string> OnMoveReceived;
        public event Action<List<Vector2Int>> OnMatchesProcessed;
        public event Action<List<FruitMovement>> OnGravityApplied;
        public event Action<string> OnTurnChanged;
        public event Action OnGameStarted;
        public event Action<int, int> OnBoardDataReceived;
        public event Action<Vector2Int, Vector2Int> OnSwapReceived;
        public event Action<string, int, int> OnStatsReceived;


        // ── Клієнт → Хост ────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        public void SendMoveServerRpc(
            Vector2Int from,
            Vector2Int to,
            string senderId)
        {
            OnMoveReceived?.Invoke(from, to, senderId);
        }

        // ── Хост → всі клієнти ───────────────────────────────

        [ClientRpc]
        public void BroadcastMatchesClientRpc(Vector2Int[] destroyed)
        {
            OnMatchesProcessed?.Invoke(new List<Vector2Int>(destroyed));
        }

        [ClientRpc]
        public void BroadcastGravityClientRpc(FruitMovementData[] movements)
        {
            var list = new List<FruitMovement>();
            foreach (var m in movements)
                list.Add(new FruitMovement
                {
                    From = m.From,
                    To = m.To,
                    Path = new List<Vector2Int>(m.Path),
                    SyncFruitType = m.NewFruitType
                });

            OnGravityApplied?.Invoke(list);
        }

        [ClientRpc]
        public void BroadcastTurnClientRpc(string playerId)
        {
            OnTurnChanged?.Invoke(playerId);
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
        public void UpdatePlayerStatsClientRpc(string playerId, int score, int moves)
        {
            OnStatsReceived?.Invoke(playerId, score, moves);
        }
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