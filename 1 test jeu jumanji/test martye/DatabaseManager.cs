using System;
using System.Collections.Generic;
using System.Text;

namespace test_martye
{
    internal class DatabaseManager
    {

        public class DatabaseManager
        {
            private string connString = "server=localhost;database=jumanji_db;user=root;password=;";

            public List<PlayerData> LoadPlayers()
            {
                List<PlayerData> list = new List<PlayerData>();
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM Players", conn);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new PlayerData
                            {
                                Id = rdr.GetInt32("id"),
                                Name = rdr.GetString("name"),
                                Row = rdr.GetInt32("posX"),
                                Col = rdr.GetInt32("posY"),
                                Color = rdr.GetString("color")
                            });
                        }
                    }
                }
                return list;
            }

            public void SavePlayers(List<PlayerData> players)
            {
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    foreach (var p in players)
                    {
                        var cmd = new MySqlCommand("UPDATE Players SET posX=@r, posY=@c WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@r", p.Row);
                        cmd.Parameters.AddWithValue("@c", p.Col);
                        cmd.Parameters.AddWithValue("@id", p.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            public string GetRandomEnigme()
            {
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT texte FROM Enigmes ORDER BY RAND() LIMIT 1", conn);
                    var result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : "Le silence règne dans la jungle...";
                }
            }
        }
    }
}
