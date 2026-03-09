using System;
using System.Collections.Generic;
using System.Text;

namespace test_jumanji_aj_fonction.Data
{

    public class JoueurRepository
    {
        Database db = new Database();

        public void SauvegarderJoueur(Joueur joueur)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    INSERT INTO joueurs (nom, row_position, col_position)
                    VALUES (@nom, @row, @col)
                    ON DUPLICATE KEY UPDATE row_position=@row, col_position=@col;";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", joueur.Nom);
                cmd.Parameters.AddWithValue("@row", joueur.Row);
                cmd.Parameters.AddWithValue("@col", joueur.Col);

                cmd.ExecuteNonQuery();
            }
        }

        public Joueur ChargerJoueur(string nom)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM joueurs WHERE nom=@nom";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", nom);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Joueur
                    {
                        Nom = reader.GetString("nom"),
                        Row = reader.GetInt32("row_position"),
                        Col = reader.GetInt32("col_position")
                    };
                }
            }
            return null; // joueur non trouvé
        }
    }
}
