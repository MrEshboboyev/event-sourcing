namespace EventSourcing;

// BankAccount class with event handling
public class BankAccount
{
    public Guid Id { get; private set; }
    public string AccountHolder { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; }
    public int Version { get; private set; } = -1;

    public List<Event> Events = [];
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;
    private const int SNAPSHOT_THRESHOLD = 10; // Create snapshot every 10 events

    private BankAccount(IEventStore eventStore, ISnapshotStore snapshotStore)
    {
        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
    }

    // Open a new bank account
    public static BankAccount Open(
        string accountHolder,
        decimal initialDeposit,
        string currency = "USD",
        IEventStore? eventStore = null,
        ISnapshotStore? snapshotStore = null)
    {
        Logger.Info($"Opening new bank account for {accountHolder} with initial deposit {initialDeposit} {currency}");
        
        if (string.IsNullOrWhiteSpace(accountHolder))
        {
            var errorMessage = "Account holder name is required";
            Logger.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        if (initialDeposit < 0)
        {
            var errorMessage = "The initial deposit can't be negative";
            Logger.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        var bankAccount = new BankAccount(
            eventStore ?? new InMemoryEventStore(),
            snapshotStore ?? new FileSnapshotStore());
        var @event = new AccountOpened(Guid.NewGuid(), accountHolder, initialDeposit, currency, 0);

        bankAccount.Apply(@event);
        Logger.Info($"Account {bankAccount.Id} opened successfully");

        return bankAccount;
    }

    // Deposit money into the account
    public void Deposit(decimal amount, string description)
    {
        Logger.Info($"Attempting to deposit {amount} to account {Id} with description: {description}");
        
        EnsureAccountIsActive();
        if (amount <= 0)
        {
            var errorMessage = "The deposit amount must be positive";
            Logger.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        Apply(new MoneyDeposited(Id, amount, description, Version + 1));
        Logger.Info($"Successfully deposited {amount} to account {Id}. New balance: {Balance}");
    }

    // Withdraw money from the account
    public void Withdraw(decimal amount, string description)
    {
        Logger.Info($"Attempting to withdraw {amount} from account {Id} with description: {description}");
        
        EnsureAccountIsActive();
        if (amount <= 0)
        {
            var errorMessage = "The withdrawal amount must be positive";
            Logger.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        if (amount > Balance)
        {
            var errorMessage = $"Insufficient funds. Current balance: {Balance}, requested: {amount}";
            Logger.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        Apply(new MoneyWithdrawn(Id, amount, description, Version + 1));
        Logger.Info($"Successfully withdrew {amount} from account {Id}. New balance: {Balance}");
    }

    // Transfer money to another account
    public void TransferTo(Guid toAccountId, decimal amount, string description)
    {
        Logger.Info($"Attempting to transfer {amount} from account {Id} to account {toAccountId} with description: {description}");
        
        EnsureAccountIsActive();
        if (amount <= 0)
        {
            var errorMessage = "The transfer amount must be positive";
            Logger.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        if (amount > Balance)
        {
            var errorMessage = $"Insufficient funds. Current balance: {Balance}, requested: {amount}";
            Logger.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        Apply(new MoneyTransferred(Id, amount, toAccountId, description, Version + 1));
        Logger.Info($"Successfully transferred {amount} from account {Id} to account {toAccountId}. New balance: {Balance}");
    }

    public void Close(string reason)
    {
        Logger.Info($"Attempting to close account {Id} with reason: {reason}");
        
        EnsureAccountIsActive();
        if (Balance != 0)
        {
            var errorMessage = $"Cannot close account with non-zero balance. Current balance: {Balance}";
            Logger.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        Apply(new AccountClosed(Id, reason, Version + 1));
        Logger.Info($"Successfully closed account {Id}");
    }
    
    private void Apply(Event @event)
    {
        Logger.Debug($"Applying event {@event.GetType().Name} (Version: {@event.Version}) to account {Id}");
        
        // Apply the event to update the account state
        switch (@event)
        {
            case AccountOpened e:
                Id = e.AccountId;
                AccountHolder = e.AccountHolder;
                Balance = e.InitialDeposit;
                Currency = e.Currency;
                IsActive = true;
                Version = e.Version;
                Logger.Debug($"Account opened: {Id}, Holder: {AccountHolder}, Initial deposit: {Balance}");
                break;
            
            case MoneyDeposited e:
                Balance += e.Amount;
                Version = e.Version;
                Logger.Debug($"Money deposited: {e.Amount}, New balance: {Balance}");
                break;
            
            case MoneyWithdrawn e:
                Balance -= e.Amount;
                Version = e.Version;
                Logger.Debug($"Money withdrawn: {e.Amount}, New balance: {Balance}");
                break;
            
            case MoneyTransferred e:
                Balance -= e.Amount;
                Version = e.Version;
                Logger.Debug($"Money transferred: {e.Amount}, New balance: {Balance}");
                break;
            
            case AccountClosed e:
                IsActive = false;
                Version = e.Version;
                Logger.Debug($"Account closed");
                break;
        }
        
        Events.Add(@event);
    }
    
    public async Task SaveAsync()
    {
        if (_eventStore != null && Id != Guid.Empty)
        {
            Logger.Info($"Saving events for account {Id}");
            await _eventStore.SaveEventsAsync(Id, Events);
            
            // Create snapshot if threshold is reached
            if (Events.Count >= SNAPSHOT_THRESHOLD)
            {
                Logger.Info($"Creating snapshot for account {Id} (events count: {Events.Count})");
                var snapshot = new BankAccountSnapshot(this);
                await _snapshotStore.SaveSnapshotAsync(Id, snapshot);
            }
        }
    }
    
    public static async Task<BankAccount> LoadAsync(Guid accountId, IEventStore eventStore, ISnapshotStore snapshotStore)
    {
        Logger.Info($"Loading account {accountId}");
        
        try
        {
            // Try to load snapshot first
            var snapshot = await snapshotStore.GetSnapshotAsync<BankAccountSnapshot>(accountId);
            
            if (snapshot != null)
            {
                Logger.Info($"Found snapshot for account {accountId}, version {snapshot.Version}");

                // Create account from snapshot
                var bankAccount = new BankAccount(eventStore, snapshotStore)
                {
                    Id = snapshot.Id,
                    AccountHolder = snapshot.AccountHolder,
                    Balance = snapshot.Balance,
                    Currency = snapshot.Currency,
                    IsActive = snapshot.IsActive,
                    Version = snapshot.Version
                };

                // Load events that occurred after the snapshot
                var events = await eventStore.GetEventsAsync(accountId);
                var eventsAfterSnapshot = events.Where(e => e.Version > snapshot.Version).OrderBy(e => e.Version);
                
                Logger.Info($"Applying {eventsAfterSnapshot.Count()} events after snapshot");
                foreach (var @event in eventsAfterSnapshot)
                {
                    bankAccount.Apply(@event);
                }
                
                Logger.Info($"Account {accountId} loaded successfully from snapshot");
                return bankAccount;
            }
            else
            {
                Logger.Info($"No snapshot found for account {accountId}, loading from events only");
                
                // Load from events only
                var events = await eventStore.GetEventsAsync(accountId);
                var bankAccount = new BankAccount(eventStore, snapshotStore);
                
                Logger.Info($"Applying {events.Count()} events to reconstruct account state");
                foreach (var @event in events.OrderBy(e => e.Version))
                {
                    bankAccount.Apply(@event);
                }
                
                Logger.Info($"Account {accountId} loaded successfully from events");
                return bankAccount;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load account {accountId}: {ex.Message}");
            throw;
        }
    }

    public static BankAccount ReplayEvents(IEnumerable<Event> events)
    {
        Logger.Info($"Replaying events to reconstruct account state");
        
        var bankAccount = new BankAccount(new InMemoryEventStore(), new FileSnapshotStore());
        foreach (var @event in events.OrderBy(e => e.Version))
        {
            bankAccount.Apply(@event);
        }
        
        Logger.Info($"Events replayed successfully");
        return bankAccount;
    }

    private void EnsureAccountIsActive()
    {
        if (!IsActive)
        {
            var errorMessage = $"Account {Id} is closed";
            Logger.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
