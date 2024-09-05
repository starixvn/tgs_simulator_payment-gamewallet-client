using Tgs.Payment.Clients;
using Payment.GameWallet.Client;

namespace Payment.GameWallet;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    GameWalletDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
