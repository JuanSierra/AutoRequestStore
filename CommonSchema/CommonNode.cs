using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoRequestStore.CommonSchema
{
    internal class CommonNode
    {
        public int Kind { get; set; }
        public CommonName Name { get; set; }
        public SelectionSet SelectionSet { get; set; }
    }
}
