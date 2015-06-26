using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MRobot.Windows.Toasts
{
    using System.Media;
    using log4net;

    /// <summary>
    /// Interaction logic for WpfToastWindow.xaml
    /// </summary>
    public partial class WpfToastWindow : Window
    {
        #region Fields

        private const double DesiredWidth = 400;
        private const int TimeoutMiliseconds = 1000 * 6;
        private const int TimeOutDecrementMiliseconds = 100;
        private const int MilisecondsToFadeOut = 2000;

        private int _milisecondsUntilTimeOut = TimeoutMiliseconds;

        private ILog Log = LogManager.GetLogger("WpfToastWindow");

        #endregion

        #region Constructors
        public WpfToastWindow()
        {
            InitializeComponent();

            Task.Run(new Action(TimeOutTask));

            this.Loaded += OnLoaded;
            this.MouseEnter += (sender, args) =>
            {
                _milisecondsUntilTimeOut = TimeoutMiliseconds;
                this.Opacity = 1.0;
            };
        }

        public WpfToastWindow(string textHeader, string textBody, string imageFilePath)
            : this()
        {
            HeaderText = textHeader;
            BodyText = textBody;
            LoadImage(imageFilePath);
        }
        #endregion

        #region Dependancy Properties

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            "ImageSource", typeof (ImageSource), typeof (WpfToastWindow), new PropertyMetadata(default(ImageSource)));

        public ImageSource ImageSource
        {
            get { return (ImageSource) GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
            "HeaderText", typeof (string), typeof (WpfToastWindow), new PropertyMetadata(default(string)));

        public string HeaderText
        {
            get { return (string) GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty BodyTextProperty = DependencyProperty.Register(
            "BodyText", typeof (string), typeof (WpfToastWindow), new PropertyMetadata(default(string)));

        public string BodyText
        {
            get { return (string) GetValue(BodyTextProperty); }
            set { SetValue(BodyTextProperty, value); }
        }
        #endregion

        #region Events

        public event Action UserActivated;
        public event Action Dismissed;
        public event Action TimedOut;

        #endregion

        #region Methods
        
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            PlayNotificationSound();
            PositionWindowOnScreen();
            AnimateOntoScreen();
        }

        private void AnimateOntoScreen()
        {
            var widthAnimation = new DoubleAnimation()
            {
                From = 0.0,
                To = DesiredWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };
            
            MainPanel.BeginAnimation(WidthProperty, widthAnimation);
        }

        private void PlayNotificationSound()
        {
            try
            {
                var player = new SoundPlayer(Properties.Resources.mr_notification);
                player.Play();
            }
            catch (Exception exc)
            {
                Log.Debug("Playing notification sound", exc);
            }
        }

        private void PositionWindowOnScreen()
        {
            var desktopArea = SystemParameters.WorkArea;
            this.Top = 30;
            this.Left = desktopArea.Right - DesiredWidth;
        }

        private void LoadImage(string imageFilePath)
        {
            try
            {
                if (File.Exists(imageFilePath))
                {
                    this.ImageSource = new BitmapImage(new Uri(imageFilePath));
                }
            }
            catch (Exception)
            {
            }
        }

        private void TimeOutTask()
        {
            while (true)
            {
                Task.Delay(TimeOutDecrementMiliseconds).Wait();

                UpdateOpacityBasedOnTimeOut();

                if (!IsMouseOver)
                {
                    _milisecondsUntilTimeOut -= TimeOutDecrementMiliseconds;
                    _milisecondsUntilTimeOut = Math.Max(0, _milisecondsUntilTimeOut);

                    if (_milisecondsUntilTimeOut <= 0)
                    {
                        OnTimedOut();
                        this.Dispatcher.BeginInvoke(new Action(Close));
                        return;
                    }
                }
            }
        }

        private void UpdateOpacityBasedOnTimeOut()
        {
            if (_milisecondsUntilTimeOut <= MilisecondsToFadeOut)
            {
                double percentOfFadeOutElapsed = (double) _milisecondsUntilTimeOut/(double) MilisecondsToFadeOut;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Opacity = percentOfFadeOutElapsed;
                }));
            }
        }

        private void CloseIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            OnDismissed();
            this.Close();
        }

        private void MainPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            OnActivated();
            this.Close();
        }

        private void OnTimedOut()
        {
            if (TimedOut != null)
            {
                TimedOut();
            }
        }

        private void OnActivated()
        {
            if (UserActivated != null)
            {
                UserActivated();
            }
        }

        private void OnDismissed()
        {
            if (Dismissed != null)
            {
                Dismissed();
            }
        }

        #endregion
    }
}
