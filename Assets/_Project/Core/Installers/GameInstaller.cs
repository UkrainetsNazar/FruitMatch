using Data.Config;
using Data.Factories;
using Data.Services;
using Core.Interfaces;
using UnityEngine;
using Zenject;
using Presentation.Views;
using Infrastructure.Network;
using Unity.Netcode;

namespace Core.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private BoardShapeConfig _boardShapeConfig;
        [SerializeField] private BoardView _boardView;

        public override void InstallBindings()
        {
            bool isNetworkGame = NetworkManager.Singleton != null
                      && NetworkManager.Singleton.IsListening;

            if (!isNetworkGame)
            {
                Container.Bind<IGameController>()
                    .To<GameController>().AsSingle();
            }
            else if (NetworkManager.Singleton.IsHost)
            {
                Container.Bind<IGameController>()
                    .To<HostGameController>().AsSingle();
            }
            else
            {
                Container.Bind<IGameController>()
                    .To<ClientGameController>().AsSingle();
            }

            if (isNetworkGame)
            {
                Container
                    .Bind<NetworkGameManager>()
                    .FromComponentInHierarchy()
                    .AsSingle();
            }

            Container.Bind<IFruitFactory>().To<FruitFactory>().AsSingle();
            Container.Bind<IMatchBoard>().To<MatchBoard>().AsSingle();
            Container.Bind<IBoardFactory>().To<BoardFactory>().AsSingle().WithArguments(_boardShapeConfig);
            Container.Bind<IBoardView>().FromInstance(_boardView).AsSingle();
            Container.Bind<PreviewManager>().FromNew().AsSingle();
            Container.Bind<IGameStateService>().To<GameStateService>().AsSingle();
        }
    }
}