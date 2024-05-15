using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace SocketChat.Models
{
    public class P2P
    {
        public static string LOCALHOST = "127.0.0.1";
        public IPAddress ip { get; private set; }
        public int port { get; private set; }
        Socket s;
        XmlSerializer serializer;

        public P2P(IPAddress ip, int port)
        {
            this.ip = ip;
            this.port = port;
            serializer = new XmlSerializer(typeof(Message));

            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.EnableBroadcast = true;

            IPEndPoint localEndPoint = new IPEndPoint(this.ip, this.port);
            s.Bind(localEndPoint);
        }

        public Message? Receive()
        {
            int nBytes = 0;
            if ((nBytes = s.Available) > 0)
            {
                byte[] buffer = new byte[nBytes];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                s.ReceiveFrom(buffer, ref remoteEndPoint);

                string senderAddr = ((IPEndPoint)remoteEndPoint).Address.ToString();
                string msg = Encoding.UTF8.GetString(buffer, 0, nBytes);

                //return new string[] { senderAddr, msg };
                using(StringReader sr = new StringReader(msg))
                {
                    return new Message((Message)serializer.Deserialize(sr)) { SenderIP = senderAddr };
                }
            }

            return null;
        }

        public void Send(IPAddress remoteAddr, int remotePort, Message message)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteAddr, remotePort);
            byte[] msg;
            using(StringWriter sw = new StringWriter())
            {
                serializer.Serialize(sw, message);
                msg = Encoding.UTF8.GetBytes(sw.ToString());
            }
            s.SendTo(msg, remoteEndPoint);
        }

        public void Close()
        {
            s.Close();
        }
    }
}
