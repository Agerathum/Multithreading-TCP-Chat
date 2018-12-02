using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows;

namespace Chat.Server
{
    public delegate void LineReceive(ClientConnection sender, string Data);

    // The ClientConnection class encapsulates the functionality of a TcpClient connection
    // with streaming for a single user.

    public class ClientConnection
    {
        private const int READ_BUFFER_SIZE = 255;
        private byte[] readBuffer = new byte[READ_BUFFER_SIZE];
        public TcpClient client;
        private string strName;
        public string Name { get { return strName; } set { strName = value; } }

        // Overload the new operator to set up a read thread.
        public ClientConnection(TcpClient client)
        {
            this.client = client;
            // This starts the asynchronous read thread.  The data will be saved into readBuffer.
            this.client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
        }

        public event LineReceive LineReceived;

        // This subroutine uses a StreamWriter to send a message to the user.
        public void SendData(string Data)
        {
            //lock ensure that no other threads try to use the stream at the same time.
            lock (client.GetStream())
            {
                StreamWriter writer = new StreamWriter(client.GetStream());
                writer.Write(Data + (char)13 + (char)10);
                // Make sure all data is sent now.
                writer.Flush();
            }
        }

        // This is the callback function for TcpClient.GetStream.Begin. It begins an 
        // asynchronous read from a stream.
        private void StreamReceiver(IAsyncResult asyncResult)
        {
            int bytesRead;
            string strMessage;

            try
            {
                // Ensure that no other threads try to use the stream at the same time.
                lock (client.GetStream())
                {
                    // Finish asynchronous read into readBuffer and get number of bytes read.
                    bytesRead = client.GetStream().EndRead(asyncResult);
                }

                if (bytesRead == 0)
                {
                    client.Client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                else
                {
                    // Convert the byte array the message was saved into, minus one for the Chr(13).
                    strMessage = Encoding.UTF8.GetString(readBuffer, 0, bytesRead - 1);
                    LineReceived(this, strMessage);

                    // Ensure that no other threads try to use the stream at the same time.
                    lock (client.GetStream())
                    {
                        // Start a new asynchronous read into readBuffer.
                        client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
