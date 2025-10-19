using System.Text;

namespace EventSourcing;

public class ConsoleUI(IEventStore eventStore, ISnapshotStore snapshotStore)
{
    private readonly IEventStore _eventStore = eventStore;
    private readonly ISnapshotStore _snapshotStore = snapshotStore;

    public async Task RunAsync()
    {
        Logger.Info("Starting Event Sourcing Console UI");
        DisplayWelcomeMessage();

        while (true)
        {
            try
            {
                DisplayMainMenu();
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await CreateAccountAsync();
                        break;
                    case "2":
                        await LoadAccountAsync();
                        break;
                    case "3":
                        await ListAllAccountsAsync();
                        break;
                    case "4":
                        await ShowEventStatisticsAsync();
                        break;
                    case "5":
                        await DemonstratePerformanceAsync();
                        break;
                    case "6":
                        Logger.Info("Exiting Event Sourcing Console UI");
                        Console.WriteLine("Thank you for using the Event Sourcing Bank System!");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred in the UI: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    private void DisplayWelcomeMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==============================================");
        sb.AppendLine("    EVENT SOURCING BANK SYSTEM");
        sb.AppendLine("==============================================");
        sb.AppendLine();
        sb.AppendLine("This system demonstrates the power of Event Sourcing");
        sb.AppendLine("with features like:");
        sb.AppendLine("  • Event persistence");
        sb.AppendLine("  • Snapshot optimization");
        sb.AppendLine("  • Event versioning");
        sb.AppendLine("  • Comprehensive logging");
        sb.AppendLine();
        Console.WriteLine(sb.ToString());
    }

    private void DisplayMainMenu()
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAIN MENU");
        sb.AppendLine("---------");
        sb.AppendLine("1. Create New Account");
        sb.AppendLine("2. Load Existing Account");
        sb.AppendLine("3. List All Accounts");
        sb.AppendLine("4. Show Event Statistics");
        sb.AppendLine("5. Demonstrate Performance");
        sb.AppendLine("6. Exit");
        sb.AppendLine();
        sb.Append("Please select an option (1-6): ");
        Console.Write(sb.ToString());
    }

    private async Task CreateAccountAsync()
    {
        Console.Clear();
        Console.WriteLine("CREATE NEW ACCOUNT");
        Console.WriteLine("------------------");

        try
        {
            Console.Write("Enter account holder name: ");
            var accountHolder = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(accountHolder))
            {
                Console.WriteLine("Account holder name is required.");
                return;
            }

            Console.Write("Enter initial deposit amount: ");
            if (!decimal.TryParse(Console.ReadLine(), out var initialDeposit) || initialDeposit < 0)
            {
                Console.WriteLine("Invalid initial deposit amount.");
                return;
            }

            Console.Write("Enter currency (default USD): ");
            var currency = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(currency))
                currency = "USD";

            var account = BankAccount.Open(accountHolder, initialDeposit, currency, _eventStore, _snapshotStore);
            await account.SaveAsync();

            Console.WriteLine($"\nAccount created successfully!");
            Console.WriteLine($"Account ID: {account.Id}");
            Console.WriteLine($"Account Holder: {account.AccountHolder}");
            Console.WriteLine($"Balance: {account.Balance} {account.Currency}");
            Console.WriteLine($"Status: {(account.IsActive ? "Active" : "Closed")}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create account: {ex.Message}");
            Console.WriteLine($"Failed to create account: {ex.Message}");
        }
    }

    private async Task LoadAccountAsync()
    {
        Console.Clear();
        Console.WriteLine("LOAD EXISTING ACCOUNT");
        Console.WriteLine("---------------------");

        try
        {
            Console.Write("Enter account ID: ");
            var accountIdInput = Console.ReadLine();

            if (!Guid.TryParse(accountIdInput, out var accountId))
            {
                Console.WriteLine("Invalid account ID format.");
                return;
            }

            var account = await BankAccount.LoadAsync(accountId, _eventStore, _snapshotStore);

            if (account.Id == Guid.Empty)
            {
                Console.WriteLine("Account not found.");
                return;
            }

            DisplayAccountDetails(account);

            // Account operations menu
            while (true)
            {
                DisplayAccountOperationsMenu();
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await DepositAsync(account);
                        break;
                    case "2":
                        await WithdrawAsync(account);
                        break;
                    case "3":
                        await TransferAsync(account);
                        break;
                    case "4":
                        await CloseAccountAsync(account);
                        return; // Exit to main menu after closing
                    case "5":
                        DisplayAccountHistory(account);
                        break;
                    case "6":
                        return; // Return to main menu
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }

                await account.SaveAsync();
                DisplayAccountDetails(account);

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load account: {ex.Message}");
            Console.WriteLine($"Failed to load account: {ex.Message}");
        }
    }

    private void DisplayAccountDetails(BankAccount account)
    {
        Console.Clear();
        Console.WriteLine("ACCOUNT DETAILS");
        Console.WriteLine("---------------");
        Console.WriteLine($"Account ID: {account.Id}");
        Console.WriteLine($"Account Holder: {account.AccountHolder}");
        Console.WriteLine($"Balance: {account.Balance} {account.Currency}");
        Console.WriteLine($"Status: {(account.IsActive ? "Active" : "Closed")}");
        Console.WriteLine($"Version: {account.Version}");
        Console.WriteLine($"Events Count: {account.Events.Count}");
        Console.WriteLine();
    }

    private void DisplayAccountOperationsMenu()
    {
        Console.WriteLine("ACCOUNT OPERATIONS");
        Console.WriteLine("------------------");
        Console.WriteLine("1. Deposit");
        Console.WriteLine("2. Withdraw");
        Console.WriteLine("3. Transfer");
        Console.WriteLine("4. Close Account");
        Console.WriteLine("5. View History");
        Console.WriteLine("6. Back to Main Menu");
        Console.Write("Select operation (1-6): ");
    }

    private async Task DepositAsync(BankAccount account)
    {
        try
        {
            Console.Write("Enter deposit amount: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount) || amount <= 0)
            {
                Console.WriteLine("Invalid deposit amount.");
                return;
            }

            Console.Write("Enter description: ");
            var description = Console.ReadLine() ?? "Deposit";

            account.Deposit(amount, description);
            Console.WriteLine($"Successfully deposited {amount} {account.Currency}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to deposit: {ex.Message}");
            Console.WriteLine($"Failed to deposit: {ex.Message}");
        }
    }

    private async Task WithdrawAsync(BankAccount account)
    {
        try
        {
            Console.Write("Enter withdrawal amount: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount) || amount <= 0)
            {
                Console.WriteLine("Invalid withdrawal amount.");
                return;
            }

            Console.Write("Enter description: ");
            var description = Console.ReadLine() ?? "Withdrawal";

            account.Withdraw(amount, description);
            Console.WriteLine($"Successfully withdrew {amount} {account.Currency}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to withdraw: {ex.Message}");
            Console.WriteLine($"Failed to withdraw: {ex.Message}");
        }
    }

    private async Task TransferAsync(BankAccount account)
    {
        try
        {
            Console.Write("Enter transfer amount: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount) || amount <= 0)
            {
                Console.WriteLine("Invalid transfer amount.");
                return;
            }

            Console.Write("Enter recipient account ID: ");
            if (!Guid.TryParse(Console.ReadLine(), out var toAccountId))
            {
                Console.WriteLine("Invalid recipient account ID.");
                return;
            }

            Console.Write("Enter description: ");
            var description = Console.ReadLine() ?? "Transfer";

            account.TransferTo(toAccountId, amount, description);
            Console.WriteLine($"Successfully transferred {amount} {account.Currency} to account {toAccountId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to transfer: {ex.Message}");
            Console.WriteLine($"Failed to transfer: {ex.Message}");
        }
    }

    private async Task CloseAccountAsync(BankAccount account)
    {
        try
        {
            Console.Write("Enter reason for closing account: ");
            var reason = Console.ReadLine() ?? "Account closure";

            account.Close(reason);
            Console.WriteLine("Account closed successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to close account: {ex.Message}");
            Console.WriteLine($"Failed to close account: {ex.Message}");
        }
    }

    private void DisplayAccountHistory(BankAccount account)
    {
        Console.Clear();
        Console.WriteLine("ACCOUNT HISTORY");
        Console.WriteLine("---------------");

        if (account.Events.Count == 0)
        {
            Console.WriteLine("No events found for this account.");
            return;
        }

        foreach (var @event in account.Events.OrderBy(e => e.Version))
        {
            Console.WriteLine($"{@event.Timestamp:yyyy-MM-dd HH:mm:ss} | {@event.GetType().Name,-20} | Version: {@event.Version}");
            switch (@event)
            {
                case AccountOpened e:
                    Console.WriteLine($"  Account opened for {e.AccountHolder} with initial deposit of {e.InitialDeposit} {e.Currency}");
                    break;
                case MoneyDeposited e:
                    Console.WriteLine($"  Deposit of {e.Amount} {account.Currency} - {e.Description}");
                    break;
                case MoneyWithdrawn e:
                    Console.WriteLine($"  Withdrawal of {e.Amount} {account.Currency} - {e.Description}");
                    break;
                case MoneyTransferred e:
                    Console.WriteLine($"  Transfer of {e.Amount} {account.Currency} to account {e.ToAccountId} - {e.Description}");
                    break;
                case AccountClosed e:
                    Console.WriteLine($"  Account closed - {e.Reason}");
                    break;
            }
            Console.WriteLine();
        }
    }

    private async Task ListAllAccountsAsync()
    {
        Console.Clear();
        Console.WriteLine("ALL ACCOUNTS");
        Console.WriteLine("-------------");

        try
        {
            var accountEvents = await _eventStore.GetEventsByTypeAsync<AccountOpened>();
            var accounts = accountEvents.Cast<AccountOpened>();

            if (!accounts.Any())
            {
                Console.WriteLine("No accounts found.");
                return;
            }

            Console.WriteLine($"{"Account ID",-36} {"Holder",-20} {"Balance",-12} {"Currency",-8} {"Status",-10}");
            Console.WriteLine(new string('-', 90));

            foreach (var accountEvent in accounts)
            {
                // Load the account to get current balance and status
                try
                {
                    var account = await BankAccount.LoadAsync(accountEvent.AccountId, _eventStore, _snapshotStore);
                    var status = account.IsActive ? "Active" : "Closed";
                    Console.WriteLine($"{account.Id,-36} {account.AccountHolder,-20} {account.Balance,12:F2} {account.Currency,-8} {status,-10}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load account {accountEvent.AccountId}: {ex.Message}");
                    Console.WriteLine($"{accountEvent.AccountId,-36} {accountEvent.AccountHolder,-20} {"Error",-12} {"N/A",-8} {"Error",-10}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to list accounts: {ex.Message}");
            Console.WriteLine($"Failed to list accounts: {ex.Message}");
        }
    }

    private async Task ShowEventStatisticsAsync()
    {
        Console.Clear();
        Console.WriteLine("EVENT STATISTICS");
        Console.WriteLine("----------------");

        try
        {
            var accountOpenedEvents = await _eventStore.GetEventsByTypeAsync<AccountOpened>();
            var moneyDepositedEvents = await _eventStore.GetEventsByTypeAsync<MoneyDeposited>();
            var moneyWithdrawnEvents = await _eventStore.GetEventsByTypeAsync<MoneyWithdrawn>();
            var moneyTransferredEvents = await _eventStore.GetEventsByTypeAsync<MoneyTransferred>();
            var accountClosedEvents = await _eventStore.GetEventsByTypeAsync<AccountClosed>();

            Console.WriteLine($"Total Accounts Opened: {accountOpenedEvents.Count()}");
            Console.WriteLine($"Total Deposits: {moneyDepositedEvents.Count()}");
            Console.WriteLine($"Total Withdrawals: {moneyWithdrawnEvents.Count()}");
            Console.WriteLine($"Total Transfers: {moneyTransferredEvents.Count()}");
            Console.WriteLine($"Total Accounts Closed: {accountClosedEvents.Count()}");

            // Calculate total money flow
            var totalDeposits = moneyDepositedEvents.Cast<MoneyDeposited>().Sum(e => e.Amount);
            var totalWithdrawals = moneyWithdrawnEvents.Cast<MoneyWithdrawn>().Sum(e => e.Amount);
            var totalTransfers = moneyTransferredEvents.Cast<MoneyTransferred>().Sum(e => e.Amount);

            Console.WriteLine();
            Console.WriteLine("MONEY FLOW");
            Console.WriteLine("----------");
            Console.WriteLine($"Total Deposited: ${totalDeposits:F2}");
            Console.WriteLine($"Total Withdrawn: ${totalWithdrawals:F2}");
            Console.WriteLine($"Total Transferred: ${totalTransfers:F2}");
            Console.WriteLine($"Net Flow: ${totalDeposits - totalWithdrawals - totalTransfers:F2}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to show statistics: {ex.Message}");
            Console.WriteLine($"Failed to show statistics: {ex.Message}");
        }
    }

    private async Task DemonstratePerformanceAsync()
    {
        Console.Clear();
        Console.WriteLine("PERFORMANCE DEMONSTRATION");
        Console.WriteLine("-------------------------");

        try
        {
            Console.WriteLine("Creating account with many transactions to demonstrate snapshot performance...");

            var account = BankAccount.Open("Performance Test", 0, "USD", _eventStore, _snapshotStore);

            // Create many transactions to trigger snapshots
            var transactionCount = 50;
            Console.WriteLine($"Creating {transactionCount} transactions...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < transactionCount; i++)
            {
                account.Deposit(100, $"Performance deposit #{i + 1}");
            }
            stopwatch.Stop();

            await account.SaveAsync();

            Console.WriteLine($"Created {transactionCount} transactions in {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Account version: {account.Version}");
            Console.WriteLine($"Events count: {account.Events.Count}");

            // Now demonstrate loading with snapshot
            Console.WriteLine("\nLoading account (this will use snapshot if available)...");
            stopwatch.Restart();
            var loadedAccount = await BankAccount.LoadAsync(account.Id, _eventStore, _snapshotStore);
            stopwatch.Stop();

            Console.WriteLine($"Loaded account in {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Loaded account version: {loadedAccount.Version}");
            Console.WriteLine($"Loaded events count: {loadedAccount.Events.Count}");

            Console.WriteLine("\nPerformance demonstration completed!");
        }
        catch (Exception ex)
        {
            Logger.Error($"Performance demonstration failed: {ex.Message}");
            Console.WriteLine($"Performance demonstration failed: {ex.Message}");
        }
    }
}
