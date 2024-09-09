using System.ComponentModel;

namespace Payment.GameWallet.Client;
public record GameWalletWithdrawSettings(
    string MerchantCode,
    string CurrencyCode,
    string BankCode,
    string Customer,
    Guid Reference,
    decimal Amount,
    string AccountNumber,
    string AccountName,
    string MerchantSecurityCode);
