using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace test_martye
{
    public partial class GamePage : Page
    {
        private const int BoardSize = 16;

        private readonly DatabaseManager databaseManager;
        private readonly Action returnHome;
        private readonly Random random = new();
        private readonly GameSession session;

        private Button[,] boardButtons = null!;

        public GamePage(DatabaseManager databaseManager, int playerCount, Action returnHome)
        {
            InitializeComponent();
            this.databaseManager = databaseManager;
            this.returnHome = returnHome;
            session = databaseManager.LoadOrCreateSession(playerCount);

            BuildBoard();
            RefreshUi("La jungle est prete. Lancez le de.");
        }

        public void PersistGame()
        {
            if (session.IsFinished)
            {
                databaseManager.DeleteSavedGame(session.PlayerCount);
                return;
            }

            databaseManager.SaveGame(session);
        }

        private void BuildBoard()
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            boardButtons = new Button[BoardSize, BoardSize];

            for (int row = 0; row < BoardSize; row++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition());
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());

                for (int col = 0; col < BoardSize; col++)
                {
                    var button = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(2),
                        Focusable = false,
                        IsHitTestVisible = false
                    };

                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    BoardGrid.Children.Add(button);
                    boardButtons[row, col] = button;
                }
            }
        }

        private void RefreshUi(string statusMessage)
        {
            UpdateBoard();
            UpdateSidebar(statusMessage);
        }

        private void UpdateBoard()
        {
            foreach (var button in boardButtons)
            {
                button.Background = Brushes.Transparent;
                button.Content = null;
            }

            foreach (var cell in session.Players.GroupBy(player => (player.Row, player.Col)))
            {
                var button = boardButtons[cell.Key.Row, cell.Key.Col];
                var wrapPanel = new WrapPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                foreach (var player in cell.OrderBy(player => player.PlayerIndex))
                {
                    wrapPanel.Children.Add(CreateToken(player));
                }

                button.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
                button.Content = wrapPanel;
            }
        }

        private void UpdateSidebar(string statusMessage)
        {
            PlayerCountText.Text = $"{session.PlayerCount} joueurs dans la partie";
            StatusText.Text = statusMessage;
            DiceText.Text = session.LastDice > 0
                ? $"Dernier de : {session.LastDice}"
                : "Dernier de : pas encore lance";

            if (session.IsFinished)
            {
                CurrentPlayerText.Text = "Partie terminee";
                CurrentPlayerText.Foreground = Brushes.DarkGreen;
                RollDiceButton.IsEnabled = false;
                SaveAndQuitButton.Content = "Retour a l'accueil";
            }
            else
            {
                PlayerData currentPlayer = session.Players[session.CurrentTurnIndex];
                CurrentPlayerText.Text = currentPlayer.Name;
                CurrentPlayerText.Foreground = ToBrush(currentPlayer.Color);
                RollDiceButton.IsEnabled = true;
                SaveAndQuitButton.Content = "Quitter et sauvegarder";
            }

            PlayersPanel.Children.Clear();
            foreach (var player in session.Players.OrderBy(player => player.PlayerIndex))
            {
                PlayersPanel.Children.Add(CreatePlayerSummary(player));
            }
        }

        private void RollDiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (session.IsFinished)
            {
                return;
            }

            PlayerData currentPlayer = session.Players[session.CurrentTurnIndex];

            if (currentPlayer.IsBlocked)
            {
                currentPlayer.IsBlocked = false;
                session.LastDice = 0;
                QuestionText.Text = "Les sables mouvants relachent enfin leur prise.";
                MessageBox.Show($"{currentPlayer.Name} etait bloque et passe son tour.");
                AdvanceTurn();
                RefreshUi($"{currentPlayer.Name} passe son tour.");
                return;
            }

            int dice = random.Next(1, 7);
            session.LastDice = dice;
            MovePlayer(currentPlayer, dice);

            if (HasPlayerWon(currentPlayer))
            {
                session.IsFinished = true;
                QuestionText.Text = $"{currentPlayer.Name} a atteint la derniere case et gagne la partie.";
                MessageBox.Show($"JUMANJI ! {currentPlayer.Name} remporte la partie.");
                RefreshUi($"{currentPlayer.Name} a gagne.");
                return;
            }

            string enigme = databaseManager.GetRandomEnigme();
            string statusMessage;

            int linearPosition = (currentPlayer.Row * BoardSize) + currentPlayer.Col;
            if (linearPosition % 7 == 0 && linearPosition != 0)
            {
                MovePlayer(currentPlayer, -3);
                statusMessage = $"{currentPlayer.Name} est maudit et recule de 3 cases.";
                MessageBox.Show($"{enigme}\n\nMaudit ! Recule de 3 cases.");
            }
            else if (linearPosition % 13 == 0 && linearPosition != 0)
            {
                currentPlayer.IsBlocked = true;
                statusMessage = $"{currentPlayer.Name} est bloque au prochain tour.";
                MessageBox.Show($"{enigme}\n\nSables mouvants ! Le prochain tour sera bloque.");
            }
            else
            {
                statusMessage = $"{currentPlayer.Name} avance de {dice} cases.";
                MessageBox.Show(enigme);
            }

            QuestionText.Text = enigme;
            AdvanceTurn();
            RefreshUi(statusMessage);
        }

        private void SaveAndQuitButton_Click(object sender, RoutedEventArgs e)
        {
            PersistGame();
            returnHome();
        }

        private void MovePlayer(PlayerData player, int delta)
        {
            int maxPosition = (BoardSize * BoardSize) - 1;
            int linearPosition = (player.Row * BoardSize) + player.Col + delta;

            if (linearPosition < 0)
            {
                linearPosition = 0;
            }

            if (linearPosition > maxPosition)
            {
                linearPosition = maxPosition;
            }

            player.Row = linearPosition / BoardSize;
            player.Col = linearPosition % BoardSize;
        }

        private bool HasPlayerWon(PlayerData player)
        {
            return ((player.Row * BoardSize) + player.Col) >= (BoardSize * BoardSize) - 1;
        }

        private void AdvanceTurn()
        {
            session.CurrentTurnIndex = (session.CurrentTurnIndex + 1) % session.Players.Count;
        }

        private Border CreateToken(PlayerData player)
        {
            return new Border
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(14),
                Background = ToBrush(player.Color),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = player.Initial,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            };
        }

        private Border CreatePlayerSummary(PlayerData player)
        {
            bool isCurrentPlayer = !session.IsFinished && player.PlayerIndex == session.CurrentTurnIndex;

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            row.Children.Add(new Border
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 2, 10, 0),
                CornerRadius = new CornerRadius(8),
                Background = ToBrush(player.Color)
            });

            row.Children.Add(new TextBlock
            {
                Text = $"{player.Name} - case {(player.Row * BoardSize) + player.Col + 1}" +
                       (player.IsBlocked ? " - bloque" : string.Empty),
                FontSize = 15,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#203518")),
                TextWrapping = TextWrapping.Wrap
            });

            return new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(12),
                Background = isCurrentPlayer
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7E9B8"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8EE")),
                Child = row
            };
        }

        private static Brush ToBrush(string colorValue)
        {
            return (Brush?)new BrushConverter().ConvertFromString(colorValue) ?? Brushes.Gold;
        }
    }
}
