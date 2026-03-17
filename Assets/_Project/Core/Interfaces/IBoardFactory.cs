using Core.Domain;

namespace Core.Interfaces
{
    public interface IBoardFactory
    {
        Board CreateRandom();
    }
}