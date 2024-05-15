using SocketChat.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// Interaction logic for CustomizationWindow.xaml
    /// </summary>
    public partial class CustomizationWindow : Window
    {
        Brush chatBgColor;
        Brush textColor;
        string username;
        ChangeNameDelegate changeName;
        ChangeBrushDelegate changeMainWindowBrush;

        public CustomizationWindow(string? username, ChangeNameDelegate changeNameDelegate, Brush chatBgColor, Brush textColor, ChangeBrushDelegate changeBrush)
        {
            this.username = username;
            changeName = changeNameDelegate;
            this.chatBgColor = chatBgColor;
            this.textColor = textColor;

            InitializeComponent();
            changeMainWindowBrush = changeBrush;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbxName.Text = username != null ? username : "";
            tbxNameColorHex.Text = MainWindow.nameColor.ToString();
            tbxNameColorHex_TextChanged(null, null);

            //rctChatBgColor.Fill = chatBgColor;
            tbxChatBgColorHex.Text = chatBgColor.ToString();
            tbxTextColorHex.Text = textColor.ToString();
        }

        void ChangeColor(string hex, ref Brush brushToChange, ChatColor chatColor)
        {
            try
            {
                brushToChange = (SolidColorBrush)new BrushConverter().ConvertFrom(hex);
                if(brushToChange != null)
                {
                    changeMainWindowBrush?.Invoke(brushToChange, chatColor);
                }
            }
            catch { }
        }

        private void tbxBgColorHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChangeColor(tbxChatBgColorHex.Text, ref chatBgColor, ChatColor.Background);
        }

        private void btnResetChatBgColor_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(MainWindow.DEFAULT_BG_COLOR.ToString(), ref chatBgColor, ChatColor.Background);
            tbxChatBgColorHex.Text = chatBgColor.ToString();
        }

        private void tbxTextColorHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChangeColor(tbxTextColorHex.Text, ref textColor, ChatColor.Text);
        }

        private void btnResetTextColor_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(MainWindow.DEFAULT_TEXT_COLOR.ToString(), ref textColor, ChatColor.Text);
            tbxTextColorHex.Text = textColor.ToString();
        }

        private void tbxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(tbxName.Text != string.Empty)
            {
                changeName?.Invoke(tbxName.Text, P2P.LOCALHOST);
            }
        }

        private void tbxNameColorHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Brush b = (SolidColorBrush)new BrushConverter().ConvertFrom(tbxNameColorHex.Text);
                if(b != null)
                {
                    MainWindow.nameColor = b;
                    lblName.Foreground = b;
                    tbxName.Foreground = b;
                }
            } catch { }
        }
    }
}
