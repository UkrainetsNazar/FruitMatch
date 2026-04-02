using Unity.Netcode;
using UnityEngine;

namespace Presentation.UI
{
    public class SingleGameUI : BaseGameUI
    {
        [SerializeField] protected GameObject singleGamePanel;

        protected override void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                enabled = false;
                return;
            }
            
            base.Start();
            if (singleGamePanel != null) singleGamePanel.SetActive(true);

            RefreshUI();
        }

        protected override void RefreshUI()
        {
            var me = _gameState.GetPlayerData("Local");
            playerScore.text = $"{me.Score}";
            playerMoves.text = $"Moves: {me.MovesLeft}";
        }
    }
}