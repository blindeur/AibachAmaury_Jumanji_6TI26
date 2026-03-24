using System;
using System.Windows;
using System.Windows.Controls;

namespace test_martye
{
    public partial class HomePage : Page
    {
        private readonly DatabaseManager databaseManager;
        private readonly Action<int> startGame;

        public HomePage(DatabaseManager databaseManager, Action<int> startGame)
        {
            InitializeComponent();   
            this.databaseManager = databaseManager;
            this.startGame = startGame;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TwoPlayersSaveText.Text = BuildSaveMessage(2);
            ThreePlayersSaveText.Text = BuildSaveMessage(3);
            FourPlayersSaveText.Text = BuildSaveMessage(4);
        }

        private void PlayerCountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag || !int.TryParse(tag, out int playerCount))
            {
                return;
            }

            startGame(playerCount);
        }

        private string BuildSaveMessage(int playerCount)
        {
            return databaseManager.HasSavedGame(playerCount)
                ? $"Une partie {playerCount} joueurs est deja sauvegardee. Elle sera reprise."
                : $"Nouvelle partie pour {playerCount} joueurs.";
        }
    }
}
