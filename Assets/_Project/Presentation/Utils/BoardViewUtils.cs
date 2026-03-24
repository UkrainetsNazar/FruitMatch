using UnityEngine;

namespace Presentation.Utils
{
    public class BoardViewUtils
    {
        private readonly float _cellSize;
        private readonly float _cellSpacing;
        private Vector2 _startPos;

        public BoardViewUtils(float cellSize, float cellSpacing)
        {
            _cellSize = cellSize;
            _cellSpacing = cellSpacing;
        }

        public void CalculateStartPos(int boardWidth, int boardHeight)
        {
            float totalWidth = boardWidth * (_cellSize + _cellSpacing) - _cellSpacing;
            float totalHeight = boardHeight * (_cellSize + _cellSpacing) - _cellSpacing;

            _startPos = new Vector2(
                -totalWidth / 2f + _cellSize / 2f,
                -totalHeight / 2f + _cellSize / 2f
            );
        }

        public Vector2 GridToWorld(int x, int y) =>
            new(
                _startPos.x + x * (_cellSize + _cellSpacing),
                _startPos.y + y * (_cellSize + _cellSpacing)
            );

        public Vector2 GridToWorld(Vector2Int pos) =>
            GridToWorld(pos.x, pos.y);

        public Vector2Int WorldToGrid(Vector3 worldPos) =>
            new(
                Mathf.RoundToInt((worldPos.x - _startPos.x) / (_cellSize + _cellSpacing)),
                Mathf.RoundToInt((worldPos.y - _startPos.y) / (_cellSize + _cellSpacing))
            );
    }
}