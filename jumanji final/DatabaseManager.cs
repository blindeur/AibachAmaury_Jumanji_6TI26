using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace test_martye
{
    /// <summary>
    /// Centralise toutes les operations MySQL du projet.
    /// Cette classe sait :
    /// - creer la base si besoin,
    /// - creer les tables,
    /// - charger une session,
    /// - sauvegarder une session,
    /// - recuperer des events et des questions.
    /// </summary>
    public sealed class DatabaseManager
    {
        // Couleurs par defaut attribuees aux joueurs au moment de la creation d'une nouvelle partie.
        private static readonly string[] DefaultColors = { "#C62828", "#1565C0", "#EF6C00", "#2E7D32" };

        // Parametres de connexion figes selon la configuration donnee par l'utilisateur.
        private readonly string serverName;
        private readonly uint port;
        private readonly string databaseName;
        private readonly string userName;
        private readonly string password;

        /// <summary>
        /// Initialise la configuration de connexion puis prepare la base.
        /// </summary>
        public DatabaseManager()
        {
            serverName = "127.0.0.1";
            port = 3306;
            databaseName = "amaury";
            userName = "root";
            password = "Amaury2006@";

            EnsureDatabase();
        }

        /// <summary>
        /// Petite synthese utile pour afficher ou diagnostiquer la connexion.
        /// </summary>
        public string ConnectionSummary =>
            $"serveur={serverName}; port={port}; base={databaseName}; utilisateur={userName}";

        /// <summary>
        /// Verifie s'il existe deja une sauvegarde pour 2, 3 ou 4 joueurs.
        /// </summary>
        public bool HasSavedGame(int playerCount)
        {
            ValidatePlayerCount(playerCount);

            using var connection = OpenDatabaseConnection();

            const string sql = """
                SELECT 1
                FROM GameSaves
                WHERE playerCount = @playerCount
                LIMIT 1;
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@playerCount", MySqlDbType.UByte).Value = playerCount;

            return command.ExecuteScalar() is not null;
        }

        /// <summary>
        /// Charge la session sauvegardee si elle existe.
        /// Sinon, cree une nouvelle session propre.
        /// </summary>
        public GameSession LoadOrCreateSession(int playerCount)
        {
            ValidatePlayerCount(playerCount);

            using var connection = OpenDatabaseConnection();

            var savedSession = LoadSavedSession(connection, playerCount);
            return savedSession ?? CreateNewSession(playerCount);
        }

        /// <summary>
        /// Sauvegarde la partie courante dans la base :
        /// - la ligne principale dans GameSaves,
        /// - les joueurs dans GamePlayers.
        /// Tout est fait dans une transaction pour garder une sauvegarde coherente.
        /// </summary>
        public void SaveGame(GameSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            ValidatePlayerCount(session.PlayerCount);

            using var connection = OpenDatabaseConnection();
            using var transaction = connection.BeginTransaction();

            const string upsertSessionSql = """
                INSERT INTO GameSaves (playerCount, currentTurnIndex, lastDice, pendingQuestionId, updatedAt)
                VALUES (@playerCount, @currentTurnIndex, @lastDice, @pendingQuestionId, UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE
                    currentTurnIndex = VALUES(currentTurnIndex),
                    lastDice = VALUES(lastDice),
                    pendingQuestionId = VALUES(pendingQuestionId),
                    updatedAt = VALUES(updatedAt);
                """;

            using (var command = new MySqlCommand(upsertSessionSql, connection, transaction))
            {
                command.Parameters.Add("@playerCount", MySqlDbType.UByte).Value = session.PlayerCount;
                command.Parameters.Add("@currentTurnIndex", MySqlDbType.UByte).Value = session.CurrentTurnIndex;
                command.Parameters.Add("@lastDice", MySqlDbType.UByte).Value = session.LastDice;
                command.Parameters.Add("@pendingQuestionId", MySqlDbType.Int32).Value =
                    session.PendingQuestionId.HasValue ? session.PendingQuestionId.Value : DBNull.Value;
                command.ExecuteNonQuery();
            }

            session.SaveId = GetSessionId(connection, transaction, session.PlayerCount);
            int sessionId = session.SaveId
                ?? throw new InvalidOperationException("Impossible de retrouver la sauvegarde apres l'enregistrement.");

            const string deletePlayersSql = """
                DELETE FROM GamePlayers
                WHERE gameId = @gameId;
                """;

            using (var command = new MySqlCommand(deletePlayersSql, connection, transaction))
            {
                command.Parameters.Add("@gameId", MySqlDbType.Int32).Value = sessionId;
                command.ExecuteNonQuery();
            }

            const string insertPlayerSql = """
                INSERT INTO GamePlayers (gameId, playerIndex, name, posX, posY, color, isBlocked)
                VALUES (@gameId, @playerIndex, @name, @posX, @posY, @color, @isBlocked);
                """;

            using (var command = new MySqlCommand(insertPlayerSql, connection, transaction))
            {
                command.Parameters.Add("@gameId", MySqlDbType.Int32);
                command.Parameters.Add("@playerIndex", MySqlDbType.UByte);
                command.Parameters.Add("@name", MySqlDbType.Text);
                command.Parameters.Add("@posX", MySqlDbType.UByte);
                command.Parameters.Add("@posY", MySqlDbType.UByte);
                command.Parameters.Add("@color", MySqlDbType.String);
                command.Parameters.Add("@isBlocked", MySqlDbType.Bit);

                foreach (var player in session.Players)
                {
                    command.Parameters["@gameId"].Value = sessionId;
                    command.Parameters["@playerIndex"].Value = player.PlayerIndex;
                    command.Parameters["@name"].Value = player.Name;
                    command.Parameters["@posX"].Value = player.Row;
                    command.Parameters["@posY"].Value = player.Col;
                    command.Parameters["@color"].Value = player.Color;
                    command.Parameters["@isBlocked"].Value = player.IsBlocked;
                    command.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        /// <summary>
        /// Supprime la sauvegarde d'une configuration de joueurs.
        /// Utilise par exemple quand une partie est terminee.
        /// </summary>
        public void DeleteSavedGame(int playerCount)
        {
            ValidatePlayerCount(playerCount);

            using var connection = OpenDatabaseConnection();
            using var transaction = connection.BeginTransaction();

            int? sessionId = GetSessionId(connection, transaction, playerCount, throwIfMissing: false);
            if (!sessionId.HasValue)
            {
                return;
            }

            using (var deletePlayersCommand = new MySqlCommand(
                "DELETE FROM GamePlayers WHERE gameId = @gameId;",
                connection,
                transaction))
            {
                deletePlayersCommand.Parameters.Add("@gameId", MySqlDbType.Int32).Value = sessionId.Value;
                deletePlayersCommand.ExecuteNonQuery();
            }

            using (var deleteSessionCommand = new MySqlCommand(
                "DELETE FROM GameSaves WHERE id = @gameId;",
                connection,
                transaction))
            {
                deleteSessionCommand.Parameters.Add("@gameId", MySqlDbType.Int32).Value = sessionId.Value;
                deleteSessionCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        /// <summary>
        /// Tire un event aleatoire.
        /// Si negativeOnly = true, on ne tire que parmi les events negatifs.
        /// </summary>
        public GameEvent GetRandomEvent(bool negativeOnly = false)
        {
            using var connection = OpenDatabaseConnection();

            string sql = negativeOnly
                ? """
                    SELECT id, title, description, effectType, effectValue, isNegative
                    FROM Events
                    WHERE isActive = TRUE AND isNegative = TRUE
                    ORDER BY RAND()
                    LIMIT 1;
                    """
                : """
                    SELECT id, title, description, effectType, effectValue, isNegative
                    FROM Events
                    WHERE isActive = TRUE
                    ORDER BY RAND()
                    LIMIT 1;
                    """;

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return new GameEvent
                {
                    Title = negativeOnly ? "Malediction" : "Murmure de jungle",
                    Description = negativeOnly
                        ? "La jungle se referme sur toi. Recule de 2 cases."
                        : "Le vent traverse la jungle. Rien ne change cette fois.",
                    EffectType = negativeOnly ? "Move" : "None",
                    EffectValue = negativeOnly ? -2 : 0,
                    IsNegative = negativeOnly
                };
            }

            return new GameEvent
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Description = reader.GetString("description"),
                EffectType = reader.GetString("effectType"),
                EffectValue = reader.GetInt32("effectValue"),
                IsNegative = reader.GetBoolean("isNegative")
            };
        }

        /// <summary>
        /// Tire une question aleatoire active.
        /// </summary>
        public GameQuestion? GetRandomQuestion()
        {
            using var connection = OpenDatabaseConnection();

            const string sql = """
                SELECT id, questionText, answerText, hintText
                FROM Questions
                WHERE isActive = TRUE
                ORDER BY RAND()
                LIMIT 1;
                """;

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new GameQuestion
            {
                Id = reader.GetInt32("id"),
                QuestionText = reader.GetString("questionText"),
                AnswerText = reader.GetString("answerText"),
                HintText = reader.IsDBNull(reader.GetOrdinal("hintText"))
                    ? string.Empty
                    : reader.GetString("hintText")
            };
        }

        /// <summary>
        /// Recharge une question par son identifiant.
        /// Tres utile quand une sauvegarde a ete faite pendant une enigme en attente.
        /// </summary>
        public GameQuestion? GetQuestionById(int questionId)
        {
            using var connection = OpenDatabaseConnection();

            const string sql = """
                SELECT id, questionText, answerText, hintText
                FROM Questions
                WHERE id = @questionId AND isActive = TRUE
                LIMIT 1;
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@questionId", MySqlDbType.Int32).Value = questionId;
            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new GameQuestion
            {
                Id = reader.GetInt32("id"),
                QuestionText = reader.GetString("questionText"),
                AnswerText = reader.GetString("answerText"),
                HintText = reader.IsDBNull(reader.GetOrdinal("hintText"))
                    ? string.Empty
                    : reader.GetString("hintText")
            };
        }

        /// <summary>
        /// Prepare toute la structure MySQL necessaire au jeu.
        /// On cree d'abord la base, puis les tables, puis les donnees minimales.
        /// </summary>
        private void EnsureDatabase()
        {
            using (var serverConnection = OpenServerConnection())
            {
                string createDatabaseSql = $"""
                    CREATE DATABASE IF NOT EXISTS `{databaseName}`
                    CHARACTER SET utf8mb4
                    COLLATE utf8mb4_unicode_ci;
                    """;

                ExecuteNonQuery(serverConnection, createDatabaseSql);
            }

            using var databaseConnection = OpenDatabaseConnection();

            ExecuteNonQuery(databaseConnection, """
                CREATE TABLE IF NOT EXISTS GameSaves
                (
                    id INT NOT NULL AUTO_INCREMENT,
                    playerCount TINYINT UNSIGNED NOT NULL,
                    currentTurnIndex TINYINT UNSIGNED NOT NULL DEFAULT 0,
                    lastDice TINYINT UNSIGNED NOT NULL DEFAULT 0,
                    pendingQuestionId INT NULL,
                    updatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (id),
                    UNIQUE KEY ux_gamesaves_playercount (playerCount)
                );
                """);

            ExecuteNonQuery(databaseConnection, """
                CREATE TABLE IF NOT EXISTS GamePlayers
                (
                    id INT NOT NULL AUTO_INCREMENT,
                    gameId INT NOT NULL,
                    playerIndex TINYINT UNSIGNED NOT NULL,
                    name TINYTEXT NOT NULL,
                    posX TINYINT UNSIGNED NOT NULL DEFAULT 0,
                    posY TINYINT UNSIGNED NOT NULL DEFAULT 0,
                    color CHAR(7) NOT NULL,
                    isBlocked BIT NOT NULL DEFAULT b'0',
                    PRIMARY KEY (id),
                    UNIQUE KEY ux_gameplayers_slot (gameId, playerIndex),
                    KEY ix_gameplayers_gameid (gameId),
                    CONSTRAINT fk_gameplayers_gamesaves
                        FOREIGN KEY (gameId) REFERENCES GameSaves(id)
                        ON DELETE CASCADE
                );
                """);

            ExecuteNonQuery(databaseConnection, """
                CREATE TABLE IF NOT EXISTS Events
                (
                    id INT NOT NULL AUTO_INCREMENT,
                    title TINYTEXT NOT NULL,
                    description TEXT NOT NULL,
                    effectType ENUM('None', 'Move', 'Block') NOT NULL DEFAULT 'None',
                    effectValue SMALLINT NOT NULL DEFAULT 0,
                    isNegative BIT NOT NULL DEFAULT b'0',
                    isActive BIT NOT NULL DEFAULT b'1',
                    PRIMARY KEY (id)
                );
                """);

            ExecuteNonQuery(databaseConnection, """
                CREATE TABLE IF NOT EXISTS Questions
                (
                    id INT NOT NULL AUTO_INCREMENT,
                    questionText TEXT NOT NULL,
                    answerText TINYTEXT NOT NULL,
                    hintText TEXT NULL,
                    isActive BIT NOT NULL DEFAULT b'1',
                    PRIMARY KEY (id)
                );
                """);

            EnsureColumnExists(
                databaseConnection,
                "GameSaves",
                "pendingQuestionId",
                "ALTER TABLE GameSaves ADD COLUMN pendingQuestionId INT NULL AFTER lastDice;");

            MigrateLegacyEnigmesToEvents(databaseConnection);
            SeedEvents(databaseConnection);
            SeedQuestions(databaseConnection);
        }

        /// <summary>
        /// Ouvre une connexion au serveur MySQL sans selectionner de base.
        /// Sert notamment pour la creation du schema si besoin.
        /// </summary>
        private MySqlConnection OpenServerConnection()
        {
            var builder = CreateBaseBuilder();
            var connection = new MySqlConnection(builder.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Ouvre une connexion directement sur la base de jeu.
        /// </summary>
        private MySqlConnection OpenDatabaseConnection()
        {
            var builder = CreateBaseBuilder();
            builder.Database = databaseName;
            var connection = new MySqlConnection(builder.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Construit les parametres techniques communs a toutes les connexions.
        /// </summary>
        private MySqlConnectionStringBuilder CreateBaseBuilder()
        {
            return new MySqlConnectionStringBuilder
            {
                Server = serverName,
                Port = port,
                UserID = userName,
                Password = password,
                Pooling = true,
                MinimumPoolSize = 0,
                MaximumPoolSize = 10,
                SslMode = MySqlSslMode.Disabled,
                AllowPublicKeyRetrieval = true,
                CharacterSet = "utf8mb4",
                AllowUserVariables = true
            };
        }

        /// <summary>
        /// Charge une session complete : metadonnees de la partie + joueurs.
        /// </summary>
        private static GameSession? LoadSavedSession(MySqlConnection connection, int playerCount)
        {
            const string sessionSql = """
                SELECT id, playerCount, currentTurnIndex, lastDice, pendingQuestionId
                FROM GameSaves
                WHERE playerCount = @playerCount
                LIMIT 1;
                """;

            using var sessionCommand = new MySqlCommand(sessionSql, connection);
            sessionCommand.Parameters.Add("@playerCount", MySqlDbType.UByte).Value = playerCount;

            int sessionId;
            int currentTurnIndex;
            int lastDice;
            int? pendingQuestionId;

            using (var reader = sessionCommand.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                sessionId = reader.GetInt32("id");
                currentTurnIndex = reader.GetInt32("currentTurnIndex");
                lastDice = reader.GetInt32("lastDice");
                pendingQuestionId = reader.IsDBNull(reader.GetOrdinal("pendingQuestionId"))
                    ? null
                    : reader.GetInt32("pendingQuestionId");
            }

            const string playersSql = """
                SELECT id, playerIndex, name, posX, posY, color, isBlocked
                FROM GamePlayers
                WHERE gameId = @gameId
                ORDER BY playerIndex;
                """;

            using var playersCommand = new MySqlCommand(playersSql, connection);
            playersCommand.Parameters.Add("@gameId", MySqlDbType.Int32).Value = sessionId;

            var players = new List<PlayerData>();
            using (var reader = playersCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    players.Add(new PlayerData
                    {
                        Id = reader.GetInt32("id"),
                        PlayerIndex = reader.GetInt32("playerIndex"),
                        Name = reader.GetString("name"),
                        Row = reader.GetInt32("posX"),
                        Col = reader.GetInt32("posY"),
                        Color = reader.GetString("color"),
                        IsBlocked = reader.GetBoolean("isBlocked")
                    });
                }
            }

            if (players.Count != playerCount)
            {
                return CreateNewSession(playerCount);
            }

            return new GameSession
            {
                SaveId = sessionId,
                PlayerCount = playerCount,
                CurrentTurnIndex = currentTurnIndex,
                LastDice = lastDice,
                PendingQuestionId = pendingQuestionId,
                Players = players
            };
        }

        /// <summary>
        /// Cree une nouvelle session vierge avec le bon nombre de joueurs.
        /// </summary>
        private static GameSession CreateNewSession(int playerCount)
        {
            var session = new GameSession
            {
                PlayerCount = playerCount,
                CurrentTurnIndex = 0,
                LastDice = 0
            };

            for (int i = 0; i < playerCount; i++)
            {
                session.Players.Add(new PlayerData
                {
                    PlayerIndex = i,
                    Name = $"Joueur {i + 1}",
                    Row = 0,
                    Col = 0,
                    Color = DefaultColors[i],
                    IsBlocked = false
                });
            }

            return session;
        }

        /// <summary>
        /// Execute une commande SQL qui ne renvoie pas de resultat
        /// (CREATE TABLE, INSERT, UPDATE, DELETE, ALTER...).
        /// </summary>
        private static void ExecuteNonQuery(MySqlConnection connection, string sql)
        {
            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retourne l'identifiant de la sauvegarde correspondant a un nombre de joueurs.
        /// </summary>
        private static int? GetSessionId(
            MySqlConnection connection,
            MySqlTransaction transaction,
            int playerCount,
            bool throwIfMissing = true)
        {
            const string sql = """
                SELECT id
                FROM GameSaves
                WHERE playerCount = @playerCount
                LIMIT 1;
                """;

            using var command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.Add("@playerCount", MySqlDbType.UByte).Value = playerCount;
            object? result = command.ExecuteScalar();

            if (result is null)
            {
                if (throwIfMissing)
                {
                    throw new InvalidOperationException("Impossible de retrouver la sauvegarde.");
                }

                return null;
            }

            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Compte le nombre de lignes d'une table.
        /// Sert surtout pour savoir si les donnees de base ont deja ete inserees.
        /// </summary>
        private static long CountRows(MySqlConnection connection, string tableName)
        {
            using var command = new MySqlCommand($"SELECT COUNT(*) FROM {tableName};", connection);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        /// <summary>
        /// Verifie l'existence d'une table dans le schema courant.
        /// </summary>
        private static bool TableExists(MySqlConnection connection, string tableName)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                  AND table_name = @tableName;
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@tableName", MySqlDbType.String).Value = tableName;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Verifie l'existence d'une colonne.
        /// Utile pour les petites migrations sans casser les anciennes bases.
        /// </summary>
        private static bool ColumnExists(MySqlConnection connection, string tableName, string columnName)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = @tableName
                  AND column_name = @columnName;
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@tableName", MySqlDbType.String).Value = tableName;
            command.Parameters.Add("@columnName", MySqlDbType.String).Value = columnName;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Ajoute une colonne si elle n'existe pas deja.
        /// </summary>
        private static void EnsureColumnExists(
            MySqlConnection connection,
            string tableName,
            string columnName,
            string alterSql)
        {
            if (!ColumnExists(connection, tableName, columnName))
            {
                ExecuteNonQuery(connection, alterSql);
            }
        }

        /// <summary>
        /// Migre d'anciennes donnees de la table Enigmes vers la table Events.
        /// Cela permet de ne pas perdre les anciennes bases du projet.
        /// </summary>
        private static void MigrateLegacyEnigmesToEvents(MySqlConnection connection)
        {
            if (!TableExists(connection, "Enigmes") || CountRows(connection, "Events") > 0)
            {
                return;
            }

            const string sql = """
                INSERT INTO Events (title, description, effectType, effectValue, isNegative, isActive)
                SELECT
                    'Event de jungle',
                    texte,
                    'None',
                    0,
                    b'0',
                    isActive
                FROM Enigmes;
                """;

            ExecuteNonQuery(connection, sql);
        }

        /// <summary>
        /// Insere quelques events de base si la table est vide.
        /// </summary>
        private static void SeedEvents(MySqlConnection connection)
        {
            if (CountRows(connection, "Events") > 0)
            {
                return;
            }

            (string Title, string Description, string EffectType, int EffectValue, bool IsNegative)[] events =
            {
                ("Tambours anciens", "Des tambours retentissent. La jungle te laisse respirer un instant.", "None", 0, false),
                ("Lianes protectrices", "Une liane t'aide a gagner du terrain. Avance de 2 cases.", "Move", 2, false),
                ("Source cachee", "Tu trouves un passage humide sous les fougeres. Avance de 1 case.", "Move", 1, false),
                ("Branche sournoise", "Une branche te fouette au passage. Recule de 2 cases.", "Move", -2, true),
                ("Sables mouvants", "Le sol se derobe sous tes pieds. Ton prochain tour sera bloque.", "Block", 1, true),
                ("Rugissement lointain", "Le cri d'une bete immense glace la table. Rien ne change, mais la pression monte.", "None", 0, false),
                ("Essaim furieux", "Un essaim te force a fuir. Recule de 3 cases.", "Move", -3, true),
                ("Masque sacre", "Un ancien masque te benit. Avance de 3 cases.", "Move", 3, false)
            };

            const string sql = """
                INSERT INTO Events (title, description, effectType, effectValue, isNegative, isActive)
                VALUES (@title, @description, @effectType, @effectValue, @isNegative, b'1');
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@title", MySqlDbType.String);
            command.Parameters.Add("@description", MySqlDbType.Text);
            command.Parameters.Add("@effectType", MySqlDbType.String);
            command.Parameters.Add("@effectValue", MySqlDbType.Int16);
            command.Parameters.Add("@isNegative", MySqlDbType.Bit);

            foreach (var gameEvent in events)
            {
                command.Parameters["@title"].Value = gameEvent.Title;
                command.Parameters["@description"].Value = gameEvent.Description;
                command.Parameters["@effectType"].Value = gameEvent.EffectType;
                command.Parameters["@effectValue"].Value = gameEvent.EffectValue;
                command.Parameters["@isNegative"].Value = gameEvent.IsNegative;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insere quelques questions de base si la table est vide.
        /// </summary>
        private static void SeedQuestions(MySqlConnection connection)
        {
            if (CountRows(connection, "Questions") > 0)
            {
                return;
            }

            (string QuestionText, string AnswerText, string HintText)[] questions =
            {
                ("Je parle sans bouche et j'entends sans oreilles. Qui suis-je ?", "echo", "On l'entend revenir."),
                ("Plus j'ai de gardiens, moins on me voit. Qui suis-je ?", "secret", "On le protege."),
                ("Quel mot devient plus court quand on lui ajoute deux lettres ?", "court", "C'est un jeu de langue."),
                ("Je monte mais je ne descends jamais. Qui suis-je ?", "age|l'age|âge", "Tout le monde l'a."),
                ("Quel animal marche a quatre pattes le matin, deux le midi et trois le soir ?", "homme|humain", "C'est l'enigme la plus celebre."),
                ("Je peux remplir une piece sans prendre de place. Qui suis-je ?", "lumiere|lumière", "Elle chasse l'ombre.")
            };

            const string sql = """
                INSERT INTO Questions (questionText, answerText, hintText, isActive)
                VALUES (@questionText, @answerText, @hintText, b'1');
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@questionText", MySqlDbType.Text);
            command.Parameters.Add("@answerText", MySqlDbType.String);
            command.Parameters.Add("@hintText", MySqlDbType.Text);

            foreach (var question in questions)
            {
                command.Parameters["@questionText"].Value = question.QuestionText;
                command.Parameters["@answerText"].Value = question.AnswerText;
                command.Parameters["@hintText"].Value = question.HintText;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Interdit les configurations non prevues par le jeu.
        /// </summary>
        private static void ValidatePlayerCount(int playerCount)
        {
            if (playerCount is < 2 or > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Le jeu accepte de 2 a 4 joueurs.");
            }
        }
    }
}
