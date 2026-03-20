using UnityEngine;

namespace Core.Pathfinding
{
    public class PathNode
    {
        public Vector2Int Position { get; set; }
        public float GCost { get; set; }
        public float HCost { get; set; }
        public float FCost => GCost + HCost;
        public PathNode Parent { get; set; }

        public PathNode(Vector2Int position)
        {
            Position = position;
        }
    }
}