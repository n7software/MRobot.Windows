using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using log4net;
using MRobot.Civilization.Save;
using MRobot.Windows.Data;
using MRobot.Windows.Extensions;
using MRobot.Windows.Models;
using MRobot.Windows.Settings;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.GameLogic
{
    public class GameManager
    {
        #region Fields

        public const string SavedGameExtension = ".Civ5Save";
        private readonly ILog Log = LogManager.GetLogger("GameManager");
        private readonly Dictionary<int, Game> Games = new Dictionary<int, Game>();
        
        #endregion

        #region Constructor

        public GameManager()
        {
            InitializeLocalGameSaveManager();
            InitializeGameSaveBroker();
            RegisterForServerEvents();
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
                        game.GameSlots = (await App.GameHub.GetGameSlots(game.Id)).ToList();
                        game.CurrenTurn = await App.GameHub.GetCurrentTurn(game.Id);
                        game.LastTurn = await App.GameHub.GetLastTurn(game.Id);

                        lock (Games)
                        {
                            Games[game.Id] = game;
                        }
                    })
                .ContinueWith(t => UpdateGamesList())
                .ContinueWith(t => CheckForNewTurns(games.Select(g => g.Id).ToList()));
        }

        private void UpdateGamesList()
        {
            List<Game> games;

            lock (Games)
            {
                games = Games.Values.ToList();
            }

            var newLinks = games.OrderBy(g => !g.IsCurrentUserTurnAndNotSubmitted()).Select(g => new GameLink(g) as Link).ToList();
            newLinks.AddRange(AppMenuItems.GamesMenu.Links.Skip(AppMenuItems.GamesMenu.Links.Count - 2).Take(2));
            AppMenuItems.GamesMenu.Links = newLinks;
        }

        private void CheckForNewTurns(List<int> gameIdsToToast)
        {
            List<Game> games;
            var gamesWithTurn = new List<Game>();

            lock (Games)
            {
                games = Games.Values.ToList();
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
                string toastHeader = string.Format("It's your turn in {0} games.", gamesWithTurn.Count);

                if (gamesWithTurn.Count == 1)
                {
                    toastHeader = string.Format("It's your turn in {0}.", gamesWithTurn.First().Name);
                }

                App.ToastMaker.ShowToast(toastHeader, "Click here to launch Civilization V.", () => CivGameHelper.LaunchGame());
            }
        }

        private Game GetGameFromCache(int gameId)
        {
            lock (Games)
            {
                return Games.ContainsKey(gameId) ? Games[gameId] : null;
            }
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
            lock (Games)
            {
                if (Games.ContainsKey(gameId))
                {
                    Games.Remove(gameId);
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
            }
        }

        public string ReplaceInvalidFileNameChars(string fileName, string replacementChar = "_")
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(fileName, replacementChar);
        }

        #endregion
    }
}
