namespace Payment.GameWallet.Client;
public record GameWalletWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal MerchantBalance);
