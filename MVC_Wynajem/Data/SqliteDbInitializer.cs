// Data/SqliteDbInitializer.cs
using System.IO;
using Microsoft.Data.Sqlite;

namespace Reservo.Data
{
    public static class SqliteDbInitializer
    {
        public static void Initialize(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Login (
                  Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                  User     TEXT    NOT NULL UNIQUE,
                  Password TEXT    NOT NULL,
                  IsAdmin  INTEGER NOT NULL
                );";
                cmd.ExecuteNonQuery();
            }

            using (var count = conn.CreateCommand())
            {
                count.CommandText = "SELECT COUNT(*) FROM Login;";
                var c = (long)count.ExecuteScalar()!;
                if (c == 0)
                {
                    using var ins = conn.CreateCommand();
                    ins.CommandText = @"
                    INSERT INTO Login (User, Password, IsAdmin)
                    VALUES ($u, $p, $a);";
                    ins.Parameters.AddWithValue("$u", "admin");
                    ins.Parameters.AddWithValue("$a", 1);
                    ins.ExecuteNonQuery();
                }
            }
        }
    }
}
