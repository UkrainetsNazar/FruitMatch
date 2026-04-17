using System.Collections.Generic;
using Core.Interfaces;
using Core.Systems.Pathfinding;
using UnityEngine;

namespace Data.Services.Helpers
{
    public class AStarHintService
    {
        private readonly IMatchBoard _matchBoard;
        private readonly MatchFinder _matchFinder;

        public AStarHintService(IMatchBoard matchBoard)
        {
            _matchBoard = matchBoard;
            _matchFinder = new MatchFinder();
        }

        public (Vector2Int from, Vector2Int to, List<Vector2Int> path)? FindBestHint()
        {
            var board = _matchBoard.CurrentBoard;
            var best = _matchFinder.FindBestMove(board);

            if (best == null) return null;

            var (from, to, _) = best.Value;

            var graph = new BoardGraph(board);
            var pathfinder = new AStarPathfinder(graph);
            var path = pathfinder.FindPath(from, to);

            if (path == null || path.Count == 0)
                path = new List<Vector2Int> { from, to };

            return (from, to, path);
        }
    }
}