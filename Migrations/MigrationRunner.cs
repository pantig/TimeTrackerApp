using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

namespace TimeTrackerApp.Migrations
{
    public static class MigrationRunner
    {
        public static void RunMigrations(string connectionString)
        {
            var migrationsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
            
            if (!Directory.Exists(migrationsDirectory))
            {
                Console.WriteLine($"Migrations directory not found: {migrationsDirectory}");
                return;
            }

            var sqlFiles = Directory.GetFiles(migrationsDirectory, "*.sql")
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            if (!sqlFiles.Any())
            {
                Console.WriteLine("No migration files found.");
                return;
            }

            Console.WriteLine($"Found {sqlFiles.Count} migration file(s).");

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Utworzenie tabeli do śledzenia migracji
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS __MigrationHistory (
                        MigrationId TEXT PRIMARY KEY,
                        AppliedAt TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            foreach (var sqlFile in sqlFiles)
            {
                var migrationId = Path.GetFileNameWithoutExtension(sqlFile);

                // Sprawdź czy migracja już została zastosowana
                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(*) FROM __MigrationHistory WHERE MigrationId = @id";
                    checkCmd.Parameters.AddWithValue("@id", migrationId);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        Console.WriteLine($"Migration {migrationId} already applied. Skipping.");
                        continue;
                    }
                }

                Console.WriteLine($"Applying migration: {migrationId}");

                try
                {
                    var sql = File.ReadAllText(sqlFile);
                    
                    // Podziel na pojedyncze polecenia (rozdzielone przez puste linie)
                    var commands = sql.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(cmd => !string.IsNullOrWhiteSpace(cmd) && !cmd.Trim().StartsWith("--"))
                        .ToList();

                    using var transaction = connection.BeginTransaction();
                    
                    foreach (var commandText in commands)
                    {
                        if (string.IsNullOrWhiteSpace(commandText)) continue;
                        
                        using var cmd = connection.CreateCommand();
                        cmd.Transaction = transaction;
                        cmd.CommandText = commandText.Trim();
                        cmd.ExecuteNonQuery();
                    }

                    // Zapisz informację o zastosowanej migracji
                    using (var recordCmd = connection.CreateCommand())
                    {
                        recordCmd.Transaction = transaction;
                        recordCmd.CommandText = "INSERT INTO __MigrationHistory (MigrationId, AppliedAt) VALUES (@id, @date)";
                        recordCmd.Parameters.AddWithValue("@id", migrationId);
                        recordCmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
                        recordCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.WriteLine($"Migration {migrationId} applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying migration {migrationId}: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine("All migrations completed.");
        }
    }
}
