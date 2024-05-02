using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Tele.Bot.Models
{
    public class LocalNames
    {
        public string El { get; set; }
        public string Oc { get; set; }
        public string Es { get; set; }
        public string Ug { get; set; }
        public string Ku { get; set; }
        public string Ur { get; set; }
        public string Ca { get; set; }
        public string Fa { get; set; }
        public string Ps { get; set; }
        public string Pl { get; set; }
        public string De { get; set; }
        public string Ar { get; set; }
        public string En { get; set; }
        public string Pa { get; set; }
        public string Hy { get; set; }
        public string Gr { get; set; }
        public string He { get; set; }
        public string Fr { get; set; }
        public string Pt { get; set; }
    }

    public class City
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
//        public LocalNames LocalNames { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
    }
}
