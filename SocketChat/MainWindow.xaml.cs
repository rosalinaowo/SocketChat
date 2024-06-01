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
    
    
    public enum ChatColor { Background, Text }
    public delegate void ChangeNameDelegate(string name, string ipAddress);
    public delegate void ChangePortDelegate(int port);
    public delegate void UpdateAddressBookDelegate(AddressBook ab);
    public delegate void ChangeBrushDelegate(Brush brush, ChatColor chatColor);

    public partial class MainWindow : Window
    {
        public static readonly int DEFAULT_PORT = 50000;
        public static readonly string DESCRIPTION = "For testing porpuses only.\n" +
                                                    "Right click on name, timestamp or message to copy it.";
        public static readonly Brush DEFAULT_BG_COLOR = Brushes.LightBlue;
        public static readonly Brush DEFAULT_TEXT_COLOR = Brushes.Black;
        public static readonly Brush DEFAULT_NAME_COLOR = Brushes.Blue;

        public static Brush nameColor = DEFAULT_NAME_COLOR;
        public string? whoami { get; set; }
        Brush TextColor;
        public ObservableCollection<MessageWPF> messages { get; private set; }
        int sourcePort = DEFAULT_PORT;
        P2P? socket;
        Task receiveTask;
        CancellationTokenSource cancellationTokenSource;
        readonly string databasePath = "contacts.xml";
        XmlSerializer serializer;
        AddressBook AddressBook;
        NetworkConfWindow NetworkConfWindow;
        ContactsWindow ContactsWindow;
        public ChangeNameDelegate ChangeName { get; private set; }
        public ChangePortDelegate ChangePort { get; private set; }
        public UpdateAddressBookDelegate UpdateAddressBook { get; private set; }
        public ChangeBrushDelegate ChangeBrush { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            // Inatialize main stuff
            whoami = null;
            Background = DEFAULT_BG_COLOR;
            TextColor = DEFAULT_TEXT_COLOR;
            messages = new ObservableCollection<MessageWPF>();
            serializer = new XmlSerializer(typeof(List<Contact>));

            // Delegates
            ChangeName += (name, ipAddress) =>
            {
                AddressBook.DeleteContact(whoami, ipAddress);
                whoami = name;
                AddressBook.AddContact(whoami, ipAddress);
            };
            ChangePort += ChangePortImplementation;
            UpdateAddressBook += (AddressBook ab) => AddressBook = ab;
            ChangeBrush += ChangeBrushImplementation;

            AddressBook = LoadXML();
            try
            {
                whoami = AddressBook.GetContactFromIP(P2P.LOCALHOST)?.Name;
            } catch { whoami = null; }

            ChangePort.Invoke(sourcePort);
        }

        private AddressBook LoadXML()
        {
            if (File.Exists(databasePath))
            {
                try
                {
                    StreamReader sr = new StreamReader(databasePath);
                    AddressBook ab = new AddressBook((List<Contact>)serializer.Deserialize(sr));
                    sr.Close();
                    return ab;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error loading {databasePath}:\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new AddressBook();
                }
            }
            else
            {
                return new AddressBook();
            }
        }

        private void SaveXML()
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

        public void ChangeBrushImplementation(Brush brush, ChatColor chatColor)
        {
            switch(chatColor)
            {
                case ChatColor.Background: Background = brush; break;
                case ChatColor.Text: TextColor = brush; break;
                default: break;
            }
        }

        public void StartReceiving()
        {
            cancellationTokenSource = new CancellationTokenSource();
            receiveTask = Task.Factory.StartNew(async () =>
            {
                while(!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Message? message = socket.Receive();
                    if(message != null)
                    {
                        //await Dispatcher.InvokeAsync(() => AddMessage(message));
                        //AddMessage(message);
                        Dispatcher.Invoke(AddMessage, message);
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
            if(remoteSocketData[0] == "localhost") { remoteSocketData[0] = P2P.LOCALHOST; }
            if(!IPAddress.TryParse(remoteSocketData[0], out remoteAddr) || !int.TryParse(remoteSocketData[1], out remotePort) || remotePort <= 0 || remotePort > 65535)
            {
                MessageBox.Show(this, "Insert a valid address and port", "Invalid remote socket", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Message msg = new Message(P2P.LOCALHOST, sourcePort, whoami, new BrushConverter().ConvertToString(nameColor), tbxMessage.Text, DateTime.Now);
            socket.Send(remoteAddr, remotePort, msg);
            AddMessage(msg);
            tbxMessage.Text = string.Empty;
        }

        private void AddMessage(Message message)
        {
            MessageWPF msg;
            try
            {
                string? preferredSenderName = AddressBook.GetContactFromIP(message.SenderIP).Name;
                //msg = new MessageWPF(new Message(message) { SenderName = preferredSenderName });
                msg = Dispatcher.Invoke(() =>
                {
                    var msgCopy = new Message(message) { SenderName = preferredSenderName };
                    return new MessageWPF(msgCopy);
                });
            }
            catch (NullReferenceException ex)
            {
                msg = Dispatcher.Invoke(() => new MessageWPF(message));
            }

            messages.Add(msg);
            lstMessages.ScrollIntoView(msg);

            // Async implementation
            /* MessageWPF msg = await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    string? preferredSenderName;
                    if (message.SenderIP == "127.0.0.1")
                    {
                        preferredSenderName = whoami; // salva automaticamente nei contatti te stessa
                    }
                    else
                    {
                        preferredSenderName = AddressBook.GetContactFromIP(message.SenderIP).Name;
                    }
                    var msgCopy = new Message(message) { SenderName = preferredSenderName };
                    return new MessageWPF(msgCopy);
                }
                catch (NullReferenceException ex)
                {
                    return new MessageWPF(message);
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                messages.Add(msg);
                lstMessages.ScrollIntoView(msg);
            }); */
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
                .FirstOrDefault(); // Return first IP string (hoping it's the default adapter) or null
        }

        private void ShowNetConfig(Window? owner)
        {
            NetworkConfWindow = new NetworkConfWindow(GetDefaultIP(), sourcePort, ChangePort);
            if(owner != null)
            {
                NetworkConfWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                NetworkConfWindow.Owner = owner;
            }
            else
            {
                NetworkConfWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            
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
            SaveXML();
        }

        private void mniCustomization_Click(object sender, RoutedEventArgs e)
        {
            CustomizationWindow customizationWindow = new CustomizationWindow(whoami, ChangeName, Background, TextColor, ChangeBrush);
            customizationWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            customizationWindow.Owner = this;
            customizationWindow.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(LoadXML() != AddressBook)
            {
                var result = MessageBox.Show(this, "Do you want to save the address book?", "Save?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (result)
                {
                    case MessageBoxResult.Yes: SaveXML(); break;
                    case MessageBoxResult.No: break;
                    case MessageBoxResult.Cancel: case MessageBoxResult.None: e.Cancel = true; break;
                }
            }
        }
    }
}