using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using MRobot.Civilization.Save;
using MRobot.Windows.Data;
using MRobot.Windows.Models;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.GameLogic
{
    public class LocalGameSaveManager
    {
        #region Fields

        private const string ArchiveFolderName = "GMR Archive";

        private ILog Log = LogManager.GetLogger("LocalGameSaveManager");
        private EnhancedFileSystemWatcher FileWatcher;
        private string CivVSaveDirectoryPath = null;

        private readonly Dictionary<int, LocalSaveFile> LocalSaveFiles = new Dictionary<int, LocalSaveFile>();

        #endregion

        #region Constructor

        public LocalGameSaveManager()
        {
            InitializeFileSystemWatcher();
        }

        #endregion

        #region Events

        public event EventHandler<NewGameSaveDetectedArgs> NewGameSaveDetected;

        #endregion

        #region Public Methods

        public string CreateLocalSaveFilePath(Game game)
        {
            string savesDirPath = GetSavesDirectoryPath();
            return Path.Combine(savesDirPath, GameManager.CreateSaveName(game));
        }

        public void AddLocalSave(Game game, string localFilePath)
        {
            lock (LocalSaveFiles)
            {
                LocalSaveFiles[game.Id] = new LocalSaveFile
                {
                    GameId = game.Id,
                    PathOnDisk = localFilePath,
                    DownloadedAt = DateTime.UtcNow
                };
                UpdateCurrentTurnCount();
            }
        }

        public void DeleteLocalSaveIfPresent(Game game)
        {
            string saveFilePath = null;

            lock (LocalSaveFiles)
            {
                if (LocalSaveFiles.ContainsKey(game.Id))
                {
                    saveFilePath = LocalSaveFiles[game.Id].PathOnDisk;
                    LocalSaveFiles.Remove(game.Id);
                }
            }

            UpdateCurrentTurnCount();

            if (saveFilePath == null)
            {
                saveFilePath = CreateLocalSaveFilePath(game);
            }

            try
            {
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                }
            }
            catch (Exception exc)
            {
                Log.Error("Deleting local save file.", exc);
            }
        }

        public LocalSaveFile GetLocalSaveFile(int gameId)
        {
            lock (LocalSaveFiles)
            {
                return LocalSaveFiles.ContainsKey(gameId) ? LocalSaveFiles[gameId] : null;
            }
        }

        public void ArchiveFile(LocalGameSave localSave)
        {
            // TODO: Check whether save should be archived
            if (File.Exists(localSave.LocalFilePath))
            {
                try
                {
                    using (var tmpStream = FileOpenUtil.TryGetStream(localSave.LocalFilePath, FileAccess.Write))
                    {
                        tmpStream.Close();
                    }

                    string archiveFileName = string.Format(
                        "MR-{0}_{1}{2}",
                        localSave.GameSave.Name,
                        DateTime.Now.ToString("MMddyy-hhmmss"),
                        GameManager.SavedGameExtension);

                    var archiveFilePath = Path.Combine(GetArchiveFolderPath(), archiveFileName);
                    File.Move(localSave.LocalFilePath, archiveFilePath);
                }
                catch (Exception exc)
                {
                    Log.Debug("Archiving file.", exc);
                }
            }
        }

        #endregion

        #region Private Methods

        private string GetSavesDirectoryPath()
        {
            if (CivVSaveDirectoryPath == null)
            {
                CivVSaveDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                CivVSaveDirectoryPath = Path.Combine(CivVSaveDirectoryPath, @"My Games\Sid Meier's Civilization 5\Saves\hotseat");

                Directory.CreateDirectory(CivVSaveDirectoryPath);
            }

            return CivVSaveDirectoryPath;
        }

        private void OnCivSaveFileModified(string filePath)
        {
            HandleNewFile(filePath);
        }

        private void OnCivSaveFileCreated(string filePath)
        {
            HandleNewFile(filePath);
        }

        private void HandleNewFile(string filePath)
        {
            GameSave civSave = ReadCivSaveFromDisk(filePath);

            if (civSave != null)
            {
                int gameId;
                if (int.TryParse(civSave.Name, out gameId))
                {
                    LocalSaveFile localSave = GetLocalSaveFile(gameId);
                    if (localSave != null
                        && DateTime.UtcNow.Subtract(localSave.DownloadedAt).TotalSeconds > 5)
                    {
                        if (NewGameSaveDetected != null)
                        {
                            NewGameSaveDetected(this, new NewGameSaveDetectedArgs(gameId, new LocalGameSave(civSave, filePath)));
                        }
                    }
                }
            }
        }

        private GameSave ReadCivSaveFromDisk(string filePath)
        {
            GameSave civSave = null;
            using (var fileStream = FileOpenUtil.TryGetStream(filePath, FileAccess.ReadWrite))
            {
                try
                {
                    civSave = GameSave.Load(fileStream, (int)fileStream.Length);
                }
                catch (Exception exc)
                {
                    Log.DebugFormat("GameSave failed parsing. '{0}' - {1}", filePath, exc.Message);
                }

                fileStream.Close();
            }
            return civSave;
        }

        private void InitializeFileSystemWatcher()
        {
            FileWatcher = new EnhancedFileSystemWatcher(GetSavesDirectoryPath())
            {
                WatchSubdirectories = false,
                ExtensionWhiteList = new List<string> { ".civ5save" }
            };
            FileWatcher.ItemAdded += OnCivSaveFileCreated;
            FileWatcher.ItemModified += OnCivSaveFileModified;
            FileWatcher.Start();
        }

        private void UpdateCurrentTurnCount()
        {
            AppMenuItems.GamesMenu.Count = LocalSaveFiles.Count;
        }

        public string GetArchiveFolderPath()
        {
            string archiveFolderPath = Path.Combine(CivVSaveDirectoryPath, ArchiveFolderName);
            Directory.CreateDirectory(archiveFolderPath);

            return archiveFolderPath;
        }

        #endregion
    }

    public class NewGameSaveDetectedArgs : EventArgs
    {
        public int GameId { get; set; }
        public LocalGameSave Save { get; set; }

        public NewGameSaveDetectedArgs(int gameId, LocalGameSave save)
        {
            GameId = gameId;
            Save = save;
        }
    }
}
