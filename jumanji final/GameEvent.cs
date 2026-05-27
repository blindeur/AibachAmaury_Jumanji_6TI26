namespace test_martye
{
    /// <summary>
    /// Modele d'un event tire depuis la base de donnees.
    /// Il peut etre neutre, positif ou negatif selon son contenu.
    /// </summary>
    public sealed class GameEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EffectType { get; set; } = "None";
        public int EffectValue { get; set; }
        public bool IsNegative { get; set; }
    }
}
