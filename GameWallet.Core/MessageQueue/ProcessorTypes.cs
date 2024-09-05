namespace Payment.GameWallet.MessageQueue;

public static class ProcessorTypes
{
    public static readonly Type DepositProcessorType = typeof(DepositProcessor);
    public static readonly Type WithdrawProcessorType = typeof(WithdrawProcessor);
    public static readonly Type DepositTimeoutCheckHandlerType = typeof(DepositTimeoutCheckHandler);
    public static readonly Type GetBalanceProcessorType = typeof(GetBalanceProcessor);
}
