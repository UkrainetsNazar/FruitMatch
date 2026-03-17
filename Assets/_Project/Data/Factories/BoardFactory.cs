using Core.Interfaces;
using Data.Config;

public class BoardFactory : IBoardFactory
{
    private readonly BoardShapeConfig _config;
    private readonly IMatchBoard _matchBoard;

    public void CreateRandom()
    {
        var shape = _config.GetRandom();
        _matchBoard.Initialize(shape.GetMask());
    }
}