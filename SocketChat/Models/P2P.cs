using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SocketChat.Models
{
    public class P2P
    {
        public IPAddress ip { get; private set; }
        public int port { get; private set; }
        Socket s;

        public P2P(IPAddress ip, int port)
        {
            this.ip = ip;
            this.port = port;

            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.EnableBroadcast = true;

            IPEndPoint localEndPoint = new IPEndPoint(this.ip, this.port);
            s.Bind(localEndPoint);
        }

        public string[]? Receive()
        {
            int nBytes = 0;
            if ((nBytes = s.Available) > 0)
            {
                byte[] buffer = new byte[nBytes];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                s.ReceiveFrom(buffer, ref remoteEndPoint);

                string senderAddr = ((IPEndPoint)remoteEndPoint).Address.ToString();
                string message = Encoding.UTF8.GetString(buffer, 0, nBytes);

                return new string[] { senderAddr, message };
            }

            return null;
        }

        public void Send(IPAddress remoteAddr, int remotePort, string message)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteAddr, remotePort);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            s.SendTo(msg, remoteEndPoint);
        }

        public void Close()
        {
            s.Close();
        }
    }
}
