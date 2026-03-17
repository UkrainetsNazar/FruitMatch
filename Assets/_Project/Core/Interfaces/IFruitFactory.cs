using Core.Domain;

namespace Core.Interfaces
{
    public interface IFruitFactory
    {
        Fruit CreateRandom();
        Fruit Create(FruitType type);
    }
}