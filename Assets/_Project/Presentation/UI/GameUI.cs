using System.Linq;
using Infrastructure.Network;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Presentation.UI
{
    public class OnlineGameUI : BaseGameUI
    {
        [SerializeField] protected GameObject waitingPanel;
        [SerializeField] protected GameObject onlineGamePanel;
        [SerializeField] protected TMP_Text turnText;
        [SerializeField] private TMP_Text opponentScore, opponentMoves;
        [InjectOptional] private NetworkGameManager _network;

        protected override void Start()
        {
            if (_network != null)
            {
                base.Start();
                if (onlineGamePanel != null) onlineGamePanel.SetActive(true);
                _network.OnGameStarted += OnGameStarted;
                _network.OnTurnChanged += UpdateTurnDisplay;
            }
            else enabled = false;
        }

        private void UpdateTurnDisplay(string activePlayerId)
        {
            bool isMyTurn = activePlayerId == NetworkManager.Singleton.LocalClientId.ToString();
            turnText.text = isMyTurn ? "YOUR TURN" : "OPPONENT'S TURN";
            turnText.color = isMyTurn ? Color.green : Color.red;
        }

        protected override void RefreshUI()
        {
            ulong myIdRaw = NetworkManager.Singleton.LocalClientId;
            ulong opponentIdRaw = NetworkManager.Singleton.ConnectedClientsIds.FirstOrDefault(id => id != myIdRaw);

            var me = _gameState.GetPlayerData(myIdRaw.ToString());
            playerScore.text = $"{me.Score}";
            playerMoves.text = $"Moves: {me.MovesLeft}";

            if (opponentIdRaw != 0)
            {
                var op = _gameState.GetPlayerData(opponentIdRaw.ToString());
                opponentScore.text = $"{op.Score}";
                opponentMoves.text = $"Moves: {op.MovesLeft}";
            }
        }

        private void OnGameStarted() => waitingPanel.SetActive(false);
    }
}