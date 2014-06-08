using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MissionPlanner.aiaa
{
    public class TriggerPhotoListener
    {
        private const int listenPort = 6516;

        private UdpClient listener;

        private UdpClient responder;

        private IPEndPoint groupEp = new IPEndPoint(IPAddress.Any, listenPort);

        public delegate void CommandHandler(object sender, CommandEventArgs e);

        public event CommandHandler OnCommand;

        public void Attach()
        {
            if( listener == null )
            {
                listener = new UdpClient(6516, AddressFamily.InterNetwork);
                responder = new UdpClient(AddressFamily.InterNetwork);
                
                listener.BeginReceive(new AsyncCallback(receiveAsync), null);
            }
        }

        public void Detatch()
        {
            try
            {
                listener.Close();
                responder.Close();
            }
            catch( SocketException ex )
            {
                Console.WriteLine("SPYKAT Command error on detatching:\r\n" + ex.ToString());
            }

            listener = null;
            responder = null;
        }

        private void sendAsync(IAsyncResult res )
        {
            responder.EndSend(res);
        }

        private void receiveAsync(IAsyncResult res)
        {
            IPEndPoint rx = new IPEndPoint(IPAddress.Any, 5616); // dummy gets overwritten
            byte[] data = listener.EndReceive(res, ref rx);
            String s = System.Text.Encoding.ASCII.GetString(data).Trim().ToUpper();
            if( !String.IsNullOrEmpty( s ) && s.StartsWith("SPYKAT: ") && s.Length > 8 )
            {
                CommandEventArgs args = new CommandEventArgs(s.Substring(8));

                String confirm = "SPYKAT-ACK: " + args.Command;
                byte[] ack_data = Encoding.ASCII.GetBytes(confirm);
                responder.BeginSend(ack_data, ack_data.Length, new IPEndPoint( rx.Address, 5616 ), new AsyncCallback(sendAsync), null);
                
                if( OnCommand != null )
                {
                    OnCommand(this, args);
                }

            }
            else
            {
                Console.WriteLine("Invalid SPYKAT command received on UDP port 6516");
            }

            listener.BeginReceive(new AsyncCallback(receiveAsync), null);
        }

        public class CommandEventArgs : EventArgs
        {
            public String Command { get; set; }

            public CommandEventArgs(String s)
            {
                this.Command = s;
            }
        }
    }
}
