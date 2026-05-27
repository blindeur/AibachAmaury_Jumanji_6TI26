using System.Collections.Generic;

namespace test_martye
{
    /// <summary>
    /// Represente l'etat complet d'une partie sauvegardable.
    /// Cette classe permet de restaurer une partie inachevee.
    /// </summary>
    public sealed class GameSession
    {
        public int? SaveId { get; set; }
        public int PlayerCount { get; set; }
        public int CurrentTurnIndex { get; set; }
        public int LastDice { get; set; }
        public int? PendingQuestionId { get; set; }
        public bool IsFinished { get; set; }
        public List<PlayerData> Players { get; set; } = new();
    }
}
