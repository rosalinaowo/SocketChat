using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SocketChat.Models
{
    public class Message
    {
        public string SenderIP { get; private set; }
        public string SenderName { get; private set; }
        public string Text { get; private set; }
        public DateTime Time { get; private set; }
        public DockPanel View { get; private set; }

        public Message(string senderIp, string senderName, string text, DateTime time)
        {
            SenderIP = senderIp;
            SenderName = senderName;
            Text = text;
            Time = time;
            InitializeView();
        }

        private void InitializeView()
        {
            View = new DockPanel();
            TextBlock tbkTime = new TextBlock() { Text = Time.ToString("T") + " " };
            TextBlock tbkSender = new TextBlock() { Text = SenderName == null ? SenderIP : SenderName, Foreground = Brushes.Blue };
            TextBlock tbkMessage = new TextBlock() { Text = ": " + Text, TextWrapping = TextWrapping.Wrap };
            tbkTime.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetDataObject(Time.ToString());
            tbkSender.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetDataObject(SenderIP);
            tbkMessage.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetDataObject(Text);
            View.Children.Add(tbkTime);
            View.Children.Add(tbkSender);
            View.Children.Add(tbkMessage);
        }
    }
}
