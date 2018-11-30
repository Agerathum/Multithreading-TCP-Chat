using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TCPChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string msgDateFormat = "(HH:mm:ss)";

        private bool doClose = false;
        public bool DoClose { get { return doClose; } set { doClose = value; } }
        private ChatServer chatServer = null;
        public ChatServer ChatServer { get { return chatServer; } set { chatServer = value; } }

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

        private void BtnBroadcast_Click()
        {
            if (!string.IsNullOrEmpty(TxtBroadcast.Text))
            {
                chatServer.UpdateStatus("Broadcasting: " + TxtBroadcast.Text, LstStatus, Palette.Colors.BELIZE_HOLE);
                chatServer.Broadcast("BROAD|" + TxtBroadcast.Text);
                TxtBroadcast.Text = string.Empty;
            }
        }
        // This subroutine sends the contents of the Broadcast textbox to all clients, if
        // it is not empty, and clears the textbox
        private void BtnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtBroadcast.Text))
            {
                chatServer.UpdateStatus("Broadcasting: " + TxtBroadcast.Text, LstStatus, Palette.Colors.BELIZE_HOLE);
                chatServer.Broadcast("BROAD|" + TxtBroadcast.Text);
                TxtBroadcast.Text = string.Empty;
            }
        }

        private void TxtBroadcast_Pressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            // your event handler here
            e.Handled = true;

            if (string.IsNullOrEmpty(TxtBroadcast.Text))
            {
                RichTextBox rbt = LstStatus.Items.GetItemAt(LstStatus.Items.Count - 1) as RichTextBox;
                rbt.Focus();
            }
            else
            {
                BtnBroadcast_Click();
            }
        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (!TxtBroadcast.IsFocused)
                {
                    TxtBroadcast.Focus();
                }
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

        private void FitItemsInList()
        {
            // Get the border of the listview (first child of a listview)
            Decorator border = VisualTreeHelper.GetChild(LstStatus, 0) as Decorator;
            double d = 0;

            // Get scrollviewer
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            if (scrollViewer.ScrollableHeight == 0)
                d = 4;
            else
                d = 23;
            foreach (RichTextBox rbt in LstStatus.Items)
            {
                rbt.Width = LstStatus.ActualWidth - d;
            }
        }

        private void LstStatus_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitItemsInList();
        }

        // Moves the scrollbar to the last added element in ListBox.
        public void UpdateScrollBar(ListBox listBox)
        {
            FitItemsInList();

            if (listBox != null)
            {
                Border border = (Border)VisualTreeHelper.GetChild(listBox, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        public void CreateTxt(string statusMessage, ListBox listBox, Color color)
        {
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.IsReadOnly = true;
            richTextBox.HorizontalAlignment = HorizontalAlignment.Stretch;

            richTextBox.BorderBrush = Palette.Brushes.TRANS;
            richTextBox.Background = Palette.Brushes.WHITE;

            richTextBox.SelectionBrush = Palette.Brushes.PROTOSS_PYLON;
            richTextBox.SelectionBrush.Opacity = 0.5;

            richTextBox.GotFocus += gotFocus;
            richTextBox.LostFocus += lostFocus;
            richTextBox.MouseEnter += mouseEnter;
            richTextBox.MouseLeave += mouseLeave;

            richTextBox.AppendText(DateTime.Now.ToString(msgDateFormat), Palette.Colors.CONCRETE);
            richTextBox.AppendText(" ");
            richTextBox.AppendText(statusMessage, color);
            richTextBox.HorizontalAlignment = HorizontalAlignment.Left;

            LstStatus.Items.Add(richTextBox);
            UpdateScrollBar(listBox);
        }
        private void gotFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            richTextBox.Background = Palette.Brushes.SWAN_WHITE;
        }
        private void lostFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            richTextBox.Background = Palette.Brushes.WHITE; ;
        }
        private void mouseEnter(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            richTextBox.Background = Palette.Brushes.SWAN_WHITE;
        }
        private void mouseLeave(object sender, RoutedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            if (!richTextBox.IsFocused)
                richTextBox.Background = Palette.Brushes.WHITE;
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
    }
}
