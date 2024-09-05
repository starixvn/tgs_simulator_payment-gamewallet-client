namespace Payment.GameWallet;
public record GameWalletClientDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string Token,
    string Currency,
    string Transaction);
