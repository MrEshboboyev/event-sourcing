namespace EventSourcing;

// BankAccount class with event handling
public class BankAccount
{
    public Guid Id { get; private set; }
    public string AccountHolder { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; }
    public bool IsActive { get; private set; }

    public List<Event> Events = [];

    private BankAccount()
    {
    }

    // Open a new bank account
    public static BankAccount Open(
        string accountHolder,
        decimal initialDeposit,
        string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(accountHolder))
        {
            throw new ArgumentException("Account holder name is required");
        }

        if (initialDeposit < 0)
        {
            throw new ArgumentException("The initial deposit can't be negative");
        }

        var bankAccount = new BankAccount();
        var @event = new AccountOpened(Guid.NewGuid(), accountHolder, initialDeposit, currency);

        bankAccount.Apply(@event);

        return bankAccount;
    }

    // Deposit money into the account
    public void Deposit(decimal amount, string description)
    {
        EnsureAccountIsActive();
        if (amount <= 0)
        {
            throw new ArgumentException("The deposit amount must be positive");
        }

        Apply(new MoneyDeposited(Id, amount, description));
    }

    // Withdraw money from the account
    public void Withdraw(decimal amount, string description)
    {
        EnsureAccountIsActive();
        if (amount <= 0)
        {
            throw new ArgumentException("The withdrawal amount must be positive");
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        Apply(new MoneyWithdrawn(Id, amount, description));
    }

    // Transfer money to another account
    public void TransferTo(Guid toAccountId, decimal amount, string description)
    {
        EnsureAccountIsActive();
        if (amount <= 0)
        {
            throw new ArgumentException("The transfer amount must be positive");
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        Apply(new MoneyTransferred(Id, amount, toAccountId, description));
    }

    public void Close(string reason)
    {
        EnsureAccountIsActive();
        if (Balance != 0)
        {
            throw new InvalidOperationException("Cannot close account with non-zero balance");
        }
        Apply(new AccountClosed(Id, reason));
    }
    
    private void Apply(Event @event)
    {
        // Apply the event to update the account state
        switch (@event)
        {
            case AccountOpened e:
                Id = e.AccountId;
                AccountHolder = e.AccountHolder;
                Balance = e.InitialDeposit;
                Currency = e.Currency;
                IsActive = true;
                break;
            
            case MoneyDeposited e:
                Balance += e.Amount;
                break;
            
            case MoneyWithdrawn e:
                Balance -= e.Amount;
                break;
            
            case MoneyTransferred e:
                Balance -= e.Amount;
                break;
            
            case AccountClosed e:
                IsActive = false;
                break;
        }
        
        Events.Add(@event);
    }
    
    public static BankAccount ReplayEvents(IEnumerable<Event> events)
    {
        var bankAccount = new BankAccount();
        foreach (var @event in events)
        {
            bankAccount.Apply(@event);
        }
        return bankAccount;
    }

    private void EnsureAccountIsActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Account is closed");
        }
    }
}