using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace test_martye
{
    /// <summary>
    /// Page de jeu principale.
    /// Le plateau n'utilise plus une grille lineaire unique :
    /// chaque joueur suit maintenant un chemin propre a sa couleur.
    /// </summary>
    public partial class GamePage : Page
    {
        // Taille historique de l'ancien plateau logique.
        // Elle sert uniquement a remapper d'anciennes sauvegardes vers les nouveaux chemins.
        private const int LegacyBoardSize = 16;

        // Chaque joueur suit des routes de 21 cases jusqu'au cristal final.
        private const int TotalPathSteps = 21;

        // Sur les nouveaux chemins, une enigme apparait toutes les 5 cases.
        private const int QuestionTriggerModulo = 5;

        // Taille d'affichage du plateau dans le Canvas.
        private const double BoardCanvasSize = 736.0;

        // Taille source du fichier image du plateau.
        private const double SourceBoardSize = 1024.0;

        // Taille visuelle d'un pion.
        private const double TokenSize = 28.0;

        private static readonly Dictionary<string, Point[]> PlayerPaths = BuildPlayerPaths();

        private readonly DatabaseManager databaseManager;
        private readonly Action returnHome;
        private readonly Random random = new();
        private readonly GameSession session;

        private GameQuestion? currentQuestion;
        private string lastEventTitle = "Event de jungle";
        private string lastEventDescription = "Lancez le de pour reveiller la jungle.";
        private string lastQuestionState = "Aucune enigme obligatoire pour l'instant.";

        /// <summary>
        /// Recharge la session, restaure une enigme si besoin et dessine le plateau.
        /// </summary>
        public GamePage(DatabaseManager databaseManager, int playerCount, Action returnHome)
        {
            InitializeComponent();
            this.databaseManager = databaseManager;
            this.returnHome = returnHome;
            session = databaseManager.LoadOrCreateSession(playerCount);

            if (session.PendingQuestionId.HasValue)
            {
                currentQuestion = databaseManager.GetQuestionById(session.PendingQuestionId.Value);
                if (currentQuestion is null)
                {
                    session.PendingQuestionId = null;
                }
                else
                {
                    lastEventTitle = "Partie reprise";
                    lastEventDescription = "Une enigme etait encore en attente au moment de la sauvegarde.";
                    lastQuestionState = "Le joueur actif doit repondre avant de pouvoir relancer le de.";
                }
            }

            BuildBoard();
            RefreshUi(currentQuestion is null
                ? "La jungle est prete. Lancez le de."
                : "La partie reprend sur une enigme obligatoire.");
        }

        /// <summary>
        /// Sauvegarde la partie courante.
        /// Si elle est terminee, on supprime plutot la sauvegarde.
        /// </summary>
        public void PersistGame()
        {
            session.PendingQuestionId = currentQuestion?.Id;

            if (session.IsFinished)
            {
                databaseManager.DeleteSavedGame(session.PlayerCount);
                return;
            }

            databaseManager.SaveGame(session);
        }

        /// <summary>
        /// Nettoie simplement le Canvas du plateau.
        /// Les pions seront ensuite redessines par UpdateBoard.
        /// </summary>
        private void BuildBoard()
        {
            BoardGrid.Children.Clear();
        }

        /// <summary>
        /// Met a jour a la fois le plateau et le panneau lateral.
        /// </summary>
        private void RefreshUi(string statusMessage)
        {
            UpdateBoard();
            UpdateSidebar(statusMessage);
        }

        /// <summary>
        /// Dessine chaque pion directement sur le Canvas, a la position correspondant
        /// au chemin de sa couleur.
        /// </summary>
        private void UpdateBoard()
        {
            BoardGrid.Children.Clear();

            foreach (var player in session.Players.OrderBy(player => player.PlayerIndex))
            {
                Point tokenPosition = GetPlayerPoint(player);
                Border token = CreateToken(player);

                Canvas.SetLeft(token, tokenPosition.X - (TokenSize / 2));
                Canvas.SetTop(token, tokenPosition.Y - (TokenSize / 2));
                BoardGrid.Children.Add(token);
            }
        }

        /// <summary>
        /// Met a jour tous les textes et les etats du panneau lateral.
        /// </summary>
        private void UpdateSidebar(string statusMessage)
        {
            PlayerCountText.Text = $"{session.PlayerCount} joueurs dans la partie";
            StatusText.Text = statusMessage;
            DiceText.Text = session.LastDice > 0
                ? $"Dernier de : {session.LastDice}"
                : "Dernier de : pas encore lance";

            LastEventText.Text = $"{lastEventTitle} : {lastEventDescription}";

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
                RollDiceButton.IsEnabled = currentQuestion is null;
                SaveAndQuitButton.Content = "Quitter et sauvegarder";
            }

            UpdateQuestionPanel();

            PlayersPanel.Children.Clear();
            foreach (var player in session.Players.OrderBy(player => player.PlayerIndex))
            {
                PlayersPanel.Children.Add(CreatePlayerSummary(player));
            }
        }

        /// <summary>
        /// Gere l'affichage de la section enigme.
        /// </summary>
        private void UpdateQuestionPanel()
        {
            if (session.IsFinished)
            {
                QuestionPromptText.Text = "La jungle s'apaise. Plus aucune enigme n'est necessaire.";
                QuestionHintText.Text = string.Empty;
                QuestionStateText.Text = lastQuestionState;
                AnswerPanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (currentQuestion is null)
            {
                QuestionPromptText.Text = "Aucune enigme obligatoire pour l'instant.";
                QuestionHintText.Text = "Les chemins colores declenchent une enigme toutes les 5 cases.";
                QuestionStateText.Text = lastQuestionState;
                AnswerTextBox.Text = string.Empty;
                AnswerPanel.Visibility = Visibility.Collapsed;
                return;
            }

            QuestionPromptText.Text = currentQuestion.QuestionText;
            QuestionHintText.Text = string.IsNullOrWhiteSpace(currentQuestion.HintText)
                ? string.Empty
                : $"Indice : {currentQuestion.HintText}";
            QuestionStateText.Text = lastQuestionState;
            AnswerPanel.Visibility = Visibility.Visible;
            AnswerTextBox.Focus();
        }

        /// <summary>
        /// Gere le tour d'un joueur :
        /// - on lance le de,
        /// - on avance sur son chemin colore,
        /// - on verifie la victoire,
        /// - on declenche une enigme ou un event.
        /// </summary>
        private void RollDiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (session.IsFinished || currentQuestion is not null)
            {
                return;
            }

            PlayerData currentPlayer = session.Players[session.CurrentTurnIndex];

            if (currentPlayer.IsBlocked)
            {
                currentPlayer.IsBlocked = false;
                session.LastDice = 0;
                lastEventTitle = "Sables mouvants";
                lastEventDescription = $"{currentPlayer.Name} etait bloque et perd ce tour.";
                lastQuestionState = "Aucune enigme obligatoire pour l'instant.";
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
                lastEventTitle = "Jumanji";
                lastEventDescription = $"{currentPlayer.Name} atteint la derniere case de son chemin et triomphe de la jungle.";
                lastQuestionState = "La partie est terminee.";
                RefreshUi($"{currentPlayer.Name} a gagne la partie.");
                MessageBox.Show($"JUMANJI ! {currentPlayer.Name} remporte la partie.");
                return;
            }

            int pathIndex = GetCurrentPathIndex(currentPlayer);
            if (ShouldAskQuestion(pathIndex))
            {
                currentQuestion = databaseManager.GetRandomQuestion();
                session.PendingQuestionId = currentQuestion?.Id;
                lastEventTitle = "Cercle des enigmes";
                lastEventDescription = $"{currentPlayer.Name} doit repondre avant de continuer sur son chemin.";

                if (currentQuestion is null)
                {
                    lastQuestionState = "Aucune question n'est disponible dans la base pour le moment.";
                    AdvanceTurn();
                    RefreshUi($"{currentPlayer.Name} n'a trouve aucune enigme dans la base.");
                    return;
                }

                lastQuestionState = "Repondez correctement pour eviter un event negatif.";
                RefreshUi($"{currentPlayer.Name} doit resoudre une enigme.");
                return;
            }

            GameEvent gameEvent = databaseManager.GetRandomEvent();
            string statusMessage = ApplyEvent(currentPlayer, gameEvent);

            if (!session.IsFinished)
            {
                AdvanceTurn();
            }

            RefreshUi(statusMessage);
        }

        /// <summary>
        /// Validation par bouton.
        /// </summary>
        private void SubmitAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            ResolveCurrentQuestion();
        }

        /// <summary>
        /// Validation par touche Entrer.
        /// </summary>
        private void AnswerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ResolveCurrentQuestion();
            }
        }

        /// <summary>
        /// Verifie la reponse a l'enigme et applique, en cas d'erreur,
        /// un event negatif sur le chemin du joueur.
        /// </summary>
        private void ResolveCurrentQuestion()
        {
            if (session.IsFinished || currentQuestion is null)
            {
                return;
            }

            PlayerData currentPlayer = session.Players[session.CurrentTurnIndex];
            string providedAnswer = AnswerTextBox.Text;
            string expectedAnswer = currentQuestion.AnswerText;

            session.PendingQuestionId = null;
            AnswerTextBox.Text = string.Empty;

            if (MatchesAnswer(expectedAnswer, providedAnswer))
            {
                lastEventTitle = "Enigme resolue";
                lastEventDescription = $"{currentPlayer.Name} a donne la bonne reponse et echappe a la colere de la jungle.";
                lastQuestionState = "Bonne reponse. Aucun event negatif n'est applique.";
                currentQuestion = null;
                AdvanceTurn();
                RefreshUi($"{currentPlayer.Name} a resolu l'enigme.");
                return;
            }

            currentQuestion = null;
            GameEvent negativeEvent = databaseManager.GetRandomEvent(negativeOnly: true);
            string consequence = ApplyEvent(currentPlayer, negativeEvent);
            lastQuestionState = $"Mauvaise reponse. Bonne reponse attendue : {GetDisplayAnswer(expectedAnswer)}.";

            if (!session.IsFinished)
            {
                AdvanceTurn();
            }

            RefreshUi($"{currentPlayer.Name} s'est trompe. {consequence}");
        }

        /// <summary>
        /// Sauvegarde puis revient au menu de selection.
        /// </summary>
        private void SaveAndQuitButton_Click(object sender, RoutedEventArgs e)
        {
            PersistGame();
            returnHome();
        }

        /// <summary>
        /// Applique un event a un joueur en utilisant sa route personnelle.
        /// </summary>
        private string ApplyEvent(PlayerData player, GameEvent gameEvent)
        {
            lastEventTitle = gameEvent.Title;
            lastEventDescription = gameEvent.Description;

            string statusMessage = $"{player.Name} traverse l'event : {gameEvent.Title}.";

            switch (gameEvent.EffectType)
            {
                case "Move":
                    MovePlayer(player, gameEvent.EffectValue);
                    statusMessage = gameEvent.EffectValue >= 0
                        ? $"{player.Name} avance encore de {gameEvent.EffectValue} case(s)."
                        : $"{player.Name} recule de {Math.Abs(gameEvent.EffectValue)} case(s).";
                    break;

                case "Block":
                    player.IsBlocked = true;
                    statusMessage = $"{player.Name} sera bloque au prochain tour.";
                    break;

                default:
                    statusMessage = $"{player.Name} subit l'event : {gameEvent.Title}.";
                    break;
            }

            if (HasPlayerWon(player))
            {
                session.IsFinished = true;
                lastEventDescription = $"{gameEvent.Description} {player.Name} atteint la fin de son chemin.";
                lastQuestionState = "La partie est terminee.";
                statusMessage = $"{player.Name} a gagne la partie.";
            }
            else if (currentQuestion is null)
            {
                lastQuestionState = "Aucune enigme obligatoire pour l'instant.";
            }

            return statusMessage;
        }

        /// <summary>
        /// Deplace un joueur sur son chemin personnel.
        /// Le plateau ne calcule plus de ligne/colonne :
        /// il calcule maintenant un index de progression dans une liste de points.
        /// </summary>
        private void MovePlayer(PlayerData player, int delta)
        {
            Point[] path = GetPathForPlayer(player);
            int currentIndex = GetCurrentPathIndex(player);
            int nextIndex = Math.Clamp(currentIndex + delta, 0, path.Length - 1);
            SetCurrentPathIndex(player, nextIndex);
        }

        /// <summary>
        /// Declenche une enigme a intervalles reguliers sur un chemin.
        /// </summary>
        private static bool ShouldAskQuestion(int pathIndex)
        {
            return pathIndex > 0 && pathIndex % QuestionTriggerModulo == 0;
        }

        /// <summary>
        /// Verifie si le joueur est arrive au bout de sa route.
        /// </summary>
        private bool HasPlayerWon(PlayerData player)
        {
            Point[] path = GetPathForPlayer(player);
            return GetCurrentPathIndex(player) >= path.Length - 1;
        }

        /// <summary>
        /// Passe au joueur suivant.
        /// </summary>
        private void AdvanceTurn()
        {
            session.CurrentTurnIndex = (session.CurrentTurnIndex + 1) % session.Players.Count;
        }

        /// <summary>
        /// Cree visuellement le pion rond d'un joueur.
        /// </summary>
        private Border CreateToken(PlayerData player)
        {
            return new Border
            {
                Width = TokenSize,
                Height = TokenSize,
                CornerRadius = new CornerRadius(TokenSize / 2),
                Background = ToBrush(player.Color),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                ToolTip = $"{player.Name} - chemin {GetPathName(player)}",
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

        /// <summary>
        /// Cree une ligne de resume dans le panneau lateral.
        /// On affiche maintenant la case dans le chemin colore plutot qu'une case de grille.
        /// </summary>
        private Border CreatePlayerSummary(PlayerData player)
        {
            bool isCurrentPlayer = !session.IsFinished && player.PlayerIndex == session.CurrentTurnIndex;
            int pathIndex = GetCurrentPathIndex(player);
            int pathLength = GetPathForPlayer(player).Length;

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
                Text = $"{player.Name} - case {pathIndex + 1}/{pathLength}" +
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

        /// <summary>
        /// Retourne le point visuel a l'ecran correspondant a la progression du joueur.
        /// </summary>
        private Point GetPlayerPoint(PlayerData player)
        {
            Point[] path = GetPathForPlayer(player);
            int pathIndex = GetCurrentPathIndex(player);
            return path[pathIndex];
        }

        /// <summary>
        /// Retourne le chemin visuel correspondant a la couleur du joueur.
        /// </summary>
        private static Point[] GetPathForPlayer(PlayerData player)
        {
            return PlayerPaths[GetPathName(player)];
        }

        /// <summary>
        /// Associe l'ordre/couleur des joueurs aux quatre routes du plateau.
        /// </summary>
        private static string GetPathName(PlayerData player)
        {
            return player.PlayerIndex switch
            {
                0 => "Red",
                1 => "Blue",
                2 => "Orange",
                3 => "Green",
                _ => "Red"
            };
        }

        /// <summary>
        /// Recupere la progression d'un joueur.
        /// Pour compatibilite, si une ancienne sauvegarde utilisait encore Row/Col comme grille,
        /// on convertit cette position en progression sur le nouveau chemin.
        /// </summary>
        private int GetCurrentPathIndex(PlayerData player)
        {
            Point[] path = GetPathForPlayer(player);

            if (player.Col == 0 && player.Row >= 0 && player.Row < path.Length)
            {
                return player.Row;
            }

            int legacyLinearPosition = (player.Row * LegacyBoardSize) + player.Col;
            double legacyProgress = legacyLinearPosition / 255.0;
            int remappedIndex = (int)Math.Round(legacyProgress * (path.Length - 1));

            return Math.Clamp(remappedIndex, 0, path.Length - 1);
        }

        /// <summary>
        /// Ecrit la nouvelle progression du joueur.
        /// Row stocke l'index de chemin et Col est remis a 0 pour marquer le nouveau systeme.
        /// </summary>
        private static void SetCurrentPathIndex(PlayerData player, int pathIndex)
        {
            player.Row = pathIndex;
            player.Col = 0;
        }

        /// <summary>
        /// Transforme une chaine de couleur en pinceau WPF.
        /// </summary>
        private static Brush ToBrush(string colorValue)
        {
            return (Brush?)new BrushConverter().ConvertFromString(colorValue) ?? Brushes.Gold;
        }

        /// <summary>
        /// Compare la reponse fournie a une ou plusieurs reponses attendues.
        /// </summary>
        private static bool MatchesAnswer(string expectedAnswers, string providedAnswer)
        {
            string normalizedProvided = NormalizeAnswer(providedAnswer);
            if (string.IsNullOrWhiteSpace(normalizedProvided))
            {
                return false;
            }

            return expectedAnswers
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeAnswer)
                .Any(answer => answer == normalizedProvided);
        }

        /// <summary>
        /// Retourne la premiere bonne reponse en version lisible.
        /// </summary>
        private static string GetDisplayAnswer(string expectedAnswers)
        {
            return expectedAnswers
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? expectedAnswers;
        }

        /// <summary>
        /// Normalise une reponse :
        /// minuscules, suppression des accents et nettoyage de la ponctuation.
        /// </summary>
        private static string NormalizeAnswer(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);

            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
                else if (char.IsWhiteSpace(character))
                {
                    builder.Append(' ');
                }
            }

            return string.Join(
                ' ',
                builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Construit les quatre chemins colores du plateau.
        /// Chaque chemin est decrit par quelques points de controle poses sur l'image d'origine,
        /// puis echantillonne automatiquement pour obtenir des cases de progression regulieres.
        /// </summary>
        private static Dictionary<string, Point[]> BuildPlayerPaths()
        {
            return new Dictionary<string, Point[]>
            {
                ["Blue"] = CreateSampledPath(
                    TotalPathSteps,
                    new Point(43, 118),
                    new Point(145, 131),
                    new Point(262, 129),
                    new Point(400, 116),
                    new Point(553, 96),
                    new Point(684, 96),
                    new Point(790, 150),
                    new Point(817, 235),
                    new Point(770, 279),
                    new Point(669, 279),
                    new Point(553, 279),
                    new Point(436, 280),
                    new Point(330, 312),
                    new Point(271, 386),
                    new Point(273, 469),
                    new Point(340, 502),
                    new Point(415, 491),
                    new Point(446, 449),
                    new Point(518, 486)),

                ["Red"] = CreateSampledPath(
                    TotalPathSteps,
                    new Point(67, 875),
                    new Point(70, 731),
                    new Point(68, 564),
                    new Point(66, 388),
                    new Point(74, 274),
                    new Point(166, 245),
                    new Point(256, 271),
                    new Point(298, 339),
                    new Point(296, 453),
                    new Point(290, 592),
                    new Point(330, 698),
                    new Point(424, 739),
                    new Point(489, 716),
                    new Point(501, 655),
                    new Point(500, 610),
                    new Point(518, 486)),

                ["Green"] = CreateSampledPath(
                    TotalPathSteps,
                    new Point(912, 878),
                    new Point(780, 851),
                    new Point(623, 829),
                    new Point(458, 811),
                    new Point(300, 781),
                    new Point(195, 744),
                    new Point(181, 695),
                    new Point(247, 669),
                    new Point(380, 667),
                    new Point(512, 668),
                    new Point(650, 670),
                    new Point(760, 639),
                    new Point(814, 580),
                    new Point(804, 522),
                    new Point(740, 486),
                    new Point(663, 476),
                    new Point(593, 476),
                    new Point(518, 486)),

                ["Orange"] = CreateSampledPath(
                    TotalPathSteps,
                    new Point(954, 116),
                    new Point(955, 267),
                    new Point(955, 442),
                    new Point(953, 628),
                    new Point(937, 721),
                    new Point(874, 773),
                    new Point(782, 759),
                    new Point(721, 703),
                    new Point(700, 608),
                    new Point(704, 487),
                    new Point(710, 367),
                    new Point(697, 284),
                    new Point(632, 251),
                    new Point(562, 244),
                    new Point(524, 298),
                    new Point(519, 364),
                    new Point(518, 486))
            };
        }

        /// <summary>
        /// Echantillonne une polyline definie sur l'image source,
        /// puis adapte le resultat a la taille affichee du plateau.
        /// </summary>
        private static Point[] CreateSampledPath(int stepCount, params Point[] sourceWaypoints)
        {
            if (sourceWaypoints.Length < 2)
            {
                throw new ArgumentException("Un chemin doit contenir au moins deux points.");
            }

            double totalLength = 0;
            for (int i = 1; i < sourceWaypoints.Length; i++)
            {
                totalLength += Distance(sourceWaypoints[i - 1], sourceWaypoints[i]);
            }

            double spacing = totalLength / (stepCount - 1);
            var sampledPoints = new List<Point>(stepCount);
            sampledPoints.Add(ScalePoint(sourceWaypoints[0]));

            double accumulatedLength = 0;
            int currentSegment = 1;

            for (int step = 1; step < stepCount - 1; step++)
            {
                double targetLength = spacing * step;

                while (currentSegment < sourceWaypoints.Length &&
                       accumulatedLength + Distance(sourceWaypoints[currentSegment - 1], sourceWaypoints[currentSegment]) < targetLength)
                {
                    accumulatedLength += Distance(sourceWaypoints[currentSegment - 1], sourceWaypoints[currentSegment]);
                    currentSegment++;
                }

                if (currentSegment >= sourceWaypoints.Length)
                {
                    break;
                }

                Point start = sourceWaypoints[currentSegment - 1];
                Point end = sourceWaypoints[currentSegment];
                double segmentLength = Distance(start, end);
                double localDistance = targetLength - accumulatedLength;
                double ratio = segmentLength <= 0 ? 0 : localDistance / segmentLength;

                double x = start.X + ((end.X - start.X) * ratio);
                double y = start.Y + ((end.Y - start.Y) * ratio);
                sampledPoints.Add(ScalePoint(new Point(x, y)));
            }

            sampledPoints.Add(ScalePoint(sourceWaypoints[^1]));
            return sampledPoints.ToArray();
        }

        /// <summary>
        /// Met a l'echelle un point defini sur l'image source 1024x1024
        /// vers la taille du Canvas du plateau.
        /// </summary>
        private static Point ScalePoint(Point sourcePoint)
        {
            double scale = BoardCanvasSize / SourceBoardSize;
            return new Point(sourcePoint.X * scale, sourcePoint.Y * scale);
        }

        /// <summary>
        /// Calcule la distance entre deux points.
        /// </summary>
        private static double Distance(Point firstPoint, Point secondPoint)
        {
            double deltaX = secondPoint.X - firstPoint.X;
            double deltaY = secondPoint.Y - firstPoint.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }
    }
}
