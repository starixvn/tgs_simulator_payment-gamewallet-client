using Tgs.Payment.Clients;
using Payment.GameWallet.Client;

namespace Payment.GameWallet;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    GameWalletWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
