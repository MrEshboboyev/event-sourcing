using EventSourcing;

Console.WriteLine("Event Sourcing : Hello world!");

// Create a file-based event store and snapshot store
var eventStore = new FileEventStore("bank_events.json");
var snapshotStore = new FileSnapshotStore("snapshots");

// Create and run the console UI
var consoleUI = new ConsoleUI(eventStore, snapshotStore);
await consoleUI.RunAsync();

// Load all AccountOpened events
var accountOpenedEvents = await eventStore.GetEventsByTypeAsync<AccountOpened>();
Console.WriteLine($"\nTotal accounts opened: {accountOpenedEvents.Count()}");

// Base event type
public abstract record Event(Guid StreamId)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int Version { get; init; }
}

// Specific events for bank account domain
public record AccountOpened : Event
{
    public Guid AccountId { get; init; }
    public string AccountHolder { get; init; }
    public decimal InitialDeposit { get; init; }
    public string Currency { get; init; }

    public AccountOpened(Guid accountId, string accountHolder, decimal initialDeposit, string currency, int version) 
        : base(accountId)
    {
        AccountId = accountId;
        AccountHolder = accountHolder;
        InitialDeposit = initialDeposit;
        Currency = currency;
        Version = version;
    }
}

public record MoneyDeposited : Event
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }

    public MoneyDeposited(Guid accountId, decimal amount, string description, int version) 
        : base(accountId)
    {
        AccountId = accountId;
        Amount = amount;
        Description = description;
        Version = version;
    }
}

public record MoneyWithdrawn : Event
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }

    public MoneyWithdrawn(Guid accountId, decimal amount, string description, int version) 
        : base(accountId)
    {
        AccountId = accountId;
        Amount = amount;
        Description = description;
        Version = version;
    }
}

public record MoneyTransferred : Event
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public Guid ToAccountId { get; init; }
    public string Description { get; init; }

    public MoneyTransferred(Guid accountId, decimal amount, Guid toAccountId, string description, int version) 
        : base(accountId)
    {
        AccountId = accountId;
        Amount = amount;
        ToAccountId = toAccountId;
        Description = description;
        Version = version;
    }
}

public record AccountClosed : Event
{
    public Guid AccountId { get; init; }
    public string Reason { get; init; }

    public AccountClosed(Guid accountId, string reason, int version) 
        : base(accountId)
    {
        AccountId = accountId;
        Reason = reason;
        Version = version;
    }
}
