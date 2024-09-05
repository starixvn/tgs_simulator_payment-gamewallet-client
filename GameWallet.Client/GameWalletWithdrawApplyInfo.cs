namespace Payment.GameWallet.Client;
public record GameWalletWithdrawApplyInfo(
    GameWalletWithdrawSettings Settings,
    GameWalletWithdrawResponse Response);