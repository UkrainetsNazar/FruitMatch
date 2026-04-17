using System.Collections.Generic;
using Core.Systems.Pathfinding;
using UnityEngine;

namespace Data.Services
{
    public class AStarPathfinder
    {
        BoardGraph _pathfindingGraph;

        public AStarPathfinder(BoardGraph pathfindingGraph)
        {
            _pathfindingGraph = pathfindingGraph;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var open = new List<PathNode> { new(start) };
            var closed = new List<PathNode>();

            while (open.Count > 0)
            {
                var current = GetLowestFCost(open);

                if (current.Position == goal)
                    return BuildPath(current);


                open.Remove(current);
                closed.Add(current);

                foreach (var neighbourPos in _pathfindingGraph.GetNeighbours(current.Position))
                {
                    if (IsInClosed(closed, neighbourPos))
                        continue;

                    if (!_pathfindingGraph.IsWalkable(neighbourPos))
                        continue;

                    float newGCost = current.GCost + _pathfindingGraph.GetCost(current.Position, neighbourPos);

                    var neighbourNode = GetFromOpen(open, neighbourPos);

                    if (neighbourNode == null)
                    {
                        neighbourNode = new PathNode(neighbourPos)
                        {
                            GCost = newGCost,
                            HCost = Heuristic(neighbourPos, goal),
                            Parent = current
                        };
                        open.Add(neighbourNode);
                    }
                    else if (newGCost < neighbourNode.GCost)
                    {
                        neighbourNode.GCost = newGCost;
                        neighbourNode.Parent = current;
                    }
                }
            }

            return null;
        }

        private PathNode GetLowestFCost(List<PathNode> list)
        {
            var lowest = list[0];
            foreach (var node in list)
                if (node.FCost < lowest.FCost)
                    lowest = node;

            return lowest;
        }

        private List<Vector2Int> BuildPath(PathNode target)
        {
            var path = new List<Vector2Int>();
            var current = target;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private bool IsInClosed(List<PathNode> closedList, Vector2Int nodePos)
        {
            foreach (var closed in closedList)
                if (closed.Position == nodePos)
                    return true;

            return false;
        }

        private PathNode GetFromOpen(List<PathNode> open, Vector2Int pos)
        {
            foreach (var node in open)
                if (node.Position == pos) return node;
            return null;
        }

        private float Heuristic(Vector2Int from, Vector2Int goal)
        {
            return Mathf.Abs(from.x - goal.x) + Mathf.Abs(from.y - goal.y);
        }
    }
}