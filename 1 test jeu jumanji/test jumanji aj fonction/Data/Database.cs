using System;
using System.Collections.Generic;
using System.Text;

namespace test_jumanji_aj_fonction.Data
{
    public class Database
    {
        private string connectionString = "Server=localhost;Database=jumanji;Uid=root;Pwd=motdepasse;";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
