using System.Collections.Generic;
using System.Linq;
using Core.Domain.ValueObjects;
using Infrastructure.Network;

namespace Infrastructure.Utils
{
    public static class NetworkDataUtils
    {
        public static FruitMovementData[] ToNetworkData(List<FruitMovement> movements) =>
            movements.Select(m => new FruitMovementData
            {
                From = m.From,
                To = m.To,
                Path = m.Path.ToArray(),
                NewFruitType = m.SyncFruitType
            }).ToArray();
    }
}