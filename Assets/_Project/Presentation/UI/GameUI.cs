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

        void Awake()
        {
            if (_network != null)
            {
                _network.OnGameStarted += OnGameStarted;
                _network.OnTurnChanged += (id, _, _) => UpdateTurnDisplay(id);
            }
        }

        protected override void Start()
        {
            if (_network != null)
            {
                base.Start();
                if (onlineGamePanel != null) onlineGamePanel.SetActive(true);
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
            string myId = NetworkManager.Singleton.LocalClientId.ToString();

            var me = _gameState.GetPlayerData(myId);
            playerScore.text = $"{me.Score}";
            playerMoves.text = $"Moves: {me.MovesLeft}";

            var opponentId = NetworkManager.Singleton.ConnectedClientsIds
                .Select(id => id.ToString())
                .FirstOrDefault(id => id != myId);

            if (!string.IsNullOrEmpty(opponentId))
            {
                var op = _gameState.GetPlayerData(opponentId);
                opponentScore.text = $"{op.Score}";
                opponentMoves.text = $"Moves: {op.MovesLeft}";
            }
        }

        private void OnGameStarted() => waitingPanel.SetActive(false);
    }
}