using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SocketChat.Models
{
    public class AddressBook
    {
        public List<Contact> Contacts { get; private set; }
        public AddressBook(List<Contact> contacts)
        {
            Contacts = contacts;
        }
        public AddressBook()
        {
            Contacts = new List<Contact>();
        }

        public void AddContact(string name, string ipAddr)
        {
            Contacts.Add(new Contact(name, ipAddr));
        }
    }
}
