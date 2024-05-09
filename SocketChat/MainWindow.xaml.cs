using SocketChat.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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
        public static readonly int DEFAULT_PORT = 50000;
        public static readonly string DESCRIPTION = "For testing porpuses only.\n" +
                                                    "Right click on name, timestamp or message to copy it.";
        public ObservableCollection<Message> messages { get; private set; }
        int sourcePort = DEFAULT_PORT;
        P2P? socket;
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

            messages = new ObservableCollection<Message>();
            DataContext = this;
            serializer = new XmlSerializer(typeof(List<Contact>));
            ChangePort += ChangePortImplementation;
            UpdateAddressBook += (AddressBook ab) => AddressBook = ab;

            LoadXML();
            ChangePort.Invoke(sourcePort);
        }

        private void LoadXML()
        {
            if (File.Exists(databasePath))
            {
                try
                {
                    StreamReader sr = new StreamReader(databasePath);
                    AddressBook = new AddressBook((List<Contact>)serializer.Deserialize(sr));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error loading {databasePath}:\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AddressBook = new AddressBook();
                }
            }
            else
            {
                AddressBook = new AddressBook();
            }
        }

        public void ChangePortImplementation(int port)
        {
            sourcePort = port;
            StopReceiving();
            if(socket != null)
            {
                socket.Close();
                socket = null;
            }

            try
            {
                socket = new P2P(IPAddress.Any, sourcePort);
            }
            catch (SocketException ex)
            {
                MessageBox.Show(this, $"Error creating socket:\n{ex.Message}", "Initialize socket error", MessageBoxButton.OK, MessageBoxImage.Error);
                socket = null;
                ShowNetConfig(IsVisible ? this : null);
                return;
            }

            StartReceiving();

            Title = $"Chat (listening on: {port})";
        }

        public void StartReceiving()
        {
            cancellationTokenSource = new CancellationTokenSource();
            receiveTask = Task.Factory.StartNew(async () =>
            {
                while(!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    string[]? message = socket.Receive();
                    if(message != null)
                    {
                        Dispatcher.Invoke(AddMessage, message[0], message[1]);
                    }
                    await Task.Delay(200);
                }
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

        private void HandleSend()
        {
            string[] remoteSocketData = tbxSocket.Text.Split(':');
            IPAddress remoteAddr;
            int remotePort;

            if(tbxMessage.Text == string.Empty) { return; }
            if(remoteSocketData[0] == "localhost") { remoteSocketData[0] = "127.0.0.1"; }
            if(!IPAddress.TryParse(remoteSocketData[0], out remoteAddr) || !int.TryParse(remoteSocketData[1], out remotePort) || remotePort <= 0 || remotePort > 65535)
            {
                MessageBox.Show(this, "Insert a valid address and port", "Invalid remote socket", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            socket.Send(remoteAddr, remotePort, tbxMessage.Text);
            AddMessage("127.0.0.1", tbxMessage.Text);
            tbxMessage.Text = string.Empty;
        }

        private void AddMessage(string senderIP, string message)
        {
            string? senderName;
            try
            {
                senderName = AddressBook.GetContactFromIP(senderIP).Name;
            } catch (NullReferenceException ex) { senderName = null; }
            Message msg = new Message(senderIP, senderName, message, DateTime.Now);
            messages.Add(msg);
            lstMessages.ScrollIntoView(msg);
        }

        public static string GetTime()
        {
            return DateTime.Now.TimeOfDay.ToString().Split('.')[0];
        }

        public static IPAddress GetBroadcastIPAddress(IPAddress ip, IPAddress subnetMask)
        {
            uint ipAddr = BitConverter.ToUInt32(ip.GetAddressBytes(), 0);
            uint mask = BitConverter.ToUInt32(subnetMask.GetAddressBytes(), 0);
            uint broadcastAddr = ipAddr | ~mask;
            
            return new IPAddress(BitConverter.GetBytes(broadcastAddr));
        }

        public static string GetDefaultIP()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetIPProperties().UnicastAddresses) // Get all IPs
                .SelectMany(addr => addr) // Convert to IEnumerable
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork) // Get only IPv4
                .Select(addr => addr.Address.ToString()) // Convert IP to string
                .FirstOrDefault(); // Return first IP string or null
        }

        private void ShowNetConfig(Window? owner)
        {
            NetworkConfWindow = new NetworkConfWindow(GetDefaultIP(), sourcePort, ChangePort);
            NetworkConfWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            NetworkConfWindow.Owner = owner;
            NetworkConfWindow.ShowDialog();
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
            MessageBox.Show(this, $"Chat v{Assembly.GetExecutingAssembly().GetName().Version}\n{DESCRIPTION}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void mniQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mniNetwork_Click(object sender, RoutedEventArgs e)
        {
            ShowNetConfig(this);
        }

        private void mniSaveContacts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddressBook.Contacts.Count > 0)
                {
                    StreamWriter sw = new StreamWriter(databasePath);
                    serializer.Serialize(sw, AddressBook.Contacts);
                    MessageBox.Show(this, $"Saved {AddressBook.Contacts.Count} contact" + (AddressBook.Contacts.Count == 1 ? "" : "s"), "Saved successfully", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error saving {databasePath}:\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}