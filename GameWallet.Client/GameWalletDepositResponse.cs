namespace Payment.GameWallet.Client;
public record GameWalletDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl);
