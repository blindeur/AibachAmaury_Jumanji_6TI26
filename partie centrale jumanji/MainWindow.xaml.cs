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
namespace partie_centrale_jumanji
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Button[,] btnB = new Button[16, 16]; // Plateau 16x16
        private Button rollDiceButton;               // Bouton lancer dé
        private TextBlock diceResultText;            // Affiche résultat dé
        private int playerRow = 0;
        private int playerCol = 0;
        private Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            PrepaGrille();       // Crée la grille dynamique transparente
            InitializeUI();      // Crée bouton lancer dé + texte
            UpdatePlayerPosition(); // Affiche position initiale
        }

        // ===========================
        // Méthode PrepaGrille() - grille 16x16 transparente
        // ===========================
        public void PrepaGrille()
        {
            // Nettoyer la grille si nécessaire
            grdMain.Children.Clear();
            grdMain.RowDefinitions.Clear();
            grdMain.ColumnDefinitions.Clear();

            // 16 colonnes
            for (int i = 0; i < 16; i++)
                grdMain.ColumnDefinitions.Add(new ColumnDefinition());

            // 16 lignes
            for (int j = 0; j < 16; j++)
                grdMain.RowDefinitions.Add(new RowDefinition());

            // Création des boutons
            for (int i = 0; i < 16; i++)
            {
                for (int k = 0; k < 16; k++)
                {
                    btnB[i, k] = new Button
                    {
                        Background = Brushes.Transparent, // Transparent pour voir le plateau
                        BorderBrush = Brushes.Gray,       // Optionnel : petite bordure
                        BorderThickness = new Thickness(0.2),
                        IsEnabled = false                 // Non cliquable
                    };

                    Grid.SetColumn(btnB[i, k], k);
                    Grid.SetRow(btnB[i, k], i);
                    grdMain.Children.Add(btnB[i, k]);
                }
            }
        }

        // ===========================
        // Bouton lancer dé et texte
        // ===========================
        private void InitializeUI()
        {
            // Bouton Lancer Dé
            rollDiceButton = new Button
            {
                Content = "Lancer le dé",
                Width = 120,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10)
            };
            rollDiceButton.Click += RollDiceButton_Click;
            grdMain.Children.Add(rollDiceButton);

            // Texte résultat
            diceResultText = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Black,
                Width = 80,
                Height = 30,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10)
            };
            grdMain.Children.Add(diceResultText);
        }

        // ===========================
        // Mise à jour position joueur
        // ===========================
        private void UpdatePlayerPosition()
        {
            for (int i = 0; i < 16; i++)
                for (int k = 0; k < 16; k++)
                    btnB[i, k].Background = Brushes.Transparent;

            // Joueur en vert
            btnB[playerRow, playerCol].Background = Brushes.Green;
        }

        // ===========================
        // Lancer le dé
        // ===========================
        private void RollDiceButton_Click(object sender, RoutedEventArgs e)
        {
            int dice = rnd.Next(1, 7);
            diceResultText.Text = $"Dé: {dice}";

            playerCol += dice;

            while (playerCol >= 16)
            {
                playerCol -= 16;
                playerRow++;
            }

            if (playerRow >= 16)
            {
                playerRow = 15;
                playerCol = 15;
                UpdatePlayerPosition();
                MessageBox.Show("🎉 Vous avez atteint la fin du plateau !");
                return;
            }

            UpdatePlayerPosition();
        }
    }
}