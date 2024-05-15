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
        public List<Contact> Contacts { get; set; }

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

        public void DeleteContact(Contact c)
        {
            Contacts.Remove(c);
        }

        public bool DeleteContact(string name, string ipAddress)
        {
            foreach (Contact c in Contacts)
            {
                if (name == c.Name && ipAddress == c.IPAddr)
                {
                    Contacts.Remove(c);
                    return true;
                }
            }
            return false;
        }

        public Contact? GetContactFromIP(string ipAddress)
        {
            foreach(Contact c in Contacts)
            {
                if(c.IPAddr == ipAddress) { return c; }
            }
            return null;
        }
        public Contact? GetContactFromName(string name)
        {
            foreach (Contact c in Contacts)
            {
                if (c.Name == name) { return c; }
            }
            return null;
        }
    }
}
