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
        private Button[,] btnB;
        private List<PlayerData> listeJoueurs;
        private int tourIndex = 0;
        private int tailleGrille = 16;
        private Random rnd = new Random();
        private DatabaseManager db = new DatabaseManager();

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
                        BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), // Bordure discrète
                        BorderThickness = new Thickness(0.5)
                    };
                    Grid.SetRow(btnB[i, j], i);
                    Grid.SetColumn(btnB[i, j], j);
                    zonePlateau.Children.Add(btnB[i, j]);
                }
            }
        }

        private void UpdatePositionsVisuelles()
        {
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