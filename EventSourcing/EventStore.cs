using System.Text.Json;

namespace EventSourcing;

public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events);
    Task<IEnumerable<Event>> GetEventsAsync(Guid aggregateId);
    Task<IEnumerable<Event>> GetEventsByTypeAsync<T>() where T : Event;
}

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<Event>> _eventStore = [];
    private readonly List<Event> _allEvents = [];

    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events)
    {
        Logger.Info($"Saving {events.Count()} events to in-memory store for aggregate {aggregateId}");
        
        if (!_eventStore.TryGetValue(aggregateId, out List<Event>? value))
        {
            value = [];
            _eventStore[aggregateId] = value;
        }

        foreach (var @event in events)
        {
            value.Add(@event);
            _allEvents.Add(@event);
        }

        Logger.Info($"Events saved successfully to in-memory store for aggregate {aggregateId}");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Event>> GetEventsAsync(Guid aggregateId)
    {
        if (_eventStore.TryGetValue(aggregateId, out List<Event>? value))
        {
            Logger.Info($"Retrieved {value.Count} events from in-memory store for aggregate {aggregateId}");
            return Task.FromResult(value.AsEnumerable());
        }

        Logger.Info($"No events found in in-memory store for aggregate {aggregateId}");
        return Task.FromResult(Enumerable.Empty<Event>());
    }

    public Task<IEnumerable<Event>> GetEventsByTypeAsync<T>() where T : Event
    {
        var eventType = typeof(T);
        var events = _allEvents.Where(e => e.GetType() == eventType).OfType<Event>();
        Logger.Info($"Retrieved {events.Count()} events of type {eventType.Name} from in-memory store");
        return Task.FromResult(events);
    }
}

public class FileEventStore : IEventStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileEventStore(string filePath = "events.json")
    {
        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new EventJsonConverter() }
        };
    }

    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events)
    {
        Logger.Info($"Saving {events.Count()} events to file store for aggregate {aggregateId}");
        
        try
        {
            var allEvents = await LoadAllEventsAsync();
            
            foreach (var @event in events)
            {
                allEvents.Add(new StoredEvent(aggregateId, @event));
            }
            
            await SaveAllEventsAsync(allEvents);
            Logger.Info($"Events saved successfully to file store for aggregate {aggregateId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save events to file store for aggregate {aggregateId}: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<Event>> GetEventsAsync(Guid aggregateId)
    {
        Logger.Info($"Retrieving events from file store for aggregate {aggregateId}");
        
        try
        {
            var allEvents = await LoadAllEventsAsync();
            var result = allEvents.Where(e => e.AggregateId == aggregateId && e.Event != null).Select(e => e.Event!);
            Logger.Info($"Retrieved {result.Count()} events from file store for aggregate {aggregateId}");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to retrieve events from file store for aggregate {aggregateId}: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<Event>> GetEventsByTypeAsync<T>() where T : Event
    {
        Logger.Info($"Retrieving events of type {typeof(T).Name} from file store");
        
        try
        {
            var allEvents = await LoadAllEventsAsync();
            var eventType = typeof(T).Name;
            var result = allEvents.Where(e => e.Event?.GetType().Name == eventType).Select(e => e.Event!);
            Logger.Info($"Retrieved {result.Count()} events of type {eventType} from file store");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to retrieve events of type {typeof(T).Name} from file store: {ex.Message}");
            throw;
        }
    }

    private async Task<List<StoredEvent>> LoadAllEventsAsync()
    {
        if (!File.Exists(_filePath))
        {
            Logger.Debug($"Event file {_filePath} does not exist, returning empty list");
            return [];
        }

        var json = await File.ReadAllTextAsync(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            Logger.Debug($"Event file {_filePath} is empty, returning empty list");
            return [];
        }

        try
        {
            var events = JsonSerializer.Deserialize<List<StoredEvent>>(json, _jsonOptions);
            Logger.Debug($"Loaded {events?.Count ?? 0} events from file {_filePath}");
            return events ?? [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to deserialize events from file {_filePath}: {ex.Message}");
            return [];
        }
    }

    private async Task SaveAllEventsAsync(List<StoredEvent> events)
    {
        try
        {
            var json = JsonSerializer.Serialize(events, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
            Logger.Debug($"Saved {events.Count} events to file {_filePath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save events to file {_filePath}: {ex.Message}");
            throw;
        }
    }
}

public class StoredEvent
{
    public Guid AggregateId { get; set; }
    public Event? Event { get; set; }
    public DateTime StoredAt { get; set; }

    public StoredEvent(Guid aggregateId, Event @event)
    {
        AggregateId = aggregateId;
        Event = @event;
        StoredAt = DateTime.UtcNow;
    }

    // Required for JSON deserialization
    public StoredEvent() { }
}

public class EventJsonConverter : System.Text.Json.Serialization.JsonConverter<Event>
{
    public override Event? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        
        var eventType = root.GetProperty("EventType").GetString();
        var eventVersion = root.TryGetProperty("EventVersion", out var versionElement) ? versionElement.GetInt32() : 1;
        var eventData = root.GetProperty("EventData").GetRawText();
        
        Logger.Debug($"Deserializing event of type {eventType}, version {eventVersion}");
        
        // Handle different versions of events
        return eventType switch
        {
            "AccountOpened" => DeserializeAccountOpened(eventData, eventVersion, options),
            "MoneyDeposited" => DeserializeMoneyDeposited(eventData, eventVersion, options),
            "MoneyWithdrawn" => DeserializeMoneyWithdrawn(eventData, eventVersion, options),
            "MoneyTransferred" => DeserializeMoneyTransferred(eventData, eventVersion, options),
            "AccountClosed" => DeserializeAccountClosed(eventData, eventVersion, options),
            _ => throw new JsonException($"Unknown event type: {eventType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Event value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("EventType", value.GetType().Name);
        writer.WriteNumber("EventVersion", GetEventVersion(value));
        
        writer.WritePropertyName("EventData");
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        
        writer.WriteEndObject();
    }

    private int GetEventVersion(Event @event)
    {
        // In a real implementation, you might have version attributes or other ways to determine version
        // For now, we'll just return 1 for all events
        return 1;
    }

    private Event? DeserializeAccountOpened(string eventData, int version, JsonSerializerOptions options)
    {
        // Handle different versions of AccountOpened
        return version switch
        {
            1 => JsonSerializer.Deserialize<AccountOpened>(eventData, options),
            _ => JsonSerializer.Deserialize<AccountOpened>(eventData, options) // Fallback to latest version
        };
    }

    private Event? DeserializeMoneyDeposited(string eventData, int version, JsonSerializerOptions options)
    {
        // Handle different versions of MoneyDeposited
        return version switch
        {
            1 => JsonSerializer.Deserialize<MoneyDeposited>(eventData, options),
            _ => JsonSerializer.Deserialize<MoneyDeposited>(eventData, options) // Fallback to latest version
        };
    }

    private Event? DeserializeMoneyWithdrawn(string eventData, int version, JsonSerializerOptions options)
    {
        // Handle different versions of MoneyWithdrawn
        return version switch
        {
            1 => JsonSerializer.Deserialize<MoneyWithdrawn>(eventData, options),
            _ => JsonSerializer.Deserialize<MoneyWithdrawn>(eventData, options) // Fallback to latest version
        };
    }

    private Event? DeserializeMoneyTransferred(string eventData, int version, JsonSerializerOptions options)
    {
        // Handle different versions of MoneyTransferred
        return version switch
        {
            1 => JsonSerializer.Deserialize<MoneyTransferred>(eventData, options),
            _ => JsonSerializer.Deserialize<MoneyTransferred>(eventData, options) // Fallback to latest version
        };
    }

    private Event? DeserializeAccountClosed(string eventData, int version, JsonSerializerOptions options)
    {
        // Handle different versions of AccountClosed
        return version switch
        {
            1 => JsonSerializer.Deserialize<AccountClosed>(eventData, options),
            _ => JsonSerializer.Deserialize<AccountClosed>(eventData, options) // Fallback to latest version
        };
    }
}
