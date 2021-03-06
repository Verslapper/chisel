using Chisel.Model.Enums;

namespace Chisel.Model.Models
{
    public class Player
    {
        public string Name { get; set; }
        public Tier Tier { get; set; }
        public Rank Rank { get; set; } // like rank, but comparable when casted
        public int Score { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int Saves { get; set; }
        public int Shots { get; set; }
        public bool MVP { get; set; }
        public bool Win { get; set; }
    }
}
