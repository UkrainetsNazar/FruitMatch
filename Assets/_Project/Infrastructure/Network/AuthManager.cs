using System;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Infrastructure.Network
{
    public class AuthManager
    {
        public string PlayerId { get; private set; }
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

        public async UniTask InitializeAsync()
        {
            try
            {
                var options = new InitializationOptions();

                #if UNITY_EDITOR
                options.SetProfile($"Profile_{Guid.NewGuid().ToString()[..8]}");
                #endif

                await UnityServices.InitializeAsync(options);

                AuthenticationService.Instance.SignedIn += OnSignedIn;
                AuthenticationService.Instance.SignInFailed += OnSignInFailed;

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Auth failed: {e.Message}");
                throw;
            }
        }

        private void OnSignedIn()
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"Signed in: {PlayerId}");
        }

        private void OnSignInFailed(RequestFailedException e)
        {
            Debug.LogError($"Sign in failed: {e.Message}");
        }
    }
}