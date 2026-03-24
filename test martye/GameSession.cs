using System.Collections.Generic;

namespace test_martye
{
    public sealed class GameSession
    {
        public int? SaveId { get; set; }
        public int PlayerCount { get; set; }
        public int CurrentTurnIndex { get; set; }
        public int LastDice { get; set; }
        public bool IsFinished { get; set; }
        public List<PlayerData> Players { get; set; } = new();
    }
}
