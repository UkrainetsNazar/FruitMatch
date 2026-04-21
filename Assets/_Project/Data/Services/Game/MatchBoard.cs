using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.ValueObjects;
using Core.Interfaces;
using UnityEngine;

namespace Data.Services
{
    public class MatchBoard : IMatchBoard
    {
        private Board _board;
        private readonly IFruitFactory _fruitFactory;

        private readonly MatchFinder _matchFinder;
        private readonly GravityProcessor _gravityProcessor;

        public event Action OnBoardInitialized;

        public Board CurrentBoard => _board;

        public MatchBoard(IFruitFactory fruitFactory)
        {
            _fruitFactory = fruitFactory;
            _matchFinder = new MatchFinder();
            _gravityProcessor = new GravityProcessor(fruitFactory);
        }

        public void Initialize(Board board, int seed)
        {
            _board = board;
            UnityEngine.Random.InitState(seed);

            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    Cell cell = _board.GetCell(x, y);
                    if (cell.IsUsable)
                        cell.Fruit = _fruitFactory.CreateRandom();
                }

            OnBoardInitialized?.Invoke();
        }

        public List<MatchResult> FindMatches()
        {
            return _matchFinder.FindMatches(_board);
        }

        public void ProcessMatches(List<MatchResult> matches, int comboMultiplier)
        {
            var toDestroy = new HashSet<Vector2Int>();

            foreach (var match in matches)
            {
                match.ComboMultiplier = comboMultiplier;

                match.Score = match.MatchedPositions.Count * 10 * comboMultiplier;

                foreach (var pos in match.MatchedPositions)
                    toDestroy.Add(pos);
            }

            foreach (var pos in toDestroy)
            {
                Cell cell = _board.GetCell(pos.x, pos.y);
                if (cell != null)
                    cell.Fruit = null;
            }
        }

        public List<FruitMovement> ApplyGravity()
        {
            return _gravityProcessor.Apply(_board, () => _fruitFactory.CreateRandom().Type);
        }

        public List<FruitMovement> ApplyGravityWithTypes(Queue<FruitType> syncedTypes)
        {
            return _gravityProcessor.Apply(_board, () => syncedTypes.Dequeue());
        }

        public int[] ShuffleBoard()
        {
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.GetCell(x, y);
                    if (cell.IsUsable) cell.Fruit = null;
                }

            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.GetCell(x, y);
                    if (cell.IsUsable) cell.Fruit = _fruitFactory.CreateRandom();
                }

            var usable = new List<int>();
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.GetCell(x, y);
                    if (cell.IsUsable) usable.Add((int)cell.Fruit.Type);
                }
            return usable.ToArray();
        }

        public List<FruitMovement> BuildSpawnMovements()
        {
            var movements = new List<FruitMovement>();

            for (int x = 0; x < _board.Width; x++)
            {
                int spawnOffset = 0;
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.GetCell(x, y);
                    if (!cell.IsUsable || cell.Fruit == null) continue;

                    var spawnPoint = new Vector2Int(x, _board.Height + spawnOffset);
                    var target = new Vector2Int(x, y);

                    movements.Add(new FruitMovement
                    {
                        From = spawnPoint,
                        To = target,
                        Path = new List<Vector2Int> { spawnPoint, target },
                        SyncFruitType = (int)cell.Fruit.Type
                    });

                    spawnOffset++;
                }
            }

            return movements;
        }

        public bool TrySwap(Vector2Int from, Vector2Int to)
        {
            if (!_board.IsValid(from) || !_board.IsValid(to)) return false;

            Cell fromCell = _board.GetCell(from.x, from.y);
            Cell toCell = _board.GetCell(to.x, to.y);

            if (!AreNeighbours(from, to)) return false;

            SwapFruits(fromCell, toCell);

            var matches = FindMatches();

            if (matches.Count == 0)
            {
                SwapFruits(fromCell, toCell);
                return false;
            }

            return true;
        }

        public bool HasFruitAt(Vector2Int pos)
        {
            if (!_board.IsValid(pos)) return false;
            var cell = _board.GetCell(pos.x, pos.y);
            return cell.IsUsable && cell.Fruit != null;
        }

        private void SwapFruits(Cell a, Cell b)
        {
            (a.Fruit, b.Fruit) = (b.Fruit, a.Fruit);
        }

        private bool AreNeighbours(Vector2Int a, Vector2Int b)
        {
            var diff = a - b;
            return (Mathf.Abs(diff.x) == 1 && diff.y == 0) ||
                   (Mathf.Abs(diff.y) == 1 && diff.x == 0);
        }

        public bool HasAnyValidMove() => _matchFinder.HasAnyValidMove(_board);

        public (Vector2Int, Vector2Int, int)? FindHint() => _matchFinder.FindBestMove(_board);

        public void ForceSwap(Vector2Int from, Vector2Int to)
        {
            var cellA = _board.GetCell(from.x, from.y);
            var cellB = _board.GetCell(to.x, to.y);
            (cellA.Fruit, cellB.Fruit) = (cellB.Fruit, cellA.Fruit);
        }

        public void SyncGravity(List<FruitMovement> movements)
        {
            var sortedMovements = movements.OrderBy(m => m.To.y).ToList();

            foreach (var move in sortedMovements)
            {
                var targetCell = _board.GetCell(move.To.x, move.To.y);

                if (move.From.y >= _board.Height)
                {
                    targetCell.Fruit = _fruitFactory.Create((FruitType)move.SyncFruitType);
                }
                else
                {
                    var sourceCell = _board.GetCell(move.From.x, move.From.y);
                    targetCell.Fruit = sourceCell.Fruit;
                    sourceCell.Fruit = null;
                }
            }
        }
    }
}