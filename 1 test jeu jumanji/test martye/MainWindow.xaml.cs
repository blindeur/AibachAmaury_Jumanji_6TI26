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

namespace test_martye
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;


    public partial class MainWindow : Window
    {
        private Button[,] btnB;   // Plateau 16x16
        private int playerRow = 0;
        private int playerCol = 0;
        private Random rnd = new Random();
        private int tailleGrille = 16;

        public MainWindow()
        {
            InitializeComponent();
            CreerGrille();
            UpdatePlayerPosition();
        }

        // ===========================
        // Créer la grille dynamique
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
                        IsEnabled = false // invisibles et non cliquables
                    };

                    Grid.SetRow(btnB[i, j], i);
                    Grid.SetColumn(btnB[i, j], j);
                    zonePlateau.Children.Add(btnB[i, j]);
                }
            }
        }


        // ===========================
        // Mise à jour position joueur
        // ===========================
        private void UpdatePlayerPosition()
        {
            for (int i = 0; i < tailleGrille; i++)
                for (int j = 0; j < tailleGrille; j++)
                    btnB[i, j].Background = Brushes.Transparent;

            btnB[playerRow, playerCol].Background = Brushes.Green;
        }

        // ===========================
        // Lancer de dé
        // ===========================
        private void btnLancerDe_Click(object sender, RoutedEventArgs e)
        {
            int dice = rnd.Next(1, 7);
            //lblDe.Text = dice.ToString();

            playerCol += dice;

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
                MessageBox.Show("🎉 Vous avez atteint la fin du plateau !");
                return;
            }

            UpdatePlayerPosition();
        }
    }
}