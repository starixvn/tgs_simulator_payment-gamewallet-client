using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tgs.Payment.Clients;

namespace Payment.GameWallet;
public record WithdrawFormBeforeVerify(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    GameWalletSettings? ApplyInfo,
    string? PaymentReturn = null);
