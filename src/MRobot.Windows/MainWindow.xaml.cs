using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using log4net;
using MRobot.Windows.GameLogic;
using MRobot.Windows.Models;
using MRobot.Windows.TaskTray;
using MRobot.Windows.Utilities;

namespace MRobot.Windows
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Commands;
    using Data;
    using Extensions;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields

        private const int CornerMargin = 15;
        private const int ShowAnimationDurationMs = 100;
        private const int UpdateCheckIntervalMiliseconds = 30000;

        private static readonly Size StatusPanelSize = new Size(400, 400);
        private const double AvatarWidthHeight = 64.0;
        private const double MenuWidth = 516.0;
        private ILog Log = LogManager.GetLogger("MainWindow");

        private bool _isClosing = false;
        private bool _isConnected = false;
        private string _avatarUri;
        private string _pendingStatus;
        private bool _isUpdating = false;
        private bool _isRequestingAuthKey = false;

        private TaskTrayManager _taskTrayManager = new TaskTrayManager();

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            WaitForConnection();

            RegisterCommandBindings();
            CheckForUpdatesAsync();

            NamedPipeChannel.BeginServerListen(TaskTrayShared.CommandsPipeName, OnTrayCommandReceived)
                            .ContinueWith(OnTrayCommandServerTaskEnded);

            Loaded += OnLoaded;
        }

        #endregion

        #region Public Properties

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty CurrentMenuProperty = DependencyProperty.Register(
            "CurrentMenu", typeof(MenuItemModel), typeof(MainWindow), new PropertyMetadata(default(MenuItemModel), CurrentMenu_PropertyChangedCallback));

        private static void CurrentMenu_PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var mainWindow = dependencyObject as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.OnPropertyChanged("HasCurrentMenu");
            }
        }

        public MenuItemModel CurrentMenu
        {
            get { return (MenuItemModel)GetValue(CurrentMenuProperty); }
            set { SetValue(CurrentMenuProperty, value); }
        }

        public bool HasCurrentMenu
        {
            get { return CurrentMenu != null; }
        }

        public string AvatarUri
        {
            get { return _avatarUri; }
            set
            {
                _avatarUri = value;
                OnPropertyChanged("AvatarUri");
            }
        }

        public string PendingStatus
        {
            get { return _pendingStatus; }
            set
            {
                _pendingStatus = value;
                OnPropertyChanged("PendingStatus");
                OnPropertyChanged("ShowPendingStatus");
            }
        }

        public bool ShowPendingStatus
        {
            get { return !string.IsNullOrEmpty(PendingStatus); }
        }

        public string AuthKey { get; set; }

        #endregion

        #region Methods

        private void RegisterCommandBindings()
        {
            this.CommandBindings.Add(new CommandBinding(MrCommands.MenuItemHeaderClick, OnMenuItemHeaderClick));
            this.CommandBindings.Add(new CommandBinding(MrCommands.MenuSubLinkClick, OnMenuSubLinkClick));
            this.CommandBindings.Add(new CommandBinding(MrCommands.LaunchCiv, OnLaunchCiv));
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            InitializeTaskTrayManager();

            PositionWindowOnScreen();

            CheckLastUpdateVersion();

            AuthKey = App.LocalSettings.AuthenticationKey;

            SlideInMenu(() => { });
        }
        
        private bool UserWantsToCancelExit()
        {
            return MessageBoxResult.No ==
                   MessageBox.Show(
                       "Are you sure you want to exit the Multiplayer Robot desktop client?",
                       "Exit Multiplayer Robot?",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Question,
                       MessageBoxResult.Yes
                   );
        }

        private void InitializeTaskTrayManager()
        {
            Task.Run(new Action(_taskTrayManager.CopyTrayExecutable))
                .ContinueWith(t => _taskTrayManager.CreateUriScheme())
                .ContinueWith(t => _taskTrayManager.StartTaskTray())
                .ContinueWith(t => StartUpdateTaskTrayIconTask());
        }

        private Task StartUpdateTaskTrayIconTask()
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    Task.Delay(500).Wait();

                    bool showAlert = AppMenuItems.GamesMenu.Count > 0
                                     || AppMenuItems.TransfersMenu.Count > 0;

                    var status = showAlert ? TaskTrayStatus.Alert : TaskTrayStatus.Normal;

                    NamedPipeChannel.SendMessageToServer(TaskTrayShared.StatusPipeName, status.ToString());
                }
            });
        }

        private void ShowWindow()
        {
            if (_isClosing) return;

            if (IsVisible)
            {
                HideWindow();
            }
            else
            {
                this.Show();
                this.Activate();
                this.Focus();

                if (_isConnected && !_isUpdating)
                {
                    MainUiPanel.Visibility = Visibility.Visible;
                    SlideInMenu(() => { });
                }
                else
                {
                    MainUiPanel.Visibility = Visibility.Hidden;
                    SlideInStatus(() => { });
                }
            }
        }

        private void HideWindow()
        {
            HidePopupMenu();
            _isClosing = true;

            Action hideApp = () =>
            {
                this.Hide();
                _isClosing = false;
            };

            if (_isConnected)
            {
                SlideOutMenu(hideApp);
            }
            else
            {
                SlideOutStatus(hideApp);
            }
        }

        private void SlideOutStatus(Action onComplete)
        {
            AnimateStatus(StatusPanelSize, new Size(0, 0), onComplete);
        }

        private void SlideInStatus(Action onComplete)
        {
            AnimateStatus(new Size(0, 0), StatusPanelSize, onComplete);
        }

        private void SlideInMenu(Action onComplete)
        {
            AnimateAvatar(0.0, AvatarWidthHeight,
                () => AnimateMenuItemsWidth(0.0, MenuWidth, onComplete));
        }

        private void SlideOutMenu(Action onComplete)
        {
            AnimateMenuItemsWidth(MenuWidth, 0.0,
                () => AnimateAvatar(AvatarWidthHeight, 0.0, onComplete));
        }

        private void AnimateMenuItemsWidth(double from, double to, Action onComplete)
        {
            var widthAnimation = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(ShowAnimationDurationMs))
            };

            widthAnimation.Completed += (sender, args) => onComplete();

            MenuItemsControl.BeginAnimation(WidthProperty, widthAnimation);
        }

        private void AnimatePopupMenuHeight(double from, double to, int duration, Action onComplete)
        {
            var heightAnimation = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration))
            };

            heightAnimation.Completed += (sender, args) => onComplete();

            PopupMenu.BeginAnimation(HeightProperty, heightAnimation);
        }

        private void AnimateAvatar(double fromSize, double toSize, Action onComplete)
        {
            var widthAnimation = new DoubleAnimation()
            {
                From = fromSize,
                To = toSize,
                Duration = new Duration(TimeSpan.FromMilliseconds(ShowAnimationDurationMs))
            };
            var heightAnimation = new DoubleAnimation()
            {
                From = fromSize,
                To = toSize,
                Duration = new Duration(TimeSpan.FromMilliseconds(ShowAnimationDurationMs))
            };

            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(WidthProperty));
            Storyboard.SetTarget(widthAnimation, Avatar);

            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(HeightProperty));
            Storyboard.SetTarget(heightAnimation, Avatar);

            var storyBoard = new Storyboard();
            storyBoard.Children.Add(widthAnimation);
            storyBoard.Children.Add(heightAnimation);
            storyBoard.Completed += (sender, args) => onComplete();

            storyBoard.Begin();
        }

        private void AnimateStatus(Size fromSize, Size toSize, Action onComplete)
        {
            var widthAnimation = new DoubleAnimation()
            {
                From = fromSize.Width,
                To = toSize.Width,
                Duration = new Duration(TimeSpan.FromMilliseconds(ShowAnimationDurationMs))
            };
            var heightAnimation = new DoubleAnimation()
            {
                From = fromSize.Height,
                To = toSize.Height,
                Duration = new Duration(TimeSpan.FromMilliseconds(ShowAnimationDurationMs))
            };

            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(WidthProperty));
            Storyboard.SetTarget(widthAnimation, StatusPanel);

            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(HeightProperty));
            Storyboard.SetTarget(heightAnimation, StatusPanel);

            var storyBoard = new Storyboard();
            storyBoard.Children.Add(widthAnimation);
            storyBoard.Children.Add(heightAnimation);
            storyBoard.Completed += (sender, args) => onComplete();

            storyBoard.Begin();
        }

        private void AnimateDxVersionSelectWidth(double from, double to, Action onComplete)
        {
            var widthAnimation = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(ShowAnimationDurationMs))
            };

            widthAnimation.Completed += (sender, args) => onComplete();

            DxVersionSelectPanel.BeginAnimation(WidthProperty, widthAnimation);
        }

        private void AttemptToUseAuthKey()
        {
            _isRequestingAuthKey = false;
            AuthTokenDialog.Visibility = Visibility.Collapsed;
            App.LocalSettings.AuthenticationKey = AuthKey.Trim();
            App.AuthenticateWithServer();
            WaitForConnection();
        }

        private void PositionWindowOnScreen()
        {
            var desktopArea = SystemParameters.WorkArea;
            this.Top = desktopArea.Bottom - this.Height - CornerMargin;
            this.Left = desktopArea.Right - this.Width - CornerMargin;
        }

        private Task CheckForUpdatesAsync()
        {
            return Task.Run(() =>
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    Log.Debug("Checking for updates task started.");

                    while (ContinueCheckingForUpdates())
                    {
                        Task.Delay(UpdateCheckIntervalMiliseconds).Wait();
                    }
                }
                else
                {
                    Log.Debug("Instance not deployed from ClickOnce. Won't check for updates.");
                }
            });
        }

        private bool ContinueCheckingForUpdates()
        {
            try
            {
                var deployment = ApplicationDeployment.CurrentDeployment;

                UpdateCheckInfo updateInfo = deployment.CheckForDetailedUpdate();

                if (updateInfo.UpdateAvailable)
                {
                    Log.Debug("New update found, applying now.");

                    ShowUpdatingStatus();

                    PerformUpdate();

                    return false;
                }
            }
            catch (Exception exc)
            {
                Log.Debug("Checking for updates.", exc);
            }

            return true;
        }

        private void ShowUpdatingStatus()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                PendingStatus = "Applying update...";

                if (IsVisible)
                {
                    HidePopupMenu();
                    SlideOutMenu(() => { });
                    SlideInStatus(() => { });
                }
            }));
        }

        private void PerformUpdate()
        {
            if (!_isUpdating)
            {
                _isUpdating = true;

                var deployment = ApplicationDeployment.CurrentDeployment;

                if (deployment.Update())
                {
                    App.Restart();
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideWindow();
            }
        }

        private void OnTrayCommandReceived(string pipName, string message)
        {
            if (message.StartsWith(TaskTrayShared.UriCommandPrefix))
            {
                HandleUriCommand(message);
            }
            else
            {
                HandleTrayCommand(message);
            }
        }

        private void HandleUriCommand(string message)
        {
            var command = message.Replace(TaskTrayShared.UriCommandPrefix, string.Empty);

            if (command.StartsWith(TaskTrayShared.UriAuthKeyCommand))
            {
                HandleUriAuthKey(command.Replace(TaskTrayShared.UriAuthKeyCommand, string.Empty));
            }
            else if (command.StartsWith(TaskTrayShared.UriLaunchCivCommand))
            {
                CivGameHelper.LaunchGame();
            }
            else
            {
                Log.DebugFormat("Recieved unknown Uri command: {0}", command);
            }
        }

        private void HandleUriAuthKey(string authKey)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isRequestingAuthKey)
                {
                    AuthKey = authKey.Trim();
                    AttemptToUseAuthKey();
                    ShowWindow();
                }
            }));
        }

        private void HandleTrayCommand(string message)
        {
            switch (message)
            {
                case TaskTrayShared.ExitCommand:
                    OnTrayExitCommand();
                    break;

                case TaskTrayShared.OpenCommand:
                    OnTrayOpenCommand();
                    break;

                default:
                    Log.DebugFormat("Received unknown task tray command: {0}", message);
                    break;
            }
        }

        private void OnTrayExitCommand()
        {
            if (App.SyncedSettings.ExitPromptEnabled && UserWantsToCancelExit())
            {
                return;
            }

            _taskTrayManager.KillTaskTray();
            Dispatcher.BeginInvoke(new Action(Close));
        }

        private void OnTrayOpenCommand()
        {
            Dispatcher.Invoke(new Action(ShowWindow));
        }

        private void CheckLastUpdateVersion()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                string currentVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                string lastVersion = App.LocalSettings.LastUpdateVersion;

                if (!string.IsNullOrEmpty(lastVersion) && currentVersion != lastVersion)
                {
                    if (App.SyncedSettings.NotifyWhenUpdated)
                    {
                        App.ToastMaker.ShowToast("Client Updated!", String.Format("Version {0}", currentVersion)); 
                    }

                    Log.DebugFormat("Successfully updated to version {0}", currentVersion);
                }

                App.LocalSettings.LastUpdateVersion = currentVersion;
            }
        }

        private void MenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DxVersionSelectPanel.Visibility = Visibility.Hidden;

            var menuItem = (sender as FrameworkElement).DataContext as MenuItemModel;
            if (menuItem != null)
            {
                if (menuItem.IsSelected)
                {
                    HidePopupMenu();
                }
                else
                {
                    if (menuItem.Links.Any() || menuItem.CustomContentTemplate != null)
                    {
                        SelectMenuItem(menuItem);
                        ShowPopupMenu(menuItem);
                    }
                }
            }
        }

        private void SelectMenuItem(MenuItemModel menuItem)
        {
            foreach (var item in AppMenuItems.MenuItems)
            {
                item.IsSelected = false;
            }

            if (menuItem != null)
            {
                menuItem.IsSelected = true;
            }
        }

        private void ShowPopupMenu(MenuItemModel menuItem)
        {
            bool wasAnotherMenuSelected = HasCurrentMenu;

            CurrentMenu = menuItem;

            double menuLeft = 64.0 * menuItem.Position,
                   maxLeft = MenuItemsControl.Width - PopupMenu.Width;

            PopupMenu.Margin = new Thickness(Math.Min(menuLeft, maxLeft), 0.0, 0.0, 0.0);

            if (!wasAnotherMenuSelected)
            {
                AnimatePopupMenuHeight(0.0, 336.0, ShowAnimationDurationMs, () => { });
            }
        }

        private void HidePopupMenu()
        {
            DxVersionSelectPanel.Visibility = Visibility.Hidden;
            AnimatePopupMenuHeight(PopupMenu.ActualHeight, 0.0, ShowAnimationDurationMs,
                () =>
                {
                    CurrentMenu = null;
                    SelectMenuItem(null);
                });
        }

        private void OnLaunchCiv(object sender, ExecutedRoutedEventArgs e)
        {
            if (App.LocalSettings.RememberDxVersion)
            {
                CivGameHelper.LaunchGame();
            }
            else
            {
                ShowCivDxVersionSelector();
            }
        }

        private void OnMenuItemHeaderClick(object sender, ExecutedRoutedEventArgs e)
        {
            var menuItem = ((e.Parameter as MouseButtonEventArgs).Source as FrameworkElement).DataContext as MenuItemModel;

            if (!string.IsNullOrEmpty(menuItem.MainLinkUrl))
            {
                LauncherUtil.LaunchUrlWithDefaultBrowser(menuItem.MainLinkUrl);
            }
        }

        private void OnMenuSubLinkClick(object sender, ExecutedRoutedEventArgs e)
        {
            var link = ((e.Parameter as MouseButtonEventArgs).Source as FrameworkElement).DataContext as Link;

            if (!string.IsNullOrEmpty(link.Url))
            {
                LauncherUtil.LaunchUrlWithDefaultBrowser(link.Url);
            }
            else if (link is ActionLink)
            {
                (link as ActionLink).ActionToPerform();
            }
        }

        private void ShowCivDxVersionSelector()
        {
            DxVersionSelectPanel.Margin = new Thickness(
                0,
                0,
                Width - PopupMenu.Margin.Left - DxVersionSelectPanel.Width,
                MainMenuPanel.Height + PopupMenu.Height - DxVersionSelectPanel.ActualHeight);

            DxVersionSelectPanel.Width = 0;
            DxVersionSelectPanel.Visibility = Visibility.Visible;
            AnimateDxVersionSelectWidth(0, 300, () =>
            {
                DxVersionSelect.Focus();
                Keyboard.Focus(DxVersionSelect);
            });
        }

        private void DxVersionSelectorLaunch_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            DxVersionSelectPanel.Visibility = Visibility.Hidden;
            //GameManager.LaunchGame();
        }

        private void RegisterForDataEvents()
        {
            App.UserHub.UserUpdated += UserHubOnUserUpdated;
        }

        private void OnConnectionEstablished()
        {
            App.UserHub.RefreshCurrentUserInfo();
            App.GameHub.GetUsersGames()
                .ContinueWith(App.GameManager.FinishLoadingGamesTask);
        }

        private void UserHubOnUserUpdated(User user)
        {
            if (user.Id == App.CurrentUserId.ToString())
            {
                RefreshCurrentUser(user);
            }
        }

        private void RefreshCurrentUser(User user)
        {
            AvatarUri = user.AvatarMediumUrl;
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var uri = (sender as Hyperlink).NavigateUri;
            LauncherUtil.LaunchUrlWithDefaultBrowser(uri.ToString());
        }

        private void SubmitAuthKey_OnClick(object sender, RoutedEventArgs e)
        {
            AttemptToUseAuthKey();
        }

        private Task WaitForConnection()
        {
            PendingStatus = "Connecting...";

            return Task.Run(() =>
            {
                while (App.CurrentUserId == 0)
                {
                    Task.Delay(50).Wait();
                }

                PendingStatus = string.Empty;

                // A value of -1 indicates an invalid auth token
                if (App.CurrentUserId == -1)
                {
                    ShowAuthKeyDialog();
                }
                else if (App.CurrentUserId == -2) // A value of -2 indicates a connection error
                {
                    PendingStatus = "Retrying in 5 seconds...";

                    Task.Delay(TimeSpan.FromSeconds(5))
                        .ContinueWith(_ =>
                        {
                            App.ConnectToServer()
                                .ContinueWith(t => App.AuthenticateWithServer(t.Result));
                            WaitForConnection();
                        });
                }
                else // Assumed to be a valid user id and authentication is complete
                {
                    _isConnected = true;

                    RegisterForDataEvents();
                    OnConnectionEstablished();

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (IsVisible)
                        {
                            SlideOutStatus(() =>
                            {
                                MainUiPanel.Visibility = Visibility.Visible;
                                SlideInMenu(() => { });
                            });
                        }
                    }));
                }
            });
        }

        private void ShowAuthKeyDialog()
        {
            _isRequestingAuthKey = true;
            Dispatcher.BeginInvoke(new Action(() => { AuthTokenDialog.Visibility = Visibility.Visible; }));
        }

        private void OnTrayCommandServerTaskEnded(Task t)
        {
            OnTrayExitCommand();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
