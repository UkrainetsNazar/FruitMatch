using Core.Interfaces;
using UnityEngine;
using Zenject;

namespace Presentation.Views
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private GameObject _brightCellPrefab;
        [SerializeField] private GameObject _darkCellPrefab;
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private float _cellSpacing = 0.1f;

        [Inject] private IMatchBoard _matchBoard;

        private void Start()
        {
            BuildBoard();
        }

        private void BuildBoard()
        {
            var board = _matchBoard.CurrentBoard;

            float totalWidth  = board.Width  * (_cellSize + _cellSpacing) - _cellSpacing;
            float totalHeight = board.Height * (_cellSize + _cellSpacing) - _cellSpacing;

            var startPos = new Vector2(
                -totalWidth  / 2f + _cellSize / 2f,
                -totalHeight / 2f + _cellSize / 2f
            );

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = board.GetCell(new Vector2Int(x, y));

                    if (!cell.IsUsable) continue;

                    var worldPos = new Vector3(
                        startPos.x + x * (_cellSize + _cellSpacing),
                        startPos.y + y * (_cellSize + _cellSpacing),
                        0f
                    );

                    if ((x + y) % 2 == 0)
                    {
                        var cellGO = Instantiate(_brightCellPrefab, worldPos, Quaternion.identity, transform);
                        cellGO.name = $"Cell ({x},{y})";
                    }
                    else
                    {
                        var cellGO = Instantiate(_darkCellPrefab, worldPos, Quaternion.identity, transform);
                        cellGO.name = $"Cell ({x},{y})";
                    }
                }
            }
        }
    }
}