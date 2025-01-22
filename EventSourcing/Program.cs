using EventSourcing;

Console.WriteLine("Event Sourcing : Hello world!");

var bankAccount = BankAccount.Open("M J", 1000);

bankAccount.Deposit(500, "Salary deposit");
bankAccount.Withdraw(200, "ATM withdrawal");
bankAccount.TransferTo(Guid.NewGuid(), 300, "Transfer to savings");
bankAccount.Withdraw(bankAccount.Balance, "Withdrawing before closing account");
bankAccount.Close("Completing the demo");

// Print the final balance and all events
Console.WriteLine($"Final balance: {bankAccount.Balance}");

foreach (var @event in bankAccount.Events)
{
    Console.WriteLine($"Event: {@event.GetType().Name} at {@event.Timestamp}");
}

var events = bankAccount.Events;

var theSameAccount = BankAccount.ReplayEvents(events);

try
{
    theSameAccount.Deposit(100, "Replaying deposit");
}
catch (Exception e)
{
    Console.WriteLine(e);
}


// Base event type
public abstract record Event(Guid StreamId)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Specific events for bank account domain
public record AccountOpened(
    Guid AccountId,
    string AccountHolder,
    decimal InitialDeposit,
    string Currency = "USD") : Event(AccountId);

public record MoneyDeposited(
    Guid AccountId,
    decimal Amount,
    string Description) : Event(AccountId);

public record MoneyWithdrawn(
    Guid AccountId,
    decimal Amount,
    string Description) : Event(AccountId);

public record MoneyTransferred(
    Guid AccountId,
    decimal Amount,
    Guid ToAccountId,
    string Description) : Event(AccountId);

public record AccountClosed(
    Guid AccountId,
    string Reason) : Event(AccountId);
