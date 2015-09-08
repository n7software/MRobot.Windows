using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using log4net;
using MRobot.Civilization.Save;
using MRobot.Windows.Data;
using MRobot.Windows.Models;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.GameLogic
{
    public class GameSaveBroker
    {
        #region Fields

        private const int SecondsToWaitBeforeTransferRetry = 10;

        private readonly ILog Log = LogManager.GetLogger("GameSaveBroker");
        private readonly LocalGameSaveManager _localSaveManager;
        
        #endregion

        #region Constructors

        public GameSaveBroker(LocalGameSaveManager localSaveManager)
        {
            if (localSaveManager == null) { throw new ArgumentNullException("localSaveManager"); }

            _localSaveManager = localSaveManager;

            CurrentSaveTransfers.CollectionChanged +=
                    (sender, args) => AppMenuItems.TransfersMenu.Count = CurrentSaveTransfers.Count;
        }

        #endregion

        #region Public Fields and Properties

        public static readonly ObservableCollection<SaveTransfer> CurrentSaveTransfers = new ObservableCollection<SaveTransfer>(); 

        #endregion

        #region Public Methods

        public void DownloadSaveIfAvailable(Game game)
        {
            if (HasAvailableGameSave(game)
                && IsGameSaveNewerThanDownloadedVersion(game))
            {
                DownloadGameSave(game);
            }
        }

        public void SubmitGameSaveToServer(Game game, LocalGameSave localSave)
        {
            AddNewSaveTransfer(
                new SaveTransfer
                {
                    Game = game,
                    IconVisual = Application.Current.FindResource("TransfersIcon") as Visual
                });

            var memStream = new MemoryStream();
            localSave.GameSave.Save(memStream);
            memStream.Position = 0;

            var progressStream = new ProgressStream(memStream);
            progressStream.BytesRead += (sender, args) => OnGameSaveUploadBytesRead(game.Id, args);

            App.GameHub.UploadSave(progressStream)
                .ContinueWith(t =>
                {
                    var saveTransfer = CurrentSaveTransfers.FirstOrDefault(st => st.GameId == game.Id);
                    if (saveTransfer != null)
                    {
                        if (t.IsFaulted)
                        {
                            Log.Error(string.Format("Uploading save for game #{0}", game.Id), t.Exception);
                            saveTransfer.IsFailed = true;
                            RetrySaveTransferAfterWait(saveTransfer, () => SubmitGameSaveToServer(game, localSave));
                        }
                        else
                        {
                            UpdateSaveTransfer(saveTransfer, 100);

                            _localSaveManager.ArchiveFile(localSave);

                            SaveUploadResult uploadResult = t.Result;
                            if (uploadResult != null)
                            {
                                //TODO: Show message from server
                            }
                        }
                    }

                    memStream.Close();
                    memStream.Dispose();
                });
        }

        #endregion

        #region Private Methods

        private void AddNewSaveTransfer(SaveTransfer transfer)
        {
            Application.Current.Dispatcher.Invoke(() => CurrentSaveTransfers.Add(transfer));
        }

        private void OnFileTransferCompleted(Game game, string localFilePath, Exception exception)
        {
            if (exception == null)
            {
                UpdateSaveTransfer(game.Id, 100);

                _localSaveManager.AddLocalSave(game, localFilePath);
            }
            else
            {
                Log.Error("Failed to transfer save.", exception);

                var existingTransfer = GetExistingTransfer(game.Id);
                if (existingTransfer != null)
                {
                    existingTransfer.IsFailed = true;
                    RetrySaveTransferAfterWait(existingTransfer, () => DownloadSaveIfAvailable(game));
                }
            }
        }

        private void OnFileTransferProgressChanged(int gameId, int progressPercentage)
        {
            UpdateSaveTransfer(gameId, progressPercentage);
        }

        private Uri CreateSaveDownloadUri(Game game)
        {
            return new Uri(string.Format("https://mrobot.blob.core.windows.net/saves/{0}/{1}",
                game.LastTurn.SaveId, GameManager.CreateSaveName(game)));
        }

        private bool HasAvailableGameSave(Game game)
        {
            return game.LastTurn != null && game.LastTurn.SaveId.HasValue;
        }

        public void DownloadGameSave(Game game)
        {
            Uri onlineSaveUrl = CreateSaveDownloadUri(game);
            string localSavePath = _localSaveManager.CreateLocalSaveFilePath(game);

            var existingTransfer = GetExistingTransfer(game.Id);
            if (existingTransfer != null)
            {
                if (existingTransfer.IsFailed)
                {
                    existingTransfer.IsFailed = false;
                    existingTransfer.ProgressPercentage = 0;
                }
                else
                {
                    Log.DebugFormat("Already downloading Game #{0}", game.Id);
                    return; // We're already downloading it!
                }
            }
            else
            {
                var transfer = new SaveTransfer
                {
                    Game = game,
                    IconVisual = Application.Current.FindResource("TransfersIcon") as Visual
                };
                AddNewSaveTransfer(transfer);
            }

            var webClient = new WebClient();
            webClient.DownloadProgressChanged +=
                (sender, args) => OnFileTransferProgressChanged(game.Id, args.ProgressPercentage);
            webClient.DownloadFileCompleted += (sender, args) => OnFileTransferCompleted(game, localSavePath, args.Error);
            webClient.DownloadFileAsync(onlineSaveUrl, localSavePath);
        }

        private bool IsGameSaveNewerThanDownloadedVersion(Game game)
        {
            LocalSaveFile localSave = _localSaveManager.GetLocalSaveFile(game.Id);

            return localSave == null || game.LastTurn.LastModifiedSave > localSave.DownloadedAt;
        }

        private void OnGameSaveUploadBytesRead(int gameId, ProgressStreamReportEventArgs args)
        {
            var saveTransfer = CurrentSaveTransfers.FirstOrDefault(st => st.GameId == gameId);
            if (saveTransfer != null)
            {
                double progress = ((double)args.StreamPosition / (double)args.StreamLength) * 100.0;
                progress = (progress > 99.0) ? 99.0 : progress;

                UpdateSaveTransfer(saveTransfer, (int)progress);
            }
        }

        private void UpdateSaveTransfer(int gameId, int progressPercentage)
        {
            var existingTransfer = GetExistingTransfer(gameId);
            if (existingTransfer != null)
            {
                UpdateSaveTransfer(existingTransfer, progressPercentage);
            }
        }
        private void UpdateSaveTransfer(SaveTransfer saveTransfer, int progressPercentage)
        {
            if (progressPercentage > saveTransfer.ProgressPercentage)
            {
                saveTransfer.ProgressPercentage = progressPercentage;
            }

            if (progressPercentage >= 100)
            {
                Application.Current.Dispatcher.Invoke(() => CurrentSaveTransfers.Remove(saveTransfer));
            }
        }

        private void RetrySaveTransferAfterWait(SaveTransfer transfer, Action retryAction)
        {
            Task.Run(() =>
            {
                transfer.SecondsUntilRetry = SecondsToWaitBeforeTransferRetry;

                do
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    transfer.SecondsUntilRetry--;
                } while (transfer.SecondsUntilRetry > 0);
            })
            .ContinueWith(t => retryAction());
        }

        private static SaveTransfer GetExistingTransfer(int gameId)
        {
            return CurrentSaveTransfers.FirstOrDefault(st => st.GameId == gameId);
        }
        #endregion
    }
}
