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
        public MainWindow()
        {
            InitializeComponent();
        }
        const int PORT_NUM = 10000;
        private Hashtable clients = new Hashtable();
        private TcpListener listenerTCP = null;
        private Thread listenerThread = null;
        private bool listening_stop = false;
        private bool _doClose = false;

        private void LstStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void LstPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void TxtBroadcast_TextChanged(object sender, TextChangedEventArgs e) { }
        private void TextBox1_TextChanged(object sender, TextChangedEventArgs e) { }
        private void BtnPM_Click(object sender, RoutedEventArgs e) { }
        private void BtnKick_Click(object sender, RoutedEventArgs e) { }


        private void StartThread()
        {
            this.StartListen.IsEnabled = false;
            this.StopListen.IsEnabled = true;

            if (listenerThread != null)
            {
                listenerThread = null;
            }

            listening_stop = false;
            ThreadStart doListen = new ThreadStart(DoListen);
            listenerThread = new Thread(doListen);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            UpdateStatus("TCP Listener started.", LstStatus);
        }
        private async Task StopThread()
        {
            this.StartListen.IsEnabled = true;
            this.StopListen.IsEnabled = false;
            this.LeftMainGrid.IsEnabled = false;
            this.RightMainGrid.IsEnabled = false;

            listening_stop = true;
            await Task.Delay(200);
            listenerTCP.Stop();
            listenerThread.Interrupt();
            listenerThread.Abort();
            listenerThread = null;
            UpdateStatus("TCP Listener stoped.", LstStatus);

            this.LeftMainGrid.IsEnabled = true;
            this.RightMainGrid.IsEnabled = true;

        }
        private async Task ClosingTask()
        {
            listening_stop = true;
            await Task.Delay(100);
            listenerTCP.Stop();
            if (listenerThread != null)
            {
                listenerThread.Interrupt();
                listenerThread.Abort();
                UpdateStatus("TCP Listener stoped.", LstStatus);
            }
            await Task.Delay(1000);
            _doClose = true;
            Environment.Exit(0);
        }

        // This subroutine sends a message to all attached clients
        private void Broadcast(string strMessage)
        {
            ClientConnection client = null;
            // All entries in the clients Hashtable are ClientConnection so it is possible
            // to assign it safely.
            foreach (DictionaryEntry entry in clients)
            {
                client = (ClientConnection)entry.Value;
                client.SendData(strMessage);
            }
        }

        // This subroutine sends the contents of the Broadcast textbox to all clients, if
        // it is not empty, and clears the textbox
        private void BtnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (TxtBroadcast.Text != "")
            {
                UpdateStatus("Broadcasting: " + TxtBroadcast.Text, LstStatus);
                Broadcast("BROAD|" + TxtBroadcast.Text);
                TxtBroadcast.Text = string.Empty;
            }
        }

        // This subroutine checks to see if username already exists in the clients Hashtable.
        // If it does, send a REFUSE message, otherwise confirm with a JOIN.
        private void ConnectUser(string userName, ClientConnection sender)
        {
            if (clients.Contains(userName))
            {
                ReplyToSender("REFUSE", sender);
            }
            else
            {
                sender.Name = userName;
                UpdateStatus(userName + " has joined the chat.", LstStatus);
                clients.Add(userName, sender);
                Dispatcher.Invoke(() => LstPlayers.Items.Add(sender.Name));
                Dispatcher.Invoke(() => UpdateScrollBar(LstPlayers));
                // Send a JOIN to sender, and notify all other clients that sender joined
                ReplyToSender("JOIN", sender);
                SendToClients("CHAT|" + sender.Name + " has joined the chat.", sender);
            }
        }

        // This subroutine notifies other clients that sender left the chat, and removes
        // the name from the clients Hashtable
        private void DisconnectUser(ClientConnection sender)
        {
            UpdateStatus(sender.Name + " has left the chat.", LstStatus);
            SendToClients("CHAT|" + sender.Name + " has left the chat.", sender);
            clients.Remove(sender.Name);
            Dispatcher.Invoke(() => LstPlayers.Items.Remove(sender.Name));
            Dispatcher.Invoke(() => UpdateScrollBar(LstPlayers));
        }

        // This subroutine uses a background listener thread that allows you to read incoming
        // messages without user interface delay.
        private void DoListen()
        {
            try
            {
                // Listen for new connections.
                listenerTCP = new TcpListener(System.Net.IPAddress.Any, PORT_NUM);
                listenerTCP.Start();

                while (!listening_stop)
                {
                    if (!listenerTCP.Pending())
                    {
                        Thread.Sleep(100);  // choose a number (in milliseconds) that makes sense
                        continue;           // skip to next iteration of loop
                    }

                    // Create a new user connection using TcpClient returned by
                    // TcpListener.AcceptTcpClient()
                    ClientConnection client = new ClientConnection(listenerTCP.AcceptTcpClient());

                    // Create an event handler to allow the ClientConnection to communicate
                    // with the window.
                    client.LineReceived += new LineReceive(OnLineReceived);

                    //AddHandler client.LineReceived, AddressOf OnLineReceived;
                    UpdateStatus("New connection found: waiting for log-in.", LstStatus);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // Combine all customer names and send them to the user who requested a list of users.
        private void ListUsers(ClientConnection sender)
        {
            ClientConnection client = null;
            string strUserList = null;
            UpdateStatus("Sending " + sender.Name + " a list of users online.", LstStatus);
            strUserList = "LISTUSERS";
            // All entries in the clients Hashtable are ClientConnection so it is possible
            // to assign it safely.

            foreach (DictionaryEntry entry in clients)
            {
                client = (ClientConnection)entry.Value;
                strUserList = strUserList + "|" + client.Name;
            }

            // Send the list to the sender.
            ReplyToSender(strUserList, sender);
        }

        // This is the event handler for the ClientConnection when it receives a full line.
        // Parse the cammand and parameters and take appropriate action.
        private void OnLineReceived(ClientConnection sender, string data)
        {
            string[] dataArray = null;
            // Message parts are divided by "|"  Break the string into an array accordingly.
            // Basically what happens here is that it is possible to get a flood of data during
            // the lock where we have combined commands and overflow
            // to simplify this proble, all I do is split the response by char 13 and then look
            // at the command, if the command is unknown, I consider it a junk message
            // and dump it, otherwise I act on it
            dataArray = data.Split((char)13);
            dataArray = dataArray[0].Split((char)124);

            // dataArray(0) is the command.
            switch (dataArray[0])
            {
                case "CONNECT":
                    ConnectUser(dataArray[1], sender);
                    break;
                case "CHAT":
                    SendChat(dataArray[1], sender);
                    break;
                case "DISCONNECT":
                    DisconnectUser(sender);
                    break;
                case "REQUESTUSERS":
                    ListUsers(sender);
                    break;
                default:
                    // Message is junk do nothing with it.
                    break;
            }
        }

        // This routine sends the response to the sender.
        private void ReplyToSender(string strMessage, ClientConnection sender)
        {
            sender.SendData(strMessage);
        }

        // We send a chat message to all clients except the sender.
        private void SendChat(string message, ClientConnection sender)
        {
            UpdateStatus(sender.Name + ": " + message, LstStatus);
            SendToClients("CHAT|" + sender.Name + ": " + message, sender);
        }

        // This routine sends the message to all attached clients except the sender.
        private void SendToClients(string strMessage, ClientConnection sender)
        {
            ClientConnection client;
            // Все записи в клиентских Hashtable являются ClientConnection, поэтому можно безопасно назначить их.
            foreach (DictionaryEntry entry in clients)
            {
                client = (ClientConnection)entry.Value;
                // Исключить отправителя.
                if (client.Name != sender.Name)
                {
                    client.SendData(strMessage);
                }
            }
        }

        // This routine adds a string to the list of states.
        private void UpdateStatus(string statusMessage, ListBox listBox)
        {
            Dispatcher.Invoke(() => LstStatus.Items.Add(DateTime.Now.ToString("(HH:mm:ss) ") + statusMessage));
            Dispatcher.Invoke(() => UpdateScrollBar(listBox));
        }

        // Moves the scrollbar to the last added element in ListBox.
        private void UpdateScrollBar(ListBox listBox)
        {
            if (listBox != null)
            {
                Border border = (Border)VisualTreeHelper.GetChild(listBox, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        #region Menu Items
        private void Menu_Start_Listen(object sender, RoutedEventArgs e)
        {
            StartThread();
        }
        private async void Menu_Stop_ListenAsync(object sender, RoutedEventArgs e)
        {
            if (!_doClose)
            {
                await StopThread();
            }
        }
        private async void Menu_Exit_ListenAsync(object sender, RoutedEventArgs e)
        {
            if (!_doClose)
            {
                this.MainGrid.IsEnabled = false;
                await ClosingTask();
            }
        }
        #endregion
        #region Windows Events
        // Starts the stream to listen to messages at the time of the first launch of the application.
        private void Window_Load(object sender, EventArgs e)
        {
            StartThread();
        }

        // When the window closes, stop the stream of listening.
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_doClose)
            {
                this.MainGrid.IsEnabled = false;
                e.Cancel = true;
                await ClosingTask();
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
