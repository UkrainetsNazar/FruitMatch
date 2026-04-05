using Core.Domain;
using Core.Interfaces;
using Data.Config;
using UnityEngine;

namespace Data.Factories
{
    public class BoardFactory : IBoardFactory
    {
        private readonly BoardShapeConfig _config;
        private readonly IMatchBoard _matchBoard;

        public BoardFactory(BoardShapeConfig config, IMatchBoard matchBoard)
        {
            _config = config;
            _matchBoard = matchBoard;
        }

        public Board CreateRandom()
        {
            int seed = Random.Range(0, int.MaxValue);
            int shapeIndex = Random.Range(0, _config.Shapes.Length);
            var shape = _config.Shapes[shapeIndex];
            var board = new Board(shape.GetMask());
            _matchBoard.Initialize(board, seed);
            return board;
        }

        public Board CreateRandom(out int shapeIndex, out int seed)
        {
            seed = Random.Range(0, int.MaxValue);
            shapeIndex = Random.Range(0, _config.Shapes.Length);
            var shape = _config.Shapes[shapeIndex];
            var board = new Board(shape.GetMask());
            _matchBoard.Initialize(board, seed);
            return board;
        }

        public Board CreateByShape(int shapeIndex, int seed)
        {
            if (shapeIndex < 0 || shapeIndex >= _config.Shapes.Length)
                shapeIndex = 0;

            var shape = _config.Shapes[shapeIndex];
            var board = new Board(shape.GetMask());
            _matchBoard.Initialize(board, seed);
            return board;
        }
    }
}