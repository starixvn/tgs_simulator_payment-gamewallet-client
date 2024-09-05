namespace Payment.GameWallet;

public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string ReceiverName,
    string BankName,
    string BankAccount,
    string BankCode,
    string PlayerIp,
    string CurrencyCode);
