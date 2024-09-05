using System.ComponentModel;

namespace Payment.GameWallet.Client;
public record GameWalletDepositSettings(
    string MerchantCode,
    string Currency,
    string ApiKey,
    Guid FormId,
    string PlayerId,
    string ItemDescription,
    string ChannelType);
