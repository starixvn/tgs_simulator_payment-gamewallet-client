using System.ComponentModel;

namespace Payment.GameWallet.Client;
public record GameWalletWithdrawSettings(
    string MerchantCode,
    string CurrencyCode,
    string BankCode,
    string Customer,
    Guid Reference,
    string Amount,
    string Note,
    string AccountNumber,
    string AccountName,
    string Datetime,
    string CallbackUri,
    string MerchantSecurityCode);
