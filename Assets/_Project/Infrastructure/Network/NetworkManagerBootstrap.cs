using UnityEngine;

namespace Infrastructure.Network
{
    public class NetworkManagerBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (FindObjectsOfType<NetworkManagerBootstrap>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}