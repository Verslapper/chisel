using System.Collections.Generic;

namespace Chisel.Model.Models
{
    public class Team
    {
        public bool Win { get; set; }
        public int Score { get; set; }
        public List<Player> Players { get; set; }
    }
}
