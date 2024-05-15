using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace SocketChat.Models
{
    [Serializable]
    [XmlInclude(typeof(DateTime))]
    public class Message
    {
        public string SenderIP { get; set; }
        public int SenderPort { get; set; }
        public string SenderName { get; set; }
        public string NameColorHex { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }

        public Message(string senderIp, int senderPort, string senderName, string nameColorHex, string text, DateTime time)
        {
            SenderIP = senderIp;
            SenderPort = senderPort;
            SenderName = senderName;
            NameColorHex = nameColorHex;
            Text = text;
            Time = time;
        }

        public Message(Message message)
        {
            if(message == null) { throw new ArgumentNullException(nameof(message)); }
            SenderIP = message.SenderIP;
            SenderPort = message.SenderPort;
            SenderName = message.SenderName;
            NameColorHex = message.NameColorHex;
            Text = message.Text;
            Time = message.Time;
        }

        public Message() { }
    }

    public class MessageWPF : Message
    {
        public DockPanel View { get; private set; }

        public MessageWPF(string senderIp, int senderPort, string senderName, string nameColorHex, string text, DateTime time) :
               base(senderIp, senderPort, senderName, nameColorHex, text, time)
        {
            InitializeView();
        }

        public MessageWPF(Message message) : base(message)
        {
            InitializeView();
        }

        private void InitializeView()
        {
            View = new DockPanel();
            TextBlock tbkTime = new TextBlock() { Text = Time.ToString("T") + " " };
            TextBlock tbkSender = new TextBlock()
            {
                Text = SenderName == null ? SenderIP : SenderName,
                Foreground = (Brush)new BrushConverter().ConvertFromString(NameColorHex),
            };
            TextBlock tbkMessage = new TextBlock() { Text = ": " + Text, TextWrapping = TextWrapping.Wrap };
            tbkTime.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetDataObject(Time.ToString());
            tbkSender.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetDataObject(SenderIP + ":" + SenderPort);
            tbkMessage.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetDataObject(Text);
            View.Children.Add(tbkTime);
            View.Children.Add(tbkSender);
            View.Children.Add(tbkMessage);
        }
    }
}
