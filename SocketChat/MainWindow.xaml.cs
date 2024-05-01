using SocketChat.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace SocketChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public delegate void ChangePort(int port);

    public partial class MainWindow : Window
    {
        int sourcePort = 50000;
        Socket s;
        Task receiveTask;
        CancellationTokenSource cancellationTokenSource;
        readonly string database = "contacts.xml";
        AddressBook Contacts;
        NetworkConfWindow NetworkConfWindow;
        ContactsWindow ContactsWindow;
        public ChangePort ChangePort { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists(database))
            {
                StreamReader sr = new StreamReader(database);
                XmlSerializer serializer = new XmlSerializer(typeof(List<Contact>));
                Contacts = new AddressBook((List<Contact>)serializer.Deserialize(sr));
            }
            else
            {
                Contacts = new AddressBook();
            }

            ChangePort += ChangePortImplementation;
            ChangePort.Invoke(sourcePort);
        }

        public void ChangePortImplementation(int port)
        {
            sourcePort = port;
            StopReceiving();
            if (s != null) { s.Close(); }
            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress localAddr = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(localAddr, sourcePort);
            s.Bind(localEndPoint);
            StartReceiving();

            Title = $"Chat (listening on: {port})";
        }

        public void StartReceiving()
        {
            cancellationTokenSource = new CancellationTokenSource();
            receiveTask = Task.Factory.StartNew(() =>
            {
                while(!cancellationTokenSource.Token.IsCancellationRequested) { receive(); }
            }, cancellationTokenSource.Token);
        }

        public void StopReceiving()
        {
            if (receiveTask != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            HandleSend();
        }

        private void tbxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { HandleSend(); }
        }

        private void receive()
        {
            int nBytes = 0;
            if ((nBytes = s.Available) > 0)
            {
                byte[] buffer = new byte[nBytes];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                s.ReceiveFrom(buffer, ref remoteEndPoint);

                string senderAddr = ((IPEndPoint)remoteEndPoint).Address.ToString();
                string message = Encoding.UTF8.GetString(buffer, 0, nBytes);

                Dispatcher.Invoke(AddMessage, $"{GetTime()} {senderAddr}: {message}");
            }
        }

        private void HandleSend()
        {
            string[] remoteSocket = tbxSocket.Text.Split(':'); // controlla validita' dati
            if (remoteSocket.Length != 2 || tbxMessage.Text == string.Empty) { return; }
            if (remoteSocket[0] == "localhost") { remoteSocket[0] = "127.0.0.1"; }

            send(remoteSocket, tbxMessage.Text);
            lstMessages.Items.Add($"{GetTime()} Me: {tbxMessage.Text}");
            tbxMessage.Text = string.Empty;
        }

        private void send(string[] remoteSocket, string message)
        {
            IPAddress remoteAddr = IPAddress.Parse(remoteSocket[0]);
            int remotePort = int.Parse(remoteSocket[1]);
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteAddr, remotePort);

            byte[] msg = Encoding.UTF8.GetBytes(message);
            s.SendTo(msg, remoteEndPoint);
        }

        private void AddMessage(string message)
        {
            lstMessages.Items.Add(message);
        }

        public static string GetTime()
        {
            return DateTime.Now.TimeOfDay.ToString().Split('.')[0];
        }

        private void btnContacts_Click(object sender, RoutedEventArgs e)
        {
            ContactsWindow = new ContactsWindow(ref Contacts);
            ContactsWindow.Show();
            ContactsWindow.Owner = this;
        }

        private void mniInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Chat v{Assembly.GetExecutingAssembly().GetName().Version}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void mniQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mniNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworkConfWindow = new NetworkConfWindow("placeholder", sourcePort, ChangePort);
            NetworkConfWindow.Show();
            NetworkConfWindow.Owner = this;
        }
    }
}