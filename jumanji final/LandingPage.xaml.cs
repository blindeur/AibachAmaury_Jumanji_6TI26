using System;
using System.Windows;
using System.Windows.Controls;

namespace test_martye
{
    /// <summary>
    /// Page d'accueil du jeu.
    /// Son seul role est de presenter l'univers visuel et d'envoyer le joueur vers
    /// le menu de selection du nombre de participants.
    /// </summary>
    public partial class LandingPage : Page
    {
        private readonly Action openSelectionMenu;

        /// <summary>
        /// Recoit l'action a executer quand le joueur clique sur le bouton "Jouer".
        /// </summary>
        public LandingPage(Action openSelectionMenu)
        {
            InitializeComponent();
            this.openSelectionMenu = openSelectionMenu;
        }

        /// <summary>
        /// Ouvre le menu de selection des participants.
        /// </summary>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            openSelectionMenu();
        }
    }
}
