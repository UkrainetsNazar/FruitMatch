using System.Linq;
using Core.Domain.Enums;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using Infrastructure.Utils;
using Presentation.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class MultiplayerHostController : IGameController
    {
        private readonly IMatchBoard _matchBoard;
        private readonly IBoardView _boardView;
        private readonly IFruitFactory _fruitFactory;
        private readonly IBoardFactory _boardFactory;
        private readonly PreviewManager _previewManager;
        private readonly IGameStateService _gameState;
        private readonly NetworkGameManager _network;
        private readonly HintSystem _hint;
        private readonly TurnManager _turnManager;
        private readonly ScoreTracker _scoreTracker;
        private readonly MatchProcessor _matchProcessor;

        private bool _clientBoardReady;
        private string _localPlayerId => NetworkManager.Singleton.LocalClientId.ToString();

        public MultiplayerHostController(IFruitFactory fruitFactory, IMatchBoard matchBoard,
            IBoardFactory boardFactory, IBoardView boardView, PreviewManager previewManager,
            IGameStateService gameState, NetworkGameManager network)
        {
            _fruitFactory = fruitFactory;
            _matchBoard = matchBoard;
            _boardView = boardView;
            _boardFactory = boardFactory;
            _previewManager = previewManager;
            _gameState = gameState;
            _network = network;

            _hint = new HintSystem(_boardView, _matchBoard);
            _turnManager = new TurnManager(network, matchBoard);
            _scoreTracker = new ScoreTracker(network, gameState);
            _matchProcessor = new MatchProcessor(matchBoard, boardView, gameState, network);

            _network.OnMoveReceived += OnMoveReceived;
            _network.OnClientBoardReady += () => _clientBoardReady = true;
        }

        public async UniTask StartGame()
        {
            _gameState.SetPhase(GamePhase.Lobby);
            await WaitForPlayersAsync();

            int shapeChoice = PlayerPrefs.GetInt("LobbyShapeChoice", -1);
            int fruitCount = PlayerPrefs.GetInt("LobbyFruitCount", 7);

            _fruitFactory?.SetFruitTypeCount(fruitCount);
            _network.BroadcastGameSettingsClientRpc(fruitCount);

            var playerIds = NetworkManager.Singleton.ConnectedClients.Keys
                .Select(k => k.ToString());
            _scoreTracker.Initialize(playerIds);

            _boardFactory.CreateRandom(out int shapeIndex, out int seed, shapeChoice);
            await UniTask.WaitUntil(() => _boardView.IsInitialized);

            _network.BroadcastBoardDataClientRpc(shapeIndex, seed);
            _network.BroadcastGameStartedClientRpc();
            await UniTask.WaitUntil(() => _clientBoardReady);

            await _matchProcessor.ProcessCascade(_localPlayerId, countScore: false);

            _turnManager.SetInitialTurn(_localPlayerId, _hint);
            _hint.OnTurnStarted(Vector2Int.zero, Vector2Int.zero);
            _gameState.SetPhase(GamePhase.Playing);
        }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (!_turnManager.IsMyTurn) return;
            _hint.OnPlayerActed();

            if (!_matchBoard.TrySwap(from, to))
            {
                await _previewManager.ResetPreview();
                return;
            }

            _network.BroadcastSwapClientRpc(from, to);
            await _boardView.PlaySwap(from, to);

            await ProcessAndFinalizeTurn();
            _turnManager.SwitchTurn(_hint);
            _gameState.SetPhase(_turnManager.IsMyTurn ? GamePhase.Playing : GamePhase.Paused);
        }

        private void OnMoveReceived(Vector2Int from, Vector2Int to, string senderId)
        {
            if (_turnManager.CurrentTurnPlayerId != senderId) return;
            HandleRemoteMove(from, to, senderId).Forget();
        }

        private async UniTaskVoid HandleRemoteMove(Vector2Int from, Vector2Int to, string senderId)
        {
            if (!_matchBoard.TrySwap(from, to))
            {
                _network.NotifySwapFailedClientRpc();
                return;
            }

            _network.BroadcastSwapClientRpc(from, to);
            await _boardView.PlaySwap(from, to);
            await ProcessAndFinalizeTurn();
            _turnManager.SwitchTurn(_hint);
            _gameState.SetPhase(_turnManager.IsMyTurn ? GamePhase.Playing : GamePhase.Paused);
        }

        private async UniTask ProcessAndFinalizeTurn()
        {
            _hint.OnTurnEnded();
            _gameState.SetPhase(GamePhase.Processing);

            string currentPlayer = _turnManager.CurrentTurnPlayerId;
            int totalScore = await _matchProcessor.ProcessCascade(currentPlayer, countScore: true);

            _gameState.SetPhase(GamePhase.Playing);

            _scoreTracker.AddScore(currentPlayer, totalScore);
            _scoreTracker.DecrementMoves(currentPlayer);
            _scoreTracker.SyncToNetwork(currentPlayer);

            if (_scoreTracker.IsGameOver())
            {
                var winnerId = _scoreTracker.DetermineWinner();
                _network.BroadcastGameEndedClientRpc(winnerId);
                await UniTask.Delay(500);
                _gameState.NotifyGameFinished(_gameState.GetPlayerData(_localPlayerId).Score);
                return;
            }

            if (!_matchBoard.HasAnyValidMove())
                await ShuffleAndBroadcast();
        }

        private async UniTask ShuffleAndBroadcast()
        {
            do { _matchBoard.ShuffleBoard(); }
            while (!_matchBoard.HasAnyValidMove());

            var movements = _matchBoard.BuildSpawnMovements();
            _network.BroadcastShuffleClientRpc(NetworkDataUtils.ToNetworkData(movements));
            await _boardView.PlayShuffle(movements);

            await _matchProcessor.ProcessCascade(_turnManager.CurrentTurnPlayerId, countScore: false);
            _turnManager.SetInitialTurn(_turnManager.CurrentTurnPlayerId, _hint);
        }

        private async UniTask WaitForPlayersAsync()
        {
            while (NetworkManager.Singleton.ConnectedClients.Count < 2)
                await UniTask.Delay(100);
        }
    }
}