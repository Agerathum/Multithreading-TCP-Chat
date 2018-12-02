using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net;
using Chat.Networking;
using Chat.ColorScheme;

namespace Chat.Server
{

    [System.Serializable]
    public class ChatServer
    {
        private static readonly object _lock = new object();
        private const int PORT_NUM = 10000;
        private const string msgDateFormat = "(HH:mm:ss)";
        private Hashtable clients = new Hashtable();
        private TcpListener listenerTCP = null;
        private Thread listenerThread = null;
        private MainWindow mainWindow = null;                         
        public bool Started = false;                                  //Bool value - so we know if the server is started

        public ChatServer(MainWindow mw)
        {
            mainWindow = mw;
        }

        public void StartServer()
        {
            if (listenerThread != null)
            {
                listenerThread = null;
            }

            Started = true;
            ThreadStart doListen = new ThreadStart(ListenThread);
            listenerThread = new Thread(doListen);
            //listenerThread.SetApartmentState(ApartmentState.STA);

            listenerThread.IsBackground = true;
            listenerThread.Start();

            UpdateStatus("TCP Listener started.", Palette.Colors.PIXELATED_GRASS);
        }

        public async Task StopServerTask()
        {
            Started = false;
            await Task.Delay(200);
            listenerTCP.Stop();
            listenerThread.Interrupt();
            listenerThread.Abort();
            listenerThread = null;
            UpdateStatus("TCP Listener stoped.", Palette.Colors.HARLEY_DAVIDSON_ORANGE);
        }
        public async Task ClosingTask()
        {
            Started = false;
            await Task.Delay(100);
            listenerTCP.Stop();
            if (listenerThread != null)
            {
                listenerThread.Interrupt();
                listenerThread.Abort();
                UpdateStatus("TCP Listener stoped.", Palette.Colors.HARLEY_DAVIDSON_ORANGE);
            }
            await Task.Delay(1000);
        }

        // This subroutine sends a message to all attached clients
        public void Broadcast(string strMessage)
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
                UpdateStatus(userName + " has joined the chat.", Palette.Colors.BLACK_PEARL);
                clients.Add(userName, sender);
                mainWindow.Dispatcher.Invoke(() => mainWindow.LstPlayers.Items.Add(sender.Name));
                // Send a JOIN to sender, and notify all other clients that sender joined
                ReplyToSender("JOIN", sender);
                SendToClients("CHAT|" + sender.Name + " has joined the chat.", sender);
            }
        }

        // This subroutine notifies other clients that sender left the chat, and removes
        // the name from the clients Hashtable
        private void DisconnectUser(ClientConnection sender)
        {
            UpdateStatus(sender.Name + " has left the chat.", Palette.Colors.BLACK_PEARL);
            SendToClients("CHAT|" + sender.Name + " has left the chat.", sender);
            lock (_lock) clients.Remove(sender.Name);

            mainWindow.Dispatcher.Invoke(() => mainWindow.LstPlayers.Items.Remove(sender.Name));
        }

        // This subroutine uses a background listener thread that allows you to read incoming
        // messages without user interface delay.
        private void ListenThread()
        {
            try
            {
                Thread.CurrentThread.IsBackground = true;

                // Listen for new connections.
                listenerTCP = new TcpListener(IPAddress.Any, PORT_NUM);
                listenerTCP.Start();

                while (Started)
                {

                    if (!listenerTCP.Pending())
                    {
                        Thread.Sleep(100);  // that makes sense skip to next iteration of loop
                        continue;
                    }

                    // Create a new user connection using TcpClient returned by
                    ClientConnection client = new ClientConnection(listenerTCP.AcceptTcpClient());
                    // Create an event handler to allow the ClientConnection to communicate with the window.
                    client.LineReceived += new LineReceive(OnLineReceived);

                    //AddHandler client.LineReceived, AddressOf OnLineReceived;
                    UpdateStatus("New connection found: waiting for log-in.", Palette.Colors.BLACK_PEARL);
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
            UpdateStatus("Sending " + sender.Name + " a list of users online.", Palette.Colors.BLACK_PEARL);
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
            UpdateStatus(sender.Name + ": " + message, Palette.Colors.BLACK_PEARL);
            SendToClients("CHAT|" + sender.Name + ": " + message, sender);
        }

        // This subroutine sends a message to all attached clients except the sender.
        private void SendToClients(string strMessage, ClientConnection sender)
        {
            ClientConnection client;
            // All entries in the clients Hashtable are UserConnection so it is possible to assign it safely.
            foreach (DictionaryEntry entry in clients)
            {
                client = (ClientConnection)entry.Value;
                // Exclude the sender.
                if (client.Name != sender.Name && sender.Name != null && client.Name != null)
                {
                    client.SendData(strMessage);
                }
            }
        }

        // This subroutine adds a string to the list of states.
        public void UpdateStatus(string statusMessage, Color color)
        {
            mainWindow.Dispatcher.Invoke(() => mainWindow.CreateTxt(statusMessage, color));
        }
    }
}
