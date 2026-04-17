using Infrastructure.Network;
using Zenject;

namespace Presentation.Installers
{
    public class LobbyInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<LobbyManager>().AsSingle();
            Container.Bind<RelayManager>().AsSingle();
            Container.Bind<NetworkService>().AsSingle();
        }
    }
}