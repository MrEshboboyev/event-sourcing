# 💡 Advanced Event Sourcing in .NET

This repository demonstrates a **production-ready Event Sourcing** implementation in **.NET** that showcases the true power and potential of this architectural pattern. Unlike basic examples, this implementation includes enterprise-grade features essential for real-world applications.

Event Sourcing is an advanced architectural pattern that stores changes to an application's state as a sequence of events, making it easier to track, audit, replay operations, and maintain a complete historical record of all state changes.

In this project, we use a **Banking Domain** to demonstrate how Event Sourcing can be implemented with sophisticated features like persistence, snapshotting, versioning, and comprehensive error handling.

## 🌟 Key Features

### Core Event Sourcing Concepts
- **Immutable Events**: Every state change is stored as an immutable event
- **Event Replay**: Reconstruct current state by replaying events in order
- **Domain-Driven Design**: Clean separation of domain logic and event handling
- **CQRS-ready Architecture**: Command and Query separation for scalability

### Advanced Features
- **Event Persistence**: File-based event store with JSON serialization
- **Snapshot Optimization**: Automatic snapshot creation for performance
- **Event Versioning**: Backward compatibility support for event schema evolution
- **Comprehensive Logging**: Detailed audit trail with timestamped events
- **Error Handling**: Robust exception handling and recovery mechanisms
- **Interactive Console UI**: User-friendly interface to demonstrate all features

### Performance & Scalability
- **Memory Optimization**: Snapshot-based state reconstruction
- **Concurrency Support**: Thread-safe operations
- **Extensible Design**: Easy to add new event types and domain entities

## 📂 Repository Structure

```
📦 EventSourcing
 ┣ 📜 BankAccount.cs        # Core domain model with event handling logic
 ┣ 📜 EventStore.cs         # Persistence layer with file-based storage
 ┣ 📜 Snapshot.cs           # Snapshot functionality for performance optimization
 ┣ 📜 Logger.cs             # Comprehensive logging utility
 ┣ 📜 ConsoleUI.cs          # Interactive console user interface
 ┣ 📜 Program.cs            # Application entry point
 ┗ 📜 EventSourcing.csproj  # Project configuration
```

## 🛠 Getting Started

### Prerequisites
Ensure you have the following installed:
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A modern C# IDE (e.g., Visual Studio, Visual Studio Code, or JetBrains Rider)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/MrEshboboyev/event-sourcing.git
cd event-sourcing
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

### Running the Application

Execute the console application:
```bash
dotnet run --project EventSourcing
```

This will launch the interactive console UI where you can:
- Create new bank accounts
- Perform transactions (deposits, withdrawals, transfers)
- View account history
- See performance optimizations in action

## 📖 Advanced Implementation Details

### Event Store Architecture
Our implementation includes both in-memory and file-based event stores:

```csharp
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events);
    Task<IEnumerable<Event>> GetEventsAsync(Guid aggregateId);
    Task<IEnumerable<Event>> GetEventsByTypeAsync<T>() where T : Event;
}
```

### Snapshot Functionality
To optimize performance with large event streams, we implement snapshotting:

```csharp
public class BankAccountSnapshot
{
    public Guid Id { get; set; }
    public string AccountHolder { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime LastSnapshotAt { get; set; }
}
```

### Event Versioning
Our event store supports versioning for backward compatibility:

```csharp
public class EventJsonConverter : JsonConverter<Event>
{
    public override void Write(Utf8JsonWriter writer, Event value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("EventType", value.GetType().Name);
        writer.WriteNumber("EventVersion", GetEventVersion(value));
        // ... serialize event data
    }
}
```

### Comprehensive Error Handling
All operations include detailed logging and error handling:

```csharp
public static BankAccount Open(string accountHolder, decimal initialDeposit, string currency = "USD")
{
    Logger.Info($"Opening new bank account for {accountHolder} with initial deposit {initialDeposit} {currency}");
    
    if (string.IsNullOrWhiteSpace(accountHolder))
    {
        var errorMessage = "Account holder name is required";
        Logger.Error(errorMessage);
        throw new ArgumentException(errorMessage);
    }
    // ... rest of validation and creation logic
}
```

## 🎯 Use Cases & Benefits

### Financial Systems
- Complete audit trail of all transactions
- Regulatory compliance with historical data
- Easy reconciliation and reporting

### E-commerce Platforms
- Order history tracking
- Customer behavior analysis
- Inventory change monitoring

### Healthcare Applications
- Patient record changes
- Treatment history tracking
- Compliance with medical regulations

## 🔧 Performance Optimizations

### Snapshot Threshold
The system automatically creates snapshots every 10 events to optimize load times:

```csharp
private const int SNAPSHOT_THRESHOLD = 10; // Create snapshot every 10 events
```

### Event Streaming
Events are processed and stored efficiently with minimal memory overhead.

## 🧪 Testing & Validation

The implementation includes comprehensive validation:
- Input validation for all operations
- State consistency checks
- Error recovery mechanisms
- Thread safety verification

## 🌟 Why Use This Event Sourcing Implementation?

1. **Production-Ready**: Includes all features needed for enterprise applications
2. **Performance Optimized**: Snapshotting and efficient event storage
3. **Extensible Design**: Easy to add new domain entities and event types
4. **Maintainable Code**: Clean architecture with separation of concerns
5. **Comprehensive Logging**: Complete audit trail for compliance and debugging
6. **Backward Compatibility**: Event versioning for schema evolution
7. **Interactive Demonstration**: Console UI to showcase all features

## 🏗 About the Author

This project was developed by [MrEshboboyev](https://github.com/MrEshboboyev), a software developer passionate about event-driven architectures, clean code, and scalable solutions.

## 📄 License

This project is licensed under the MIT License. Feel free to use and adapt the code for your own projects.

## 🔖 Tags

C#, .NET, Event Sourcing, DDD, CQRS, Persistence, Snapshots, Versioning, Audit Trail, Financial Systems, Banking, Software Architecture, Clean Code

---

Feel free to suggest additional features or ask questions! 🚀