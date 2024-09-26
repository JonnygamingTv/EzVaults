using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EzVaults
{
    public class Vaulter
    {
        [XmlAttribute] public string Name;
        [XmlAttribute] public string Permission;
        [XmlAttribute] public byte Width;
        [XmlAttribute] public byte Height;
        public Vaulter() { }
        public Vaulter(string n,string p,byte w,byte h) { Name = n; Permission = p; Width = w; Height = h; }
    }
}
