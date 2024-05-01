using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SocketChat.Models
{
    [Serializable]
    public class Contact
    {
        public string Name { get; set; }
        public string IPAddr { get; set; }
        public Contact() { }
        public Contact(string name, string ipAddr)
        {
            Name = name;
            IPAddr = ipAddr;
        }

        public override string ToString()
        {
            return $"{Name}: {IPAddr}";
        }
    }

    public class ContactWPF : Contact
    {
        public Grid View {  get; private set; }
        public ContactWPF(string name, string ipAddr, Grid view) : base(name, ipAddr)
        {
            View = view;
        }
    }
}
