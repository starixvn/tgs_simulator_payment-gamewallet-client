using System.ComponentModel;

namespace Payment.GameWallet.Client;
public record GameWalletWithdrawSettings(
    string MerchantCode,
    string ApiKey,
    Guid FormId,
    string ReceiverName,
    string BankName,
    string BankAccount,
    string BankCode,
    string PlayerIp,
    string CurrencyCode);
