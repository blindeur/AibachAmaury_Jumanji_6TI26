using System.ComponentModel;
using System.Windows;

namespace test_martye
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseManager databaseManager = new();
        private GamePage? activeGamePage;

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            ShowHomePage();
            player.Play();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            player.Play();
        }

        private void player_MediaEnded(object sender, RoutedEventArgs e)
        {
            player.Position = TimeSpan.Zero;
            player.Play();
        }

        private void ShowHomePage()
        {
            activeGamePage = null;
            MainFrame.Navigate(new HomePage(databaseManager, StartGame));
        }

        private void StartGame(int playerCount)
        {
            activeGamePage = new GamePage(databaseManager, playerCount, ShowHomePage);
            MainFrame.Navigate(activeGamePage);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            activeGamePage?.PersistGame();
        }
    }
}
