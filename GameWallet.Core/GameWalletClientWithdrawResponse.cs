namespace Payment.GameWallet;
public record GameWalletClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal MerchantBalance);
