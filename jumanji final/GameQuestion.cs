namespace test_martye
{
    /// <summary>
    /// Modele d'une enigme a reponse libre.
    /// Le joueur doit saisir une reponse qui sera comparee a AnswerText.
    /// </summary>
    public sealed class GameQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string AnswerText { get; set; } = string.Empty;
        public string HintText { get; set; } = string.Empty;
    }
}
