using Data.Config;
using Data.Factories;
using Data.Services;
using Core.Interfaces;
using UnityEngine;
using Zenject;
using Presentation.Views;

namespace Core.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private BoardShapeConfig _boardShapeConfig;
        [SerializeField] private BoardView _boardView;

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

            Container
                .Bind<IGameController>()
                .To<GameController>()
                .AsSingle();

            Container
                .Bind<IBoardView>()
                .FromInstance(_boardView)
                .AsSingle();
        }
    }
}