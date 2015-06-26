using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using log4net;
using MRobot.Windows.TaskTray;
using MRobot.Windows.Utilities;

namespace MRobot.WindowsTaskTray
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ILog Log = LogManager.GetLogger("TrayWindow");
        private TaskTrayStatus _status = TaskTrayStatus.Normal;
        private int _appCommunicationFailureCount = 0;

        public string TaskTrayIconSource
        {
            get
            {
                switch (_status)
                {
                    case TaskTrayStatus.Normal:
                        goto default;

                    case TaskTrayStatus.Alert:
                        return "/Resources/mr-alert.ico";

                    default:
                        return "/Resources/mr.ico";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            TrayIcon.TrayLeftMouseUp += (sender, args) => SendCommandToApp(TaskTrayShared.OpenCommand);

            NamedPipeChannel.BeginServerListen(TaskTrayShared.StatusPipeName, OnStatusMessageReceived);
        }

        private void OnStatusMessageReceived(string pipName, string message)
        {
            if (Enum.TryParse(message, out _status))
            {
                if (_status == TaskTrayStatus.Close)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        Close();
                                        Application.Current.Shutdown();
                                    }));
                }
                else
                {
                    OnPropertyChanged("TaskTrayIconSource");
                }
            }
            else
            {
                Log.DebugFormat("Received unknown status message: {0}", message);
            }
        }

        private void SendCommandToApp(string commandMessage)
        {
            App.AllowMainAppToSetForegroundWindow();

            NamedPipeChannel.SendMessageToServer(TaskTrayShared.CommandsPipeName, commandMessage)
                .ContinueWith(CheckCommunicationResult);
        }

        private void CheckCommunicationResult(Task<bool> t)
        {
            lock (this)
            {
                if (!t.Result)
                {
                    // If we couldn't talk to the app this many times in a row we close
                    if (++_appCommunicationFailureCount > 3)
                    {
                        Dispatcher.BeginInvoke(new Action(Close));
                    }
                }
                else
                {
                    _appCommunicationFailureCount = 0;
                }
            }
        }

        private void TrayMenuExit_Click(object sender, RoutedEventArgs e)
        {
            SendCommandToApp(TaskTrayShared.ExitCommand);
            Close();
        }

        private void TrayMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            SendCommandToApp(TaskTrayShared.OpenCommand);
        }

        private void TrayMenuWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(TaskTrayShared.WebsiteUrl);
        }

        private void TrayMenuDebugLogs_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(TaskTrayShared.GetAppDataDirectoryPath());
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
