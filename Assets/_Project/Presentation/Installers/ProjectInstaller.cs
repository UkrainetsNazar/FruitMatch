using Core.Interfaces;
using Data.Services;
using Infrastructure.Network;
using Zenject;

namespace Core.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<AuthManager>().AsSingle();
            Container.Bind<IGameStateService>().To<GameStateService>().AsSingle();
        }
    }
}