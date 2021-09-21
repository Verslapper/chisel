using Chisel.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Model.Models
{
    public class Game
    {
        public string Id { get; set; }
        public GameMode GameMode { get; set; }
        public bool Ranked { get; set; }
        public List<Team> Teams { get; set; }
        public List<Player> Players { get; set; }
    }
}
