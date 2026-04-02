using System;
using System.Collections.Generic;
using Core.Domain;
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

        public void Initialize(Board board)
        {
            _board = board;

            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    Cell cell = _board.GetCell(x, y);
                    if (cell.IsUsable)
                    {
                        cell.Fruit = _fruitFactory.CreateRandom();
                    }
                }
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
    }
}