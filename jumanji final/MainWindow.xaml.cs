using System;
using System.ComponentModel;
using System.Windows;
using MySql.Data.MySqlClient;

namespace test_martye
{
    /// <summary>
    /// Fenetre principale de l'application.
    /// Elle centralise trois responsabilites :
    /// 1. initialiser la base de donnees,
    /// 2. gerer la navigation entre les pages,
    /// 3. gerer la musique de fond commune a tout le jeu.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseManager databaseManager = null!;
        private GamePage? activeGamePage;
        private bool isSoundEnabled = true;

        /// <summary>
        /// Au demarrage :
        /// - on initialise l'interface,
        /// - on tente la connexion MySQL,
        /// - on affiche l'accueil,
        /// - on lance la musique.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                databaseManager = new DatabaseManager();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(
                    "Connexion MySQL impossible.\n\n" +
                    "Verifie que MySQL est demarre, que le login/mot de passe sont corrects, " +
                    "et que la base est accessible.\n\n" +
                    $"Detail : {ex.Message}",
                    "Erreur de connexion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Le jeu n'a pas pu initialiser la base de donnees.\n\n" +
                    $"Detail : {ex.Message}",
                    "Erreur de demarrage",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
                return;
            }

            Closing += MainWindow_Closing;

            // Le joueur arrive d'abord sur la vraie page d'accueil.
            ShowLandingPage();

            UpdateSoundButton();
            player.Play();
        }

        /// <summary>
        /// Quand la piste audio se termine, on la relance depuis le debut
        /// pour garder une ambiance continue.
        /// </summary>
        private void player_MediaEnded(object sender, RoutedEventArgs e)
        {
            player.Position = TimeSpan.Zero;
            player.Play();
        }

        /// <summary>
        /// Coupe ou reactive la musique sans fermer l'application.
        /// </summary>
        private void ToggleSoundButton_Click(object sender, RoutedEventArgs e)
        {
            isSoundEnabled = !isSoundEnabled;
            player.IsMuted = !isSoundEnabled;
            UpdateSoundButton();
        }

        /// <summary>
        /// Affiche la toute premiere page : l'accueil avec le bouton Jouer.
        /// </summary>
        private void ShowLandingPage()
        {
            activeGamePage = null;
            MainFrame.Navigate(new LandingPage(ShowSelectionPage));
        }

        /// <summary>
        /// Affiche le menu de selection du nombre de joueurs.
        /// </summary>
        private void ShowSelectionPage()
        {
            activeGamePage = null;
            MainFrame.Navigate(new HomePage(databaseManager, StartGame));
        }

        /// <summary>
        /// Cree la page de jeu en fonction du nombre de participants choisi.
        /// </summary>
        private void StartGame(int playerCount)
        {
            activeGamePage = new GamePage(databaseManager, playerCount, ShowSelectionPage);
            MainFrame.Navigate(activeGamePage);
        }

        /// <summary>
        /// Sauvegarde la partie avant la fermeture si une partie est en cours.
        /// </summary>
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            activeGamePage?.PersistGame();
        }

        /// <summary>
        /// Met a jour le texte du bouton de son selon l'etat actuel.
        /// </summary>
        private void UpdateSoundButton()
        {
            ToggleSoundButton.Content = isSoundEnabled ? "Couper le son" : "Activer le son";
        }
    }
}
