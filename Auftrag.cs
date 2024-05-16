using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyWorkflow
{
    public class Auftrag
    {
        [property: JsonPropertyName("hotlineNr")]
        public string HotlineNr { get; set; }
        public string projektNr { get; set; }
        public string auftragsart { get; set; }
        public string bestellArt { get; set; }
        public string bestellDatum { get; set; }
        public string titel { get; set; }
        public string bearb1 { get; set; }
        public string kalkStd { get; set; }
        public string angebDm { get; set; }
        public string termin { get; set; }
        public string woche { get; set; }
        public string erlDatum { get; set; }
        public string vermerk { get; set; }
        public string test { get; set; }
    }
    public class AuftragResponse
    {
        public List<Auftrag> recordset { get; set; }
    }
}
