using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LstStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void LstPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void TxtBroadcast_TextChanged(object sender, TextChangedEventArgs e) { }
        private void TextPM_TextChanged(object sender, TextChangedEventArgs e) { }
        private void BtnPM_Click(object sender, RoutedEventArgs e) { }
        private void BtnKick_Click(object sender, RoutedEventArgs e) { }
        // This subroutine sends the contents of the Broadcast textbox to all clients, if
        // it is not empty, and clears the textbox
        private void BtnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (TxtBroadcast.Text != "")
            {
                chatServer.UpdateStatus("Broadcasting: " + TxtBroadcast.Text, LstStatus);
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

        // Moves the scrollbar to the last added element in ListBox.
        public void UpdateScrollBar(ListBox listBox)
        {
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
