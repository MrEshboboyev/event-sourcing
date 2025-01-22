# 💡 Event Sourcing in .NET  

This repository demonstrates how to implement **Event Sourcing** in **.NET** using custom events and streams. Event Sourcing is an advanced architectural pattern that stores changes to an application’s state as a sequence of events, making it easier to track, audit, and replay operations.  

In this project, the example of a **BankAccount** is used to model:  
- Money transfers.  
- Tracking all operations on account balances.  
- Managing the account's active state.  

The implementation is designed within a **console application** to keep it simple and focused on learning.  

## 🌟 Features  

### Core Concepts  
- **Event Sourcing**: Every change to the BankAccount state is stored as an immutable event.  
- **Custom Events**: Define and handle domain-specific events like `MoneyDeposited`, `MoneyWithdrawn`, and `AccountActivated`.  
- **Stream Usage**: Leverage streams for reading and replaying events.  
- **Event Replay**: Rebuild the state of the BankAccount from a sequence of events.  

### Practical Example  
The repository uses a **BankAccount** domain to demonstrate how events can track deposits, withdrawals, and other operations.  

## 📂 Repository Structure  

```
📦 EventSourcing  
 ┣ 📂 BankAccount             # Entry point showcasing event sourcing in action  
 ┣ 📂 Program                  # Unit tests for the event sourcing implementation  
```  

## 🛠 Getting Started  

### Prerequisites  
Ensure you have the following installed:  
- .NET Core SDK  
- A modern C# IDE (e.g., Visual Studio or JetBrains Rider)  

### Step 1: Clone the Repository  
```bash  
git clone https://github.com/MrEshboboyev/event-sourcing.git  
cd EventSourcing
```  

### Step 2: Run the Console Application  
```bash  
dotnet run --project EventSourcing  
```  

### Step 3: Explore the Code  
Navigate through the `BankAccount` and `Program` files to see how custom events and stream handling are implemented.  

## 📖 Code Highlights  

### Custom Event Example  
```csharp  
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
```  

### Event Replay Example  
```csharp  
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
```  

### Stream Usage Example  
```csharp  
public class EventStream  
{  
    private readonly List<BankAccountEvent> _eventLog = new();  

    public void AppendEvent(BankAccountEvent @event)  
    {  
        _eventLog.Add(@event);  
    }  

    public IEnumerable<BankAccountEvent> GetEvents()  
    {  
        return _eventLog.AsReadOnly();  
    }  
}  
```  

## 🌐 Use Cases  

### 1. Money Transfers  
- Record deposits and withdrawals as events.  
- Rebuild account balance by replaying all events.  

### 2. Account Activity Tracking  
- Track changes like account activation or deactivation.  
- Replay events to determine the current state of the account.  

## 🧪 Testing  
The repository includes unit tests for validating the core functionalities of event sourcing.  


## 🌟 Why Use Event Sourcing?  
1. **Auditability**: Every state change is recorded, enabling complete historical tracking.  
2. **Flexibility**: Rebuild the application state at any point by replaying events.  
3. **Scalable Design**: Ideal for domains requiring event-driven and asynchronous processing.  

## 🏗 About the Author  
This project was developed by [MrEshboboyev](https://github.com/MrEshboboyev), a software developer passionate about event-driven architectures, clean code, and scalable solutions.  

## 📄 License  
This project is licensed under the MIT License. Feel free to use and adapt the code for your own projects.  

## 🔖 Tags  
C#, .NET, Event Sourcing, Streams, Bank Account, Event-Driven Architecture, Console Application, CQRS, Software Architecture, Clean Code, Financial Operations  

---  

Feel free to suggest additional features or ask questions! 🚀  
