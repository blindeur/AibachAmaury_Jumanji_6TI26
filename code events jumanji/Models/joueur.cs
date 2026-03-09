using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace code_events_jumanji.Models
{
    public class Joueur
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public int PointsDeVie { get; set; } = 10;
        public bool EstBloque { get; set; } = false;
    }
}
