using Data.Config;
using Data.Factories;
using Data.Services;
using Core.Interfaces;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private BoardShapeConfig _boardShapeConfig;

        public override void InstallBindings()
        {
            Container
                .Bind<IFruitFactory>()
                .To<FruitFactory>()
                .AsSingle();

            Container
                .Bind<IMatchBoard>()
                .To<MatchBoard>()
                .AsSingle();

            Container
                .Bind<IBoardFactory>()
                .To<BoardFactory>()
                .AsSingle()
                .WithArguments(_boardShapeConfig);
        }
    }
}