using Core.Domain;
using Core.Interfaces;
using Data.Config;

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
        var shape = _config.GetRandom();
        var mask = shape.GetMask();
        var board = new Board(mask);

        _matchBoard.Initialize(board);

        return board;
    }

    public Board CreateRandom(out int shapeIndex)
    {
        shapeIndex = UnityEngine.Random.Range(0, _config.Shapes.Length);
        var shape = _config.Shapes[shapeIndex];
        var board = new Board(shape.GetMask());
        _matchBoard.Initialize(board);
        return board;
    }
}