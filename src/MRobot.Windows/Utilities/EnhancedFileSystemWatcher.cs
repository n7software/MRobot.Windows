using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskExtensions = MRobot.Windows.Extensions.TaskExtensions;

namespace MRobot.Windows.Utilities
{
    using System.IO;
    using System.Threading;
    using log4net;

    public class EnhancedFileSystemWatcher
    {
        #region Properties
        public string PathToWatch { get; set; }
        public bool WatchSubdirectories { get; set; }
        public int PollTimeInSeconds { get; set; }
        public List<string> ExtensionWhiteList { get; set; }

        public event Action<string> ItemAdded = item => { };
        public event Action<string> ItemModified = item => { };
        public event Action<string> ItemRemoved = item => { };

        private readonly ILog Log = LogManager.GetLogger("File Watcher");

        private Thread _pollingThread;
        private FileSystemWatcher _fileSystemWatcher;
        private Dictionary<string, DateTime> _snapshotOfItems;
        private object _lockSnapshot = new object();
        private Dictionary<string, CancellationTokenSource> _handleModifiedCts = new Dictionary<string, CancellationTokenSource>();

        #endregion

        #region Constructor
        public EnhancedFileSystemWatcher(string pathToWatch)
        {
            _snapshotOfItems = new Dictionary<string, DateTime>();
            PathToWatch = Path.GetFullPath(pathToWatch);
            PollTimeInSeconds = 5;
        }
        #endregion

        #region Public Methods

        public void Start()
        {
            Stop();

            StartFileSystemWatcher();
            StartPollingThread();
        }

        public void Stop()
        {
            StopFileSystemWatcher();
            StopPollingThread();
        }
        #endregion

        #region Private Methods

        private void StartPollingThread()
        {
            _pollingThread = new Thread(PollFileSystem)
            {
                IsBackground = true,
                Name = "EnhancedFileSystemWatcher Polling"
            };
            _pollingThread.Start();
        }
        private void StopPollingThread()
        {
            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Abort();
            }

            _pollingThread = null;
        }

        private void StartFileSystemWatcher()
        {
            _fileSystemWatcher = new FileSystemWatcher(PathToWatch)
            {
                InternalBufferSize = 65536, // 64KB, the max we can set
                IncludeSubdirectories = WatchSubdirectories,
                NotifyFilter = NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Size
            };

            _fileSystemWatcher.Changed += FileSystemWatcherOnChanged;
            _fileSystemWatcher.Created += FileSystemWatcherOnCreated;
            _fileSystemWatcher.Deleted += FileSystemWatcherOnDeleted;

            // This actually starts the watcher, weird I know...
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void StopFileSystemWatcher()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Dispose();
                _fileSystemWatcher = null;
            }
        }

        private void PollFileSystem()
        {
            while (true)
            {
                try
                {
                    CheckFilesForChanges();

                    Thread.Sleep(PollTimeInSeconds * 1000);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception exc)
                {
                    Log.Error("Polling directory for changes.", exc);
                }
            }
        }

        private void CheckFilesForChanges()
        {
            var filePathsFound = new HashSet<string>();

            foreach (string fileFullPath in Directory.GetFiles(PathToWatch, "*", WatchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => s.ToLower()))
            {
                filePathsFound.Add(fileFullPath);

                CheckLocalFileForChanges(fileFullPath);
            }

            CheckSnapshotCollectionForRemovedItems(filePathsFound, GetAllItemsFromSnapshot());
        }

        private void CheckLocalFileForChanges(string fileFullPath)
        {
            DateTime? existModifiedTime = GetModifiedTimeFromSnapshot(fileFullPath);

            if (existModifiedTime == null)
            {
                Task.Factory.StartNew(() => HandleItemCreated(fileFullPath));
            }
            else if (existModifiedTime < File.GetLastWriteTimeUtc(fileFullPath))
            {
                Task.Factory.StartNew(() => HandleItemModified(fileFullPath));
            }
        }

        private DateTime? GetModifiedTimeFromSnapshot(string itemPath)
        {
            lock (_lockSnapshot)
            {
                return _snapshotOfItems.ContainsKey(itemPath) ? (DateTime?)_snapshotOfItems[itemPath] : null;
            }
        }

        private void CheckSnapshotCollectionForRemovedItems(HashSet<string> itemPathsFound, IEnumerable<string> existingItemsFromSnapshot)
        {
            foreach (string existingItem in existingItemsFromSnapshot)
            {
                if (!itemPathsFound.Contains(existingItem))
                {
                    Task.Factory.StartNew(() => HandleItemRemoved(existingItem));
                }
            }
        }

        private IEnumerable<string> GetAllItemsFromSnapshot()
        {
            lock (_lockSnapshot)
            {
                return _snapshotOfItems.Keys.ToList();
            }
        }

        private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Task.Factory.StartNew(() => HandleItemCreated(fileSystemEventArgs.FullPath.ToLower()));
        }

        private void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Task.Factory.StartNew(() => HandleItemModified(fileSystemEventArgs.FullPath.ToLower()));
        }

        private void FileSystemWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Task.Factory.StartNew(() => HandleItemRemoved(fileSystemEventArgs.FullPath.ToLower()));
        }

        private void SetItemInCache(string itemPath, DateTime lastModified)
        {
            lock (_lockSnapshot)
            {
                _snapshotOfItems[itemPath.ToLower()] = lastModified;
            }
        }

        private void RemoveItemFromCache(string itemPath)
        {
            lock (_lockSnapshot)
            {
                _snapshotOfItems.Remove(itemPath.ToLower());
            }
        }

        private bool IsPathDirectory(string path)
        {
            try
            {
                return File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldProcessItem(string itemFullPath, FileSystemInfo fileSystemInfo = null)
        {
            bool shouldProcess = true;

            if (fileSystemInfo != null)
            {
                shouldProcess &= !fileSystemInfo.Attributes.HasFlag(FileAttributes.Hidden);
            }

            if (ExtensionWhiteList != null && ExtensionWhiteList.Any())
            {
                shouldProcess &= ExtensionWhiteList.Contains(Path.GetExtension(itemFullPath));
            }

            return shouldProcess;
        }

        private void HandleItemCreated(string itemFullPath)
        {
            try
            {
                bool isDirectory = IsPathDirectory(itemFullPath);

                FileSystemInfo fileSystemInfo = isDirectory ? (FileSystemInfo)new DirectoryInfo(itemFullPath) : (FileSystemInfo)new FileInfo(itemFullPath);

                SetItemInCache(itemFullPath, fileSystemInfo.LastWriteTimeUtc);

                if (ShouldProcessItem(itemFullPath, fileSystemInfo))
                {
                    ItemAdded(itemFullPath);
                }
            }
            catch (Exception exc)
            {
                Log.Error("Handle item created.", exc);
            }
        }

        private void HandleItemRemoved(string itemFullPath)
        {
            try
            {
                RemoveItemFromCache(itemFullPath);

                if (ShouldProcessItem(itemFullPath))
                {
                    ItemRemoved(itemFullPath);
                }
            }
            catch (Exception exc)
            {
                Log.Error("Handle item removed.", exc);
            }
        }

        private void HandleItemModified(string itemFullPath)
        {
            try
            {
                bool isDirectory = IsPathDirectory(itemFullPath);

                FileSystemInfo fileSystemInfo = isDirectory ? (FileSystemInfo)new DirectoryInfo(itemFullPath) : (FileSystemInfo)new FileInfo(itemFullPath);

                SetItemInCache(itemFullPath, fileSystemInfo.LastWriteTimeUtc);

                if (ShouldProcessItem(itemFullPath, fileSystemInfo))
                {
                    if (!_handleModifiedCts.ContainsKey(itemFullPath))
                        _handleModifiedCts[itemFullPath] = null;

                    _handleModifiedCts[itemFullPath] = TaskExtensions.PerformAfterDelayWithThrottling(
                        () => ItemModified(itemFullPath),
                        TimeSpan.FromSeconds(3),
                        _handleModifiedCts[itemFullPath]);
                }
            }
            catch (Exception exc)
            {
                Log.Error("Handle item modified.", exc);
            }
        }
        #endregion
    }
}
