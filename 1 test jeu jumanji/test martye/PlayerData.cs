using System;
using System.Collections.Generic;
using System.Text;

namespace test_martye
{
    public class PlayerData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public string Color { get; set; }
        public bool IsBlocked { get; set; } // Pour le malus Jumanji
    }
}
