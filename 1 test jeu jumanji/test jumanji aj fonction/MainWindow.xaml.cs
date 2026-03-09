using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace test_jumanji_aj_fonction
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Button[,] btnB;
        private int playerRow = 0;
        private int playerCol = 0;

        private const int tailleGrille = 16;
        private const int totalCases = tailleGrille * tailleGrille;

        private Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            CreerGrille();
            UpdatePlayerPosition();
        }

        // ===========================
        // Création de la grille
        // ===========================
        private void CreerGrille()
        {
            zonePlateau.Children.Clear();
            zonePlateau.RowDefinitions.Clear();
            zonePlateau.ColumnDefinitions.Clear();

            btnB = new Button[tailleGrille, tailleGrille];

            for (int i = 0; i < tailleGrille; i++)
            {
                zonePlateau.RowDefinitions.Add(new RowDefinition());
                zonePlateau.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < tailleGrille; i++)
            {
                for (int j = 0; j < tailleGrille; j++)
                {
                    btnB[i, j] = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                        BorderThickness = new Thickness(0.5),
                        
                    };

                    Grid.SetRow(btnB[i, j], i);
                    Grid.SetColumn(btnB[i, j], j);
                    zonePlateau.Children.Add(btnB[i, j]);
                }
            }
        }

        // ===========================
        // Mise à jour joueur
        // ===========================
        private void UpdatePlayerPosition()
        {
            for (int i = 0; i < tailleGrille; i++)
                for (int j = 0; j < tailleGrille; j++)
                    btnB[i, j].Background = Brushes.Transparent;

            btnB[playerRow, playerCol].Background = Brushes.Green;

            int position = playerRow * tailleGrille + playerCol + 1;
            lblPosition.Text = $"Position : {position}";
        }

        // ===========================
        // Lancer de dé
        // ===========================
        private void btnLancerDe_Click(object sender, RoutedEventArgs e)
        {
            int dice = rnd.Next(1, 7);
            lblDe.Text = dice.ToString();

            AvancerJoueur(dice);
            EvenementAleatoire();
            UpdatePlayerPosition();
        }

        // ===========================
        // Déplacement logique
        // ===========================
        private void AvancerJoueur(int pas)
        {
            playerCol += pas;

            while (playerCol >= tailleGrille)
            {
                playerCol -= tailleGrille;
                playerRow++;
            }

            if (playerRow >= tailleGrille)
            {
                playerRow = tailleGrille - 1;
                playerCol = tailleGrille - 1;
                UpdatePlayerPosition();
                MessageBox.Show("🎉 Vous avez gagné !");
            }
        }

        // ===========================
        // Événements Jumanji
        // ===========================
        private void EvenementAleatoire()
        {
            int chance = rnd.Next(1, 6);

            if (chance == 1)
            {
                MessageBox.Show("⚡ Piège ! Recule de 2 cases.");
                Reculer(2);
            }
            else if (chance == 2)
            {
                MessageBox.Show("🦁 Une bête sauvage ! Avance de 1 case.");
                AvancerJoueur(1);
            }
        }

        private void Reculer(int pas)
        {
            int pos = playerRow * tailleGrille + playerCol - pas;
            if (pos < 0) pos = 0;

            playerRow = pos / tailleGrille;
            playerCol = pos % tailleGrille;
        }
    }
}