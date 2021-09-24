using Chisel.Model.Enums;
using System.Collections.Generic;

namespace Chisel.Model.Models
{
    public class Game
    {
        public string Id { get; set; }
        public GameMode GameMode { get; set; }
        public bool Ranked { get; set; }
        public List<Team> Teams { get; set; }
    }
}
