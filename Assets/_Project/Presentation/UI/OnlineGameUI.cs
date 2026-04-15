using System.Linq;
using Infrastructure.Audio;
using Infrastructure.Network;
using Presentation.Animations;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Presentation.UI
{
    public class OnlineGameUI : BaseGameUI
    {
        [SerializeField] protected PanelAnimator onlineGamePanel;
        [SerializeField] protected TMP_Text turnText;
        [SerializeField] private TMP_Text opponentScore, opponentMoves;
        [SerializeField] private TMP_Text resultText, finalOpponentScore;
        [SerializeField] private TMP_Text playerMultiplier, opponentMultiplier;

        [InjectOptional] private NetworkGameManager _network;

        void Awake()
        {
            if (_network != null)
            {
                _network.OnTurnChanged += HandleTurnChanged;
                _gameState.OnComboAchieved += OnComboAchieved;
            }
        }

        protected override void Start()
        {
            if (_network != null)
            {
                base.Start();
                if (onlineGamePanel != null) onlineGamePanel.Show();
                _gameState.OnGameFinished += OnGameFinished;
            }
            else
            {
                enabled = false;
                return;
            }

            _returnButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Lobby");
            });
        }

        protected override void Update()
        {
            base.Update();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_network != null)
                _network.OnTurnChanged -= HandleTurnChanged;
            if (_gameState != null)
            {
                _gameState.OnGameFinished -= OnGameFinished;
                _gameState.OnComboAchieved -= OnComboAchieved;
            }
        }

        private void OnGameFinished(int finalScore)
        {
            ShowResult();
        }

        private void ShowResult()
        {
            string myId = NetworkManager.Singleton.LocalClientId.ToString();
            var me = _gameState.GetPlayerData(myId);

            var opponentId = NetworkManager.Singleton.ConnectedClientsIds
                .Select(id => id.ToString())
                .FirstOrDefault(id => id != myId);
            var op = opponentId != null ? _gameState.GetPlayerData(opponentId) : null;

            bool isDraw = op == null || me.Score == op.Score;
            bool iWon = op != null && me.Score > op.Score;

            resultText.text = isDraw ? "DRAW!" : iWon ? "YOU WIN!" : "YOU LOSE!";
            resultText.color = isDraw ? Color.yellow : iWon ? Color.green : Color.red;
            if (!iWon)
                AudioManager.PlayLoseGame();
            else
                AudioManager.PlayWinGame();

            finalPlayerScore.text = $"{me.Score}";
            if (finalOpponentScore != null && op != null)
                finalOpponentScore.text = $"{op.Score}";

            resultPanel.Show();
            
            onlineGamePanel.Hide();
        }

        private void HandleTurnChanged(string id, Vector2Int arg2, Vector2Int arg3)
        {
            UpdateTurnDisplay(id);
        }

        private void UpdateTurnDisplay(string activePlayerId)
        {
            bool isMyTurn = activePlayerId == NetworkManager.Singleton.LocalClientId.ToString();
            turnText.text = isMyTurn ? "YOUR TURN" : "OPPONENT'S TURN";
            turnText.color = isMyTurn ? Color.green : Color.red;
        }

        private void OnComboAchieved(string playerId, int combo)
        {
            string myId = NetworkManager.Singleton.LocalClientId.ToString();
            var target = playerId == myId ? playerMultiplier : opponentMultiplier;
            ShowMultiplierOn(target, combo);
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
    }
}