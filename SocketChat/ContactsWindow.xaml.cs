using SocketChat.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SocketChat
{
    /// <summary>
    /// Logica di interazione per Contacts.xaml
    /// </summary>
    public partial class ContactsWindow : Window
    {
        AddressBook ab;
        public ObservableCollection<ContactWPF> Contacts { get; private set; }
        UpdateAddressBookDelegate updateAddressBook;
        public ContactsWindow(AddressBook ab, UpdateAddressBookDelegate updateAddressBookDelegate)
        {
            InitializeComponent();

            Contacts = new ObservableCollection<ContactWPF>();
            DataContext = this;

            this.ab = ab;
            this.updateAddressBook = updateAddressBookDelegate;
            UpdateView();
        }

        private void UpdateView()
        {
            Contacts.Clear();
            foreach (Contact c in ab.Contacts)
            {
                AddContact(c.Name, c.IPAddr);
            }
        }

        private bool CheckInfo(string name, string ipAddress)
        {
            IPAddress a;
            if (name == string.Empty || !IPAddress.TryParse(ipAddress, out a))
            {
                MessageBox.Show("Insert valid info", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public void AddContact(string name, string ipAddress)
        {
            Grid view = new Grid();
            view.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            view.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock tbkName = new TextBlock() { Text = name, Foreground = Brushes.Blue };
            TextBlock tbkIpAddr = new TextBlock() { Text = ipAddress };
            Grid.SetColumn(tbkIpAddr, 1);
            view.Children.Add(tbkName);
            view.Children.Add(tbkIpAddr);
            ContactWPF c = new ContactWPF(name, ipAddress, view);
            Contacts.Add(c);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if(!CheckInfo(tbxName.Text, tbxAddr.Text)) { return; }
            Contact c;
            if((c = ab.GetContactFromIP(tbxAddr.Text)) != null || (c = ab.GetContactFromName(tbxName.Text)) != null)
            {
                ab.Contacts[ab.Contacts.IndexOf(c)] = new Contact(tbxName.Text, tbxAddr.Text);
            }
            else
            {
                ab.AddContact(tbxName.Text, tbxAddr.Text);
            }
            tbxName.Text = string.Empty;
            tbxAddr.Text = string.Empty;
            UpdateView();
            updateAddressBook?.Invoke(ab);
        }
    }
}
