using Core.Domain;

namespace Core.Interfaces
{
    public interface IBoardFactory
    {
        Board CreateRandom();
        Board CreateByShape(int shapeIndex, int seed);
        Board CreateRandom(out int shapeIndex, out int seed);
        Board CreateRandom(out int shapeIndex, out int seed, int shapeChoice);
    }
}