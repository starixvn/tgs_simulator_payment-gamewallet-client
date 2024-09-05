namespace Payment.GameWallet;
public record DepositOrder(
    decimal Amount,
    Uri CallbackUrl,
    Guid FormId,
    string PlayerId,
    string ItemDescription);


