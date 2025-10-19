using System.Text.Json;

namespace EventSourcing;

public interface ISnapshotStore
{
    Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot) where T : class;
    Task<T?> GetSnapshotAsync<T>(Guid aggregateId) where T : class;
}

public class FileSnapshotStore : ISnapshotStore
{
    private readonly string _snapshotDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileSnapshotStore(string snapshotDirectory = "snapshots")
    {
        _snapshotDirectory = snapshotDirectory;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Create directory if it doesn't exist
        if (!Directory.Exists(_snapshotDirectory))
        {
            Directory.CreateDirectory(_snapshotDirectory);
        }
    }

    public async Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot) where T : class
    {
        Logger.Info($"Saving snapshot for aggregate {aggregateId}");
        
        try
        {
            var filePath = Path.Combine(_snapshotDirectory, $"{aggregateId}.json");
            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Logger.Info($"Snapshot saved successfully for aggregate {aggregateId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save snapshot for aggregate {aggregateId}: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> GetSnapshotAsync<T>(Guid aggregateId) where T : class
    {
        Logger.Info($"Retrieving snapshot for aggregate {aggregateId}");
        
        try
        {
            var filePath = Path.Combine(_snapshotDirectory, $"{aggregateId}.json");
            
            if (!File.Exists(filePath))
            {
                Logger.Info($"No snapshot found for aggregate {aggregateId}");
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            Logger.Info($"Snapshot retrieved successfully for aggregate {aggregateId}");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to retrieve snapshot for aggregate {aggregateId}: {ex.Message}");
            return null;
        }
    }
}

public class BankAccountSnapshot
{
    public Guid Id { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime LastSnapshotAt { get; set; }

    public BankAccountSnapshot() { }

    public BankAccountSnapshot(BankAccount account)
    {
        Id = account.Id;
        AccountHolder = account.AccountHolder;
        Balance = account.Balance;
        Currency = account.Currency;
        IsActive = account.IsActive;
        Version = account.Version;
        LastSnapshotAt = DateTime.UtcNow;
    }
}
