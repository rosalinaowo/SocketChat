using SocketChat.Models;
using System;
using System.Collections.Generic;
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
        public ContactsWindow(ref AddressBook contacts)
        {
            InitializeComponent();

            ab = contacts;
            foreach(Contact c in ab.Contacts)
            {
                lstContacts.Items.Add(c.ToString());
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            IPAddress a;
            if(tbxName.Text == string.Empty || !IPAddress.TryParse(tbxAddr.Text, out a))
            {
                MessageBox.Show("Insert valid info", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ab.AddContact(tbxName.Text, tbxAddr.Text);
        }
    }
}
