using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TCPChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool doClose = false;
        public bool DoClose { get { return doClose; } set { doClose = value; } }

        private ChatServer chatServer = null;
        public ChatServer ChatServer { get { return chatServer; } set { chatServer = value; } }

        private const string msgDateFormat = "(HH:mm:ss)";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LstStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void LstPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void TxtBroadcast_TextChanged(object sender, TextChangedEventArgs e) { }
        private void TxtPM_TextChanged(object sender, TextChangedEventArgs e) { }
        private void BtnPM_Click(object sender, RoutedEventArgs e) { }
        private void BtnKick_Click(object sender, RoutedEventArgs e) { }
        // This subroutine sends the contents of the Broadcast textbox to all clients, if
        // it is not empty, and clears the textbox
        private void BtnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (TxtBroadcast.Text != "")
            {
                chatServer.UpdateStatus("Broadcasting: " + TxtBroadcast.Text, LstStatus, MoreColors.BLACK_PEARL);
                chatServer.Broadcast("BROAD|" + TxtBroadcast.Text);
                TxtBroadcast.Text = string.Empty;
            }
        }

        #region Menu Items
        private void Menu_Start_Listen(object sender, RoutedEventArgs e)
        {
            StartListen.IsEnabled = false;
            StopListen.IsEnabled = true;

            chatServer.StartThread();
        }
        private async void Menu_Stop_ListenAsync(object sender, RoutedEventArgs e)
        {
            if (!DoClose)
            {
                StartListen.IsEnabled = true;
                StopListen.IsEnabled = false;
                LeftMainGrid.IsEnabled = false;
                RightMainGrid.IsEnabled = false;

                await chatServer.StopThread();

                LeftMainGrid.IsEnabled = true;
                RightMainGrid.IsEnabled = true;
            }
        }
        private async void Menu_Exit_ListenAsync(object sender, RoutedEventArgs e)
        {
            if (!DoClose)
            {
                MainGrid.IsEnabled = false;
                await chatServer.ClosingTask();
            }
        }
        #endregion


        private void LstStatus_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Get the border of the listview (first child of a listview)
            Decorator border = VisualTreeHelper.GetChild(LstStatus, 0) as Decorator;
            double d = 0;
            // Get scrollviewer
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            if (scrollViewer.ScrollableHeight == 0)
            {
                d = 4;
            }
            else
            {
                d = 23;
            }
            foreach (RichTextBox rbt in LstStatus.Items)
            {
                rbt.Width = LstStatus.ActualWidth - d;
            }
        }

        // Moves the scrollbar to the last added element in ListBox.
        public void UpdateScrollBar(ListBox listBox)
        {
            {
                Decorator view = VisualTreeHelper.GetChild(LstStatus, 0) as Decorator;
                double d = 0;
                // Get scrollviewer
                ScrollViewer scrollViewer = view.Child as ScrollViewer;
                if (scrollViewer.ScrollableHeight == 0)
                {
                    d = 4;
                }
                else
                {
                    d = 23;
                }
                foreach (RichTextBox rbt in LstStatus.Items)
                {
                    rbt.Width = LstStatus.ActualWidth - d;
                }
            }

            if (listBox != null)
            {
                Border border = (Border)VisualTreeHelper.GetChild(listBox, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }

        }


        #region Main Window Events
        // Starts the stream to listen to messages at the time of the first launch of the application.
        private void Window_Load(object sender, EventArgs e)
        {
            StartListen.IsEnabled = false;
            StopListen.IsEnabled = true;

            chatServer = new ChatServer(this);
            chatServer.StartThread();
        }

        // When the window closes, stop the stream of listening.
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!DoClose)
            {
                MainGrid.IsEnabled = false;
                e.Cancel = true;
                await chatServer.ClosingTask();
                DoClose = true;
                Environment.Exit(0);
            }
        }

        // Another way to close an application is for confidence.
        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Application.Current.Shutdown();
        }

        #endregion

        SolidColorBrush white = new SolidColorBrush(Colors.White);
        SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);

        public void CreateTxt(string statusMessage, ListBox listBox, Color color)
        {
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.IsReadOnly = true;
            richTextBox.HorizontalAlignment = HorizontalAlignment.Stretch;

            richTextBox.BorderBrush = transparent;
            richTextBox.Background = white;
            richTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 151, 230)) { Opacity = 0.5 };

            richTextBox.GotFocus += gotFocus;
            richTextBox.LostFocus += lostFocus;
            richTextBox.MouseEnter += mouseEnter;
            richTextBox.MouseLeave += mouseLeave;

            richTextBox.AppendText(DateTime.Now.ToString(msgDateFormat), MoreColors.CONCRETE);
            richTextBox.AppendText(" ");
            richTextBox.AppendText(statusMessage, color);
            richTextBox.HorizontalAlignment = HorizontalAlignment.Left;

            LstStatus.Items.Add(richTextBox);
            UpdateScrollBar(listBox);
        }
        private void gotFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            richTextBox.Background = new SolidColorBrush(Color.FromRgb(247, 241, 227));
        }
        private void lostFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            richTextBox.Background = white;
        }
        private void mouseEnter(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            richTextBox.Background = new SolidColorBrush(Color.FromRgb(247, 241, 227));
        }
        private void mouseLeave(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            if (!richTextBox.IsFocused)
                richTextBox.Background = white;
        }

    }
}
