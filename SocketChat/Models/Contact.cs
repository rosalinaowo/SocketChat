using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketChat.Models
{
    [Serializable]
    public class Contact
    {
        public string Name { get; private set; }
        public string IPAddr { get; private set; }
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
}
