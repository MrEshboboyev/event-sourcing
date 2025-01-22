namespace EventSourcing;

// BankAccount class with event handling
public class BankAccount
{
    public Guid Id { get; private set; }
    public string AccountHolder { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; }

    private List<Event> _events = [];

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

        return bankAccount;
    }
}