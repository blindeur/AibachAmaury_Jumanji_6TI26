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



/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
namespace test_jeu
    {
    public partial class MainWindow : Window
    {
        // Dimensions du plateau
        private const int rows = 4;              // Nombre de lignes
        private const int cols = 5;              // Nombre de colonnes
        private const int totalCells = rows * cols; // Nombre total de cases

        private Rectangle[] cells;               // Tableau pour stocker les cases
        private int playerPosition = 0;          // Position actuelle du joueur

        // Contrôles dynamiques
        private Grid gameBoard;                  // Plateau de jeu
        private Button rollDiceButton;           // Bouton pour lancer le dé
        private TextBlock diceResultText;        // Affiche le résultat du dé
        private TextBlock playerPositionText;    // Affiche la position du joueur

        public MainWindow()
        {
            InitializeComponent();  // Initialisation standard WPF
            InitializeUI();         // Crée tous les contrôles dynamiquement
            InitializeBoard();      // Crée le plateau de jeu
            UpdatePlayerPosition(); // Met à jour la position du joueur sur le plateau
        }

        // ===========================
        // Création de l'interface UI
        // ===========================
        private void InitializeUI()
        {
            // Grid principal qui contiendra tous les éléments
            Grid mainGrid = new Grid();
            this.Content = mainGrid; // Définit la Grid comme contenu de la fenêtre

            // Définition des lignes : 
            // Ligne 0 : texte (dé + position joueur)
            // Ligne 1 : plateau de jeu
            // Ligne 2 : bouton lancer le dé
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });

            // -----------------------------
            // Texte du dé
            // -----------------------------
            diceResultText = new TextBlock
            {
                FontSize = 16,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(diceResultText, 0); // Ligne 0
            mainGrid.Children.Add(diceResultText);

            // -----------------------------
            // Texte de la position du joueur
            // -----------------------------
            playerPositionText = new TextBlock
            {
                FontSize = 16,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(playerPositionText, 0); // Ligne 0
            mainGrid.Children.Add(playerPositionText);

            // -----------------------------
            // Plateau de jeu (Grid)
            // -----------------------------
            gameBoard = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            // Création des lignes et colonnes de la Grid
            for (int r = 0; r < rows; r++)
                gameBoard.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            for (int c = 0; c < cols; c++)
                gameBoard.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            Grid.SetRow(gameBoard, 1); // Ligne 1 pour le plateau
            mainGrid.Children.Add(gameBoard);

            // -----------------------------
            // Bouton pour lancer le dé
            // -----------------------------
            rollDiceButton = new Button
            {
                Content = "Lancer le dé",
                Width = 120,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            rollDiceButton.Click += RollDiceButton_Click; // Événement click
            Grid.SetRow(rollDiceButton, 2); // Ligne 2
            mainGrid.Children.Add(rollDiceButton);
        }

        // ===========================
        // Création du plateau de jeu
        // ===========================
        private void InitializeBoard()
        {
            cells = new Rectangle[totalCells]; // Tableau pour stocker chaque case
            int index = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // Crée chaque case (rectangle)
                    Rectangle rect = new Rectangle
                    {
                        Width = 50,
                        Height = 50,
                        Stroke = Brushes.Black,      // Bordure noire
                        Fill = Brushes.LightGray,    // Couleur de base
                        Margin = new Thickness(2)    // Petit espacement
                    };

                    Grid.SetRow(rect, r); // Ligne
                    Grid.SetColumn(rect, c); // Colonne
                    gameBoard.Children.Add(rect); // Ajoute à la Grid
                    cells[index] = rect;          // Stocke dans le tableau
                    index++;
                }
            }
        }

        // ===========================
        // Mise à jour de la position du joueur
        // ===========================
        private void UpdatePlayerPosition()
        {
            // Reset de toutes les cases
            foreach (var cell in cells)
                cell.Fill = Brushes.LightGray;

            if (playerPosition < totalCells)
            {
                cells[playerPosition].Fill = Brushes.Green; // Case du joueur
                playerPositionText.Text = $"Position: {playerPosition + 1}";
            }
            else
            {
                playerPositionText.Text = "🎉 Vous avez gagné !";
            }
        }

        // ===========================
        // Événement du bouton lancer le dé
        // ===========================
        private void RollDiceButton_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            int dice = rnd.Next(1, 7);  // Nombre aléatoire 1 à 6
            diceResultText.Text = $"Dé: {dice}";

            playerPosition += dice;      // Avance le joueur
            if (playerPosition >= totalCells)
                playerPosition = totalCells - 1; // Empêche de dépasser la dernière case

            HandleRandomEvent();         // Gestion des événements aléatoires
            UpdatePlayerPosition();      // Mise à jour du plateau
        }

        // ===========================
        // Événements aléatoires style Jumanji
        // ===========================
        private void HandleRandomEvent()
        {
            Random rnd = new Random();
            int chance = rnd.Next(1, 6); // 20% de chance pour un événement

            if (chance == 1)
            {
                // Piège : recule le joueur
                MessageBox.Show("⚡ Un piège ! Recule de 2 cases.");
                playerPosition -= 2;
                if (playerPosition < 0) playerPosition = 0;
            }
            else if (chance == 2)
            {
                // Bonus : avance le joueur
                MessageBox.Show("🦁 Une bête sauvage ! Avance de 1 case.");
                playerPosition += 1;
                if (playerPosition >= totalCells) playerPosition = totalCells - 1;
            }
            // Tu peux ajouter d'autres événements ici
        }
    }
}