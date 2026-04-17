using UnityEngine;

namespace Core.Domain.Entities
{
    public class Cell
    {
        public Vector2Int Position { get; private set; }
        public Fruit Fruit { get; set; }
        public bool IsUsable { get; private set; }

        public Cell(Vector2Int position, bool isUsable = true)
        {
            Position = position;
            IsUsable = isUsable;
        }
    }
}