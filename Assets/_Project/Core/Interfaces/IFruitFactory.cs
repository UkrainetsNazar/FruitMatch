using Core.Domain;

namespace Core.Interfaces
{
    public interface IFruitFactory
    {
        void SetFruitTypeCount(int count);
        Fruit CreateRandom();
        Fruit Create(FruitType type);
    }
}