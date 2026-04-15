using Data.Config;
using Data.Factories;
using Data.Services;
using Core.Interfaces;
using UnityEngine;
using Zenject;
using Presentation.Views;
using Infrastructure.Network;
using Unity.Netcode;
using Presentation.UI;

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
                Container.Bind<IGameController>().To<SinglePlayerController>().AsSingle();
            else if (NetworkManager.Singleton.IsHost)
                Container.Bind<IGameController>().To<MultiplayerHostController>().AsSingle();
            else
                Container.Bind<IGameController>().To<MultiplayerClientController>().AsSingle();

            if (isNetworkGame)
            {
                Container.Bind<NetworkGameManager>().FromComponentInHierarchy().AsSingle();
                Container.Bind<BaseGameUI>().To<OnlineGameUI>().FromComponentInHierarchy().AsSingle();
            }
            else
                Container.Bind<BaseGameUI>().To<SingleGameUI>().FromComponentInHierarchy().AsSingle();

            Container.Bind<IFruitFactory>().To<FruitFactory>().AsSingle();
            Container.Bind<IMatchBoard>().To<MatchBoard>().AsSingle();
            Container.Bind<IBoardFactory>().To<BoardFactory>().AsSingle().WithArguments(_boardShapeConfig);
            Container.Bind<IBoardView>().FromInstance(_boardView).AsSingle();
            Container.Bind<PreviewManager>().FromNew().AsSingle();
            Container.Bind<IGameStateService>().To<GameStateService>().AsSingle();
        }
    }
}