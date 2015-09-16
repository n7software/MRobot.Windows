using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MRobot.Windows.Data;
using MRobot.Windows.Extensions;
using MRobot.Windows.GameLogic.Apps;
using MRobot.Windows.Models;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.GameLogic
{
    public class GameManager
    {
        #region Fields

        public const string SavedGameExtension = ".Civ5Save";
        private readonly ILog _log = LogManager.GetLogger("GameManager");
        private readonly Dictionary<int, Game> _games = new Dictionary<int, Game>();

        private DateTime _lastTurnReminderCheck = DateTime.MinValue;

        private readonly CivAppControllerFactory _appControllerFactory = new CivAppControllerFactory();

        #endregion

        #region Constructor

        public GameManager()
        {
            InitializeLocalGameSaveManager();
            InitializeGameSaveBroker();
            RegisterForServerEvents();
            StartTurnReminderTask();
        }

        #endregion

        #region Properties
        private LocalGameSaveManager LocalGameSaveManager { get; set; }
        private GameSaveBroker GameSaveBroker { get; set; }
        #endregion

        #region Public Methods
        public static string GetCivFiveSavesDirectoryPath()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(path, @"My Games\Sid Meier's Civilization 5\Saves\hotseat");

            Directory.CreateDirectory(path);

            return path;
        }

        public void FinishLoadingGamesTask(Task<IEnumerable<Game>> gamesTask)
        {
            var games = gamesTask.Result.ToList();

            GetDataForGamesAsync(games);
        }

        public static string CreateSaveName(Game game)
        {
            return $"(MR) {game.Name}{SavedGameExtension}";
        }

        public void LaunchGame()
        {
            int firstGameType = GetFirstGameType();

            LaunchGame(firstGameType);
        }

        public void LaunchGame(int gameType)
        {
            try
            {
                ICivAppController appController = _appControllerFactory.GetAppControllerByGameType(gameType);
                appController.Launch();
            }
            catch (Exception exc)
            {
                _log.Error($"Launching Civ Game with type {gameType}", exc);
            }
        }
        #endregion

        #region Private Methods

        private void InitializeGameSaveBroker()
        {
            GameSaveBroker = new GameSaveBroker(LocalGameSaveManager);
        }

        private void RegisterForServerEvents()
        {
            App.GameHub.TurnCreated += OnTurnChanged;
            App.GameHub.TurnDeleted += OnTurnChanged;
            App.GameHub.TurnUpdated += OnTurnUpdated;
            App.GameHub.UserJoinedGame += OnUserJoinedGame;
            App.GameHub.UserLeftGame += OnUserLeftGame;
        }

        private void InitializeLocalGameSaveManager()
        {
            IFileSystemWatcher fileWatcher = new EnhancedFileSystemWatcher(GetCivFiveSavesDirectoryPath())
            {
                WatchSubdirectories = false,
                ExtensionWhiteList = new List<string> { ".civ5save" }
            };
            LocalGameSaveManager = new LocalGameSaveManager(fileWatcher);
            LocalGameSaveManager.NewGameSaveDetected += LocalGameSaveManagerOnNewGameSaveDetected;
        }

        private Task GetDataForGameAsync(Game game)
        {
            return GetDataForGamesAsync(new List<Game> { game });
        }

        private Task GetDataForGamesAsync(List<Game> games)
        {
            return games.ForEachAsync(
                    async game =>
                    {
                        try
                        {
                            game.GameSlots = (await App.GameHub.GetGameSlots(game.Id)).ToList();
                            game.CurrenTurn = await App.GameHub.GetCurrentTurn(game.Id);
                            game.LastTurn = await App.GameHub.GetLastTurn(game.Id);

                            lock (_games)
                            {
                                _games[game.Id] = game;
                            }
                        }
                        catch (Exception exc)
                        {
                            _log.Error($"Getting data for Game #{game.Id}", exc);
                        }
                    })
                .ContinueWith(t => UpdateGamesList())
                .ContinueWith(t => CheckForNewTurns(games.Select(g => g.Id).ToList()));
        }

        private void UpdateGamesList()
        {
            List<Game> games;

            lock (_games)
            {
                games = _games.Values.ToList();
            }

            var newLinks = games.OrderBy(g => !g.IsCurrentUserTurnAndNotSubmitted()).Select(g => new GameLink(g) as Link).ToList();
            newLinks.AddRange(AppMenuItems.GamesMenu.Links.Skip(AppMenuItems.GamesMenu.Links.Count - 2).Take(2));
            AppMenuItems.GamesMenu.Links = newLinks;
        }

        private void CheckForNewTurns(List<int> gameIdsToToast)
        {
            List<Game> games;
            var gamesWithTurn = new List<Game>();

            lock (_games)
            {
                games = _games.Values.ToList();
            }

            foreach (var game in games)
            {
                if (game.IsCurrentUserTurnAndNotSubmitted())
                {
                    gamesWithTurn.Add(game);
                    GameSaveBroker.DownloadSaveIfAvailable(game);
                }
                else
                {
                    LocalGameSaveManager.DeleteLocalSaveIfPresent(game);
                }
            }

            if (gameIdsToToast.Any())
            {
                ShowNewTurnsToast(gamesWithTurn.Where(g => gameIdsToToast.Contains(g.Id)).ToList());
            }
        }

        private void ShowNewTurnsToast(List<Game> gamesWithTurn)
        {
            if (gamesWithTurn.Count > 0 && App.SyncedSettings.NotifyNewTurn)
            {
                var toastHeader = $"It's your turn in {gamesWithTurn.Count} games.";

                if (gamesWithTurn.Count == 1)
                {
                    toastHeader = $"It's your turn in {gamesWithTurn.First().Name}.";
                }

                App.ToastMaker.ShowToast(toastHeader, "Click here to launch Civilization V.", LaunchGame);
            }
        }

        private int GetFirstGameType()
        {
            int firstGameType = 0;

            lock (_games)
            {
                if (_games.Any())
                {
                    firstGameType = _games.First().Value.Type;
                }
            }

            return firstGameType;
        }

        private Game GetGameFromCache(int gameId)
        {
            lock (_games)
            {
                return _games.ContainsKey(gameId) ? _games[gameId] : null;
            }
        }

        private void CloseCivIfNecessary(Game game)
        {
            if (ShouldAlwaysCloseGameAfterSave() || ShouldCloseGameAfterBecauseOfLastSave(game))
            {
                CloseCiv(game.Type);
            }
        }

        private void CloseCiv(int gameType)
        {
            try
            {
                ICivAppController appController = _appControllerFactory.GetAppControllerByGameType(gameType);
                appController.AttemptToClose();
            }
            catch (Exception exc)
            {
                _log.Error($"Attempting to close Game Type: {gameType}", exc);
            }
        }

        private bool ShouldCloseGameAfterBecauseOfLastSave(Game game)
        {
            bool result = false;

            if (App.SyncedSettings.AutoCloseCivCondition == AutoCloseCivSettings.NewSaveDetectedNoOtherSaves)
            {
                lock (_games)
                {
                    result = _games.Any(g => g.Key != game.Id && g.Value.Type == game.Type);
                }
            }

            return result;
        }

        private bool ShouldAlwaysCloseGameAfterSave()
        {
            return App.SyncedSettings.AutoCloseCivCondition == AutoCloseCivSettings.NewSaveDetected;
        }

        private void OnTurnChanged(Turn turn)
        {
            var game = GetGameFromCache(turn.GameId);
            if (game != null)
            {
                GetDataForGameAsync(game);
            }
        }

        private void OnTurnUpdated(Turn turn)
        {
            var game = GetGameFromCache(turn.GameId);
            if (game != null)
            {
                if (game.IsCurrentUserTurn()
                    && game.CurrenTurn.Id == turn.Id)
                {
                    if (turn.FinishedAt.HasValue
                        && game.CurrenTurn.FinishedAt == null)
                    {
                        if (turn.DidTurnEarnPoints() && App.SyncedSettings.NotifyPointsEarned)
                        {
                            App.ToastMaker.ShowToast($"You just earned {turn.Points} points!", game.Name);
                        }
                        else if (turn.SubmitType.WasSkipped())
                        {
                            App.ToastMaker.ShowToast("You were just skipped in", game.Name);
                        }
                    }
                    else if (turn.SubmittedAt.HasValue
                        && game.CurrenTurn.SubmittedAt == null
                        && turn.SubmitType == SubmitType.WindowsSubmitted)
                    {
                        if (game.IsCurrentUserTurnAndNotSubmitted())
                        {
                            App.ToastMaker.ShowToast("Submitted GameSave File", game.Name);
                        }
                    }

                    GetDataForGameAsync(game);
                }
            }
        }

        private void OnUserLeftGame(int gameId)
        {
            lock (_games)
            {
                if (_games.ContainsKey(gameId))
                {
                    _games.Remove(gameId);
                }
            }

            UpdateGamesList();
        }

        private async void OnUserJoinedGame(int gameId)
        {
            var newGame = await App.GameHub.GetGame(gameId);
            GetDataForGameAsync(newGame);
        }

        private void LocalGameSaveManagerOnNewGameSaveDetected(object sender, NewGameSaveDetectedArgs args)
        {
            var game = GetGameFromCache(args.GameId);
            if (game != null && game.IsCurrentUserTurnAndNotSubmitted())
            {
                GameSaveBroker.SubmitGameSaveToServer(game, args.Save);
                CloseCivIfNecessary(game);
            }
        }

        public string ReplaceInvalidFileNameChars(string fileName, string replacementChar = "_")
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex($"[{Regex.Escape(regexSearch)}]");
            return r.Replace(fileName, replacementChar);
        }

        private void StartTurnReminderTask()
        {
            Task.Run((Action)TurnReminderThread);
        }

        private void TurnReminderThread()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (App.SyncedSettings.NotifyNewTurn
                        && DateTime.Now.Subtract(_lastTurnReminderCheck).TotalMinutes >= App.SyncedSettings.RepeatTurnNotifyEveryMinutes)
                    {
                        _lastTurnReminderCheck = DateTime.Now;

                        var gameIdsToToast = new List<int>();

                        lock (_games)
                        {
                            gameIdsToToast.AddRange(_games.Keys);
                        }

                        CheckForNewTurns(gameIdsToToast);
                    }
                }
            }
            catch (Exception exc)
            {
                _log.Error("TurnReminderThread", exc);
            }
        }

        #endregion
    }
}
