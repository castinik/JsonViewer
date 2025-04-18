using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonViewer.Model
{
    class JsonNode
    {
        public string Name { get; set; }
        public string Value { get; set; } // Se è un valore semplice
        public JTokenType Type { get; set; }
        public List<JsonNode> Children { get; set; } = new List<JsonNode>();
    }
}
