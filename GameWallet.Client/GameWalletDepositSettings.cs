namespace Payment.GameWallet.Client;
public record GameWalletDepositSettings(
    string MerchantCode,
    string MerchantSecurityCode,
    string CurrencyCode,
    string BankCode,
    string Customer,
    Guid Reference,
    string SuccessUrl,
    string FailUrl,
    string Language,
    string ClientIP,
    string Method,
    double ExpirationTime);
