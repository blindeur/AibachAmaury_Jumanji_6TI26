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
<<<<<<< HEAD
        private Button[,] btnB;
        private List<PlayerData> listeJoueurs;
        private int tourIndex = 0;
        private int tailleGrille = 16;
        private Random rnd = new Random();
        private DatabaseManager db = new DatabaseManager();
=======
        private Button[,] btnB;   // Plateau 16x16
        private int playerRow = 1;
        private int playerCol = 0;
        private Random rnd = new Random();
        private int tailleGrille = 16;
        private int[] colPath = {1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 2 }; // 0 gauche, 1 pas bouger, 2 droite
        private int[] rowPath = {1, 2, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 2, 2, 2, 2, 1, 1, 1, 1, 0, 1, 2, 1, 2, 2, 1, 1, 1 }; // 0 haut, 1 pas bouger, 2 bas
        private int playerProgression = 0;
>>>>>>> 125dd22f339afef55fd3f37716130da6716e5086

        public MainWindow()
        {
            InitializeComponent();

            // 1. Charger les joueurs
            listeJoueurs = db.LoadPlayers();

            // 2. Initialiser le plateau
            CreerGrille();
            UpdatePositionsVisuelles();

            // 3. Sauvegarder en quittant
            this.Closing += (s, e) => db.SavePlayers(listeJoueurs);
        }

        private void CreerGrille()
        {
            zonePlateau.Children.Clear();
            btnB = new Button[tailleGrille, tailleGrille];

            for (int i = 0; i < tailleGrille; i++)
            {
                zonePlateau.RowDefinitions.Add(new RowDefinition());
                zonePlateau.ColumnDefinitions.Add(new ColumnDefinition());
                for (int j = 0; j < tailleGrille; j++)
                {
                    btnB[i, j] = new Button
                    {
                        Background = Brushes.Transparent,
<<<<<<< HEAD
                        BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), // Bordure discrète
                        BorderThickness = new Thickness(0.5)
=======
                        BorderBrush = Brushes.Transparent,
                        BorderThickness = new Thickness(0.5),
                        
>>>>>>> 125dd22f339afef55fd3f37716130da6716e5086
                    };
                    Grid.SetRow(btnB[i, j], i);
                    Grid.SetColumn(btnB[i, j], j);
                    zonePlateau.Children.Add(btnB[i, j]);
                }
            }
        }

        private void UpdatePositionsVisuelles()
        {
<<<<<<< HEAD
            foreach (var btn in btnB) { btn.Background = Brushes.Transparent; btn.Content = ""; }

            foreach (var p in listeJoueurs)
            {
                var color = (Brush)new BrushConverter().ConvertFromString(p.Color);
                btnB[p.Row, p.Col].Background = color;
                btnB[p.Row, p.Col].Content = p.Name.Substring(0, 1);
            }

            // Mise à jour de ton interface XAML
            lblJoeur.Text = listeJoueurs[tourIndex].Name;
        }

        private void btnLancerDe_Click(object sender, RoutedEventArgs e)
        {
            PlayerData jActuel = listeJoueurs[tourIndex];

            if (jActuel.IsBlocked)
            {
                MessageBox.Show($"{jActuel.Name} est bloqué ! Passe son tour.");
                jActuel.IsBlocked = false;
                PasserAuSuivant();
                return;
            }
=======
            for (int i = 0; i < tailleGrille; i++)
            {
                for (int j = 0; j < tailleGrille; j++)
                {
                    btnB[i, j].Background = Brushes.Transparent;
                }
            }
            btnB[playerRow, playerCol].Background = Brushes.Green;
        }

        // ===========================
        // Lancer de dé
        // ===========================
        private void btnLancerDe_Click(object sender, RoutedEventArgs e)
        {
            //int dice = 1;
            int dice = rnd.Next(1, 1);
            //lblDe.Text = dice.ToString();
            for (int i = 0; i < dice; i++)
            {
                playerProgression++;
                playerCol += colPath[playerProgression] - 1;
                playerRow += rowPath[playerProgression] - 1;
            }

            //while (playerCol >= tailleGrille)
            //{
            //    playerCol -= tailleGrille;
            //    playerRow++;
            //}

            //if (playerRow >= tailleGrille)
            //{
            //    playerRow = tailleGrille - 1;
            //    playerCol = tailleGrille - 1;
            //    UpdatePlayerPosition();
            //    MessageBox.Show("🎉 Vous avez atteint la fin du plateau !");
            //    return;
            //}
>>>>>>> 125dd22f339afef55fd3f37716130da6716e5086

            int dice = rnd.Next(1, 7);
            lbDe.Text = dice.ToString(); // Affiche le dé dans ton TextBlock "lbDe"

            DeplacerJoueur(jActuel, dice);

            // Logique Jumanji
            int pos = (jActuel.Row * tailleGrille) + jActuel.Col;
            if (pos % 7 == 0 && pos != 0)
            {
                MessageBox.Show(db.GetRandomEnigme() + "\n\nMAUDIT ! Recule de 3.");
                DeplacerJoueur(jActuel, -3);
            }
            else if (pos % 13 == 0 && pos != 0)
            {
                MessageBox.Show("SABLES MOUVANTS ! Prochain tour bloqué.");
                jActuel.IsBlocked = true;
            }
            else
            {
                MessageBox.Show(db.GetRandomEnigme());
            }

            UpdatePositionsVisuelles();
            PasserAuSuivant();
        }

        private void DeplacerJoueur(PlayerData p, int nb)
        {
            int posLineaire = (p.Row * tailleGrille) + p.Col + nb;
            if (posLineaire < 0) posLineaire = 0;
            if (posLineaire >= 255)
            {
                posLineaire = 255;
                MessageBox.Show("JUMANJI ! Victoire de " + p.Name);
            }
            p.Row = posLineaire / tailleGrille;
            p.Col = posLineaire % tailleGrille;
        }

        private void PasserAuSuivant()
        {
            tourIndex = (tourIndex + 1) % listeJoueurs.Count;
            lblJoeur.Text = listeJoueurs[tourIndex].Name;
        }
    }
}