using System;
using System.Windows;
using System.Windows.Controls;

namespace test_martye
{
    /// <summary>
    /// Menu de selection du nombre de joueurs.
    /// Cette page ne lance pas directement le jeu au chargement :
    /// elle attend que le joueur choisisse 2, 3 ou 4 participants.
    /// </summary>
    public partial class HomePage : Page
    {
        private readonly DatabaseManager databaseManager;
        private readonly Action<int> startGame;

        /// <summary>
        /// Recoit la reference vers la base de donnees et l'action qui lance la partie.
        /// </summary>
        public HomePage(DatabaseManager databaseManager, Action<int> startGame)
        {
            InitializeComponent();
            this.databaseManager = databaseManager;
            this.startGame = startGame;
        }

        /// <summary>
        /// Au chargement de la page, on verifie pour chaque configuration
        /// s'il existe deja une sauvegarde.
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TwoPlayersSaveText.Text = BuildSaveMessage(2);
            ThreePlayersSaveText.Text = BuildSaveMessage(3);
            FourPlayersSaveText.Text = BuildSaveMessage(4);
        }

        /// <summary>
        /// Recupere le nombre de joueurs depuis la propriete Tag du bouton clique,
        /// puis lance la page de jeu.
        /// </summary>
        private void PlayerCountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag || !int.TryParse(tag, out int playerCount))
            {
                return;
            }

            startGame(playerCount);
        }

        /// <summary>
        /// Construit le texte d'information sous chaque bouton.
        /// </summary>
        private string BuildSaveMessage(int playerCount)
        {
            return databaseManager.HasSavedGame(playerCount)
                ? $"Une partie {playerCount} joueurs est deja sauvegardee. Elle sera reprise."
                : $"Nouvelle partie pour {playerCount} joueurs.";
        }
    }
}
