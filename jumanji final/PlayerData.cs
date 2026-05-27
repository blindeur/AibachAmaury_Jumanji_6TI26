namespace test_martye
{
    /// <summary>
    /// Represente un joueur sur le plateau.
    /// Cette classe contient a la fois :
    /// - son identite visuelle,
    /// - sa position,
    /// - et certains etats de jeu comme le blocage.
    /// </summary>
    public sealed class PlayerData
    {
        public int Id { get; set; }
        public int PlayerIndex { get; set; }
        public string Name { get; set; } = string.Empty;
        // Row et Col sont conserves pour la compatibilite avec la base existante.
        // Dans la nouvelle version a chemins colores :
        // - Row stocke surtout l'index de progression dans le chemin du joueur,
        // - Col reste a 0 une fois la partie convertie dans ce nouveau systeme.
        public int Row { get; set; }
        public int Col { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Retourne l'initiale du joueur pour l'affichage du pion.
        /// Si le nom est vide, on affiche "?" pour eviter une erreur visuelle.
        /// </summary>
        public string Initial =>
            string.IsNullOrWhiteSpace(Name) ? "?" : Name[..1].ToUpperInvariant();
    }
}
