using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Chat.ColorScheme;

namespace Chat.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string msgDateFormat = "(HH:mm:ss)";

        public ChatServer chatServer { get; set; }

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
                chatServer.UpdateStatus("Broadcasting: " + TxtBroadcast.Text, Palette.Colors.BELIZE_HOLE);
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
                chatServer.UpdateStatus("Broadcasting: " + TxtBroadcast.Text, Palette.Colors.BELIZE_HOLE);
                chatServer.Broadcast("BROAD|" + TxtBroadcast.Text);
                TxtBroadcast.Text = string.Empty;
            }
        }

        private void TxtBroadcast_Pressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            // your event handler here
            e.Handled = true;

            BtnBroadcast_Click();
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

            chatServer.StartServer();
        }
        private async void Menu_Stop_ListenAsync(object sender, RoutedEventArgs e)
        {
            if (chatServer.Started)
            {
                StartListen.IsEnabled = true;
                StopListen.IsEnabled = false;
                LeftMainGrid.IsEnabled = false;
                RightMainGrid.IsEnabled = false;

                await chatServer.StopServerTask();

                LeftMainGrid.IsEnabled = true;
                RightMainGrid.IsEnabled = true;
            }
        }
        private async void Menu_Exit_ListenAsync(object sender, RoutedEventArgs e)
        {
            if (chatServer.Started)
            {
                MainGrid.IsEnabled = false;
                await chatServer.ClosingTask();
            }
        }
        #endregion

        public void CreateTxt(string statusMessage, Color color)
        {
            TextRange textRange = new TextRange(MessageTextBox.Document.ContentStart, MessageTextBox.Document.ContentEnd);

            if (textRange.Text.Length > 0)
            {
                MessageTextBox.AppendText("\r");
            }

            MessageTextBox.AppendText(DateTime.Now.ToString(msgDateFormat), Palette.Colors.CONCRETE);
            MessageTextBox.AppendText(" ");
            MessageTextBox.AppendText(statusMessage, color);
            MessageTextBox.ScrollToEnd();
        }

        #region Main Window Events
        // Starts the stream to listen to messages at the time of the first launch of the application.
        private void Window_Load(object sender, EventArgs e)
        {
            SetupColorScheme();

            MessageTextBox.IsReadOnly = true;                                       // Set property for that we can edit already sended messenges.
            MessageTextBox.HorizontalAlignment = HorizontalAlignment.Stretch;       // Left alignment used for relative scale with window size.
            MessageTextBox.Document.Blocks.Clear();                                 // Clear all text in RichTextBox at app startup.

            ContextMenu menu = new ContextMenu();
            MessageTextBox.ContextMenu = menu;
            MenuItem item = null;
            item = new MenuItem();
            item.Command = ApplicationCommands.Copy;
            menu.Items.Add(item);
            item = new MenuItem();
            item.Command = ApplicationCommands.SelectAll;
            menu.Items.Add(item);
            item = new MenuItem();
            item.Header = "Clear Chat";
            item.Click += ClearChat;
            menu.Items.Add(item);



            StartListen.IsEnabled = false;
            StopListen.IsEnabled = true;

            chatServer = new ChatServer(this);
            chatServer.StartServer();
        }

        private void ClearChat(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Document.Blocks.Clear();                                 // Clear all text in RichTextBox at app startup.
        }

        private void SetupColorScheme()
        {
            MessageTextBox.SelectionBrush = Palette.Brushes.PROTOSS_PYLON;
            MessageTextBox.SelectionBrush.Opacity = 0.5;
        }

        // When the window closes, stop the stream of listening.
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (chatServer.Started)
            {
                MainGrid.IsEnabled = false;
                e.Cancel = true;
                await chatServer.ClosingTask();
                chatServer.Started = false;
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

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
