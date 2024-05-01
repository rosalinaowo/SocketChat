using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for NetworkConfWindow.xaml
    /// </summary>
    public partial class NetworkConfWindow : Window
    {
        int port;
        ChangePortDelegate changePort;
        public NetworkConfWindow(string ip, int port, ChangePortDelegate changeTitleDelegate)
        {
            InitializeComponent();
            
            this.port = port;
            this.changePort = changeTitleDelegate;
            tbkIP.Text = ip;
            tbxPort.Text = this.port.ToString();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(tbxPort.Text, out port) && port > 0 && port <= 65535)
            {
                changePort?.Invoke(port);
                Close();
            }
            else
            {
                MessageBox.Show("Insert a valid port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
