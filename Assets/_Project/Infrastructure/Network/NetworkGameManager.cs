using System;
using System.Collections.Generic;
using System.IO;
using Core.Domain;
using Unity.Netcode;
using UnityEngine;

namespace Infrastructure.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        public event Action<Vector2Int, Vector2Int, ulong> OnMoveReceived;
        public event Action<List<Vector2Int>> OnMatchesProcessed;
        public event Action<List<FruitMovement>> OnGravityApplied;
        public event Action<ulong> OnTurnChanged;
        public event Action OnGameStarted;
        public event Action<int, int> OnBoardDataReceived;
        public event Action<Vector2Int, Vector2Int> OnSwapReceived;


        // ── Клієнт → Хост ────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        public void SendMoveServerRpc(
            Vector2Int from,
            Vector2Int to,
            ulong senderId)
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

        private List<Vector2Int> BuildPath(Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int> { from };

            int step = to.y < from.y ? -1 : 1;
            for (int y = from.y + step; y != to.y + step; y += step)
                path.Add(new Vector2Int(to.x, y));

            return path;
        }

        [ClientRpc]
        public void BroadcastTurnClientRpc(ulong playerId)
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