using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;


namespace test_martye
{
    public sealed class DatabaseManager
    {
        private const string DefaultEnigme = "Le silence regne dans la jungle...";
        private static readonly string[] DefaultColors = { "#C62828", "#1565C0", "#F9A825", "#2E7D32" };

        private readonly string connectionString =
            "server=10.10.51.98;database=amaury;port=3306;User Id=Amaury;password=root;Pooling=true;MinimumPoolSize=0;MaximumPoolSize=10;";// amaury

        public DatabaseManager()
        {
            EnsureDatabase();
        }

        public bool HasSavedGame(int playerCount)
        {
            ValidatePlayerCount(playerCount);

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

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

        public GameSession LoadOrCreateSession(int playerCount)
        {
            ValidatePlayerCount(playerCount);

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var savedSession = LoadSavedSession(connection, playerCount);
            return savedSession ?? CreateNewSession(playerCount);
        }

        public void SaveGame(GameSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            ValidatePlayerCount(session.PlayerCount);

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            const string upsertSessionSql = """
                INSERT INTO GameSaves (playerCount, currentTurnIndex, lastDice, updatedAt)
                VALUES (@playerCount, @currentTurnIndex, @lastDice, UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE
                    currentTurnIndex = VALUES(currentTurnIndex),
                    lastDice = VALUES(lastDice),
                    updatedAt = VALUES(updatedAt);
                """;

            using (var command = new MySqlCommand(upsertSessionSql, connection, transaction))
            {
                command.Parameters.Add("@playerCount", MySqlDbType.UByte).Value = session.PlayerCount;
                command.Parameters.Add("@currentTurnIndex", MySqlDbType.UByte).Value = session.CurrentTurnIndex;
                command.Parameters.Add("@lastDice", MySqlDbType.UByte).Value = session.LastDice;
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

        public void DeleteSavedGame(int playerCount)
        {
            ValidatePlayerCount(playerCount);

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

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

        public string GetRandomEnigme()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            const string sql = """
                SELECT texte
                FROM Enigmes
                WHERE isActive = TRUE
                ORDER BY RAND()
                LIMIT 1;
                """;

            using var command = new MySqlCommand(sql, connection);
            var result = command.ExecuteScalar();

            return result as string ?? DefaultEnigme;
        }

        private void EnsureDatabase()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS GameSaves
                (
                    id INT NOT NULL AUTO_INCREMENT,
                    playerCount TINYINT UNSIGNED NOT NULL,
                    currentTurnIndex TINYINT UNSIGNED NOT NULL DEFAULT 0,
                    lastDice TINYINT UNSIGNED NOT NULL DEFAULT 0,
                    updatedAt DATETIME NOT NULL,
                    PRIMARY KEY (id),
                    UNIQUE KEY ux_gamesaves_playercount (playerCount)
                );
                """);

            ExecuteNonQuery(connection, """
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

            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS Enigmes
                (
                    id INT NOT NULL AUTO_INCREMENT,
                    texte TEXT NOT NULL,
                    isActive BIT NOT NULL DEFAULT b'1',
                    PRIMARY KEY (id)
                );
                """);

            SeedEnigmes(connection);
        }

        private static GameSession? LoadSavedSession(MySqlConnection connection, int playerCount)
        {
            const string sessionSql = """
                SELECT id, playerCount, currentTurnIndex, lastDice
                FROM GameSaves
                WHERE playerCount = @playerCount
                LIMIT 1;
                """;

            using var sessionCommand = new MySqlCommand(sessionSql, connection);
            sessionCommand.Parameters.Add("@playerCount", MySqlDbType.UByte).Value = playerCount;

            int sessionId;
            int currentTurnIndex;
            int lastDice;

            using (var reader = sessionCommand.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                sessionId = reader.GetInt32("id");
                currentTurnIndex = reader.GetInt32("currentTurnIndex");
                lastDice = reader.GetInt32("lastDice");
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
                Players = players
            };
        }

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

        private static void ExecuteNonQuery(MySqlConnection connection, string sql)
        {
            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

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

        private static long CountRows(MySqlConnection connection, string tableName)
        {
            using var command = new MySqlCommand($"SELECT COUNT(*) FROM {tableName};", connection);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static void SeedEnigmes(MySqlConnection connection)
        {
            if (CountRows(connection, "Enigmes") > 0)
            {
                return;
            }

            string[] enigmes =
            {
                "Un rugissement fend la jungle. Releve un defi pour continuer.",
                "La jungle te met a l'epreuve. Reponds a la question du maitre du jeu.",
                "Une brume etrange t'entoure. Si tu echoues, recule de deux cases.",
                "Des tambours resonnent. Toute la table retient son souffle.",
                "Une liane te coupe la route. Garde ton calme et avance.",
                "La jungle te teste. Une bonne reponse peut sauver ton equipe."
            };

            const string sql = """
                INSERT INTO Enigmes (texte, isActive)
                VALUES (@texte, b'1');
                """;

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.Add("@texte", MySqlDbType.Text);

            foreach (var enigme in enigmes)
            {
                command.Parameters["@texte"].Value = enigme;
                command.ExecuteNonQuery();
            }
        }

        private static void ValidatePlayerCount(int playerCount)
        {
            if (playerCount is < 2 or > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Le jeu accepte de 2 a 4 joueurs.");
            }
        }
    }
}
