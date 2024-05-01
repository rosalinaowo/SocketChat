using SocketChat.Models;
using System.Collections.ObjectModel;
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
    
    public delegate void ChangePortDelegate(int port);
    public delegate void UpdateAddressBookDelegate(AddressBook ab);

    public partial class MainWindow : Window
    {
        public ObservableCollection<DockPanel> messages { get; private set; }
        int sourcePort = 50000;
        Socket s;
        Task receiveTask;
        CancellationTokenSource cancellationTokenSource;
        readonly string databasePath = "contacts.xml";
        XmlSerializer serializer;
        AddressBook AddressBook;
        NetworkConfWindow NetworkConfWindow;
        ContactsWindow ContactsWindow;
        public ChangePortDelegate ChangePort { get; private set; }
        public UpdateAddressBookDelegate UpdateAddressBook { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            messages = new ObservableCollection<DockPanel>();
            DataContext = this;
            serializer = new XmlSerializer(typeof(List<Contact>));
            ChangePort += ChangePortImplementation;
            UpdateAddressBook += (AddressBook ab) => AddressBook = ab;

            if (File.Exists(databasePath))
            {
                try
                {
                    StreamReader sr = new StreamReader(databasePath);
                    AddressBook = new AddressBook((List<Contact>)serializer.Deserialize(sr));
                } catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error loading {databasePath}:\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AddressBook = new AddressBook();
                }
            }
            else
            {
                AddressBook = new AddressBook();
            }

            
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

                Dispatcher.Invoke(AddMessage, senderAddr, message);
            }
        }

        private void HandleSend()
        {
            string[] remoteSocketData = tbxSocket.Text.Split(':'); // controlla validita' dati
            IPAddress remoteAddr;
            int remotePort;

            if(tbxMessage.Text == string.Empty) { return; }
            if (remoteSocketData[0] == "localhost") { remoteSocketData[0] = "127.0.0.1"; }
            if (!IPAddress.TryParse(remoteSocketData[0], out remoteAddr) || !int.TryParse(remoteSocketData[1], out remotePort) || remotePort <= 0 || remotePort > 65535)
            {
                MessageBox.Show(this, "Insert a valid address and port", "Invalid remote socket", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            //if (remoteSocketData.Length != 2 || tbxMessage.Text == string.Empty) { return; }
            //if (!IPAddress.TryParse(remoteSocketData[0], out remoteAddr)) { return; }

            send(remoteAddr, remotePort, tbxMessage.Text);
            AddMessage("127.0.0.1", tbxMessage.Text);
            tbxMessage.Text = string.Empty;
        }

        private void send(IPAddress remoteAddr, int remotePort, string message)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteAddr, remotePort);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            s.SendTo(msg, remoteEndPoint);
        }

        private void AddMessage(string ip, string message)
        {
            DockPanel msg = new DockPanel();
            string sender;
            try
            {
                sender = AddressBook.GetContactFromIP(ip).Name;
            } catch (NullReferenceException ex) { sender = ip; }
            TextBlock tbkInfo = new TextBlock() { Text = $"{GetTime()} {sender}", Foreground = Brushes.Blue };
            TextBlock tbkMessage = new TextBlock() { Text = ": " + message, TextWrapping = TextWrapping.Wrap };
            tbkInfo.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetText(ip);
            tbkMessage.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => Clipboard.SetText(message);
            msg.Children.Add(tbkInfo);
            msg.Children.Add(tbkMessage);
            messages.Add(msg);
            lstMessages.ScrollIntoView(msg);
        }

        public static string GetTime()
        {
            return DateTime.Now.TimeOfDay.ToString().Split('.')[0];
        }

        private void btnContacts_Click(object sender, RoutedEventArgs e)
        {
            ContactsWindow = new ContactsWindow(AddressBook, UpdateAddressBook);
            ContactsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ContactsWindow.Owner = this;
            ContactsWindow.Show();
        }

        private void mniInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, $"Chat v{Assembly.GetExecutingAssembly().GetName().Version}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void mniQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mniNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworkConfWindow = new NetworkConfWindow("placeholder", sourcePort, ChangePort);
            NetworkConfWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            NetworkConfWindow.Owner = this;
            NetworkConfWindow.ShowDialog();
        }

        private void mniSaveContacts_Click(object sender, RoutedEventArgs e)
        {
            if(AddressBook.Contacts.Count > 0)
            {
                StreamWriter sw = new StreamWriter(databasePath);
                serializer.Serialize(sw, AddressBook.Contacts);
                MessageBox.Show(this, $"Saved {AddressBook.Contacts.Count} contact" + (AddressBook.Contacts.Count == 1 ? "" : "s"), "Saved successfully", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}