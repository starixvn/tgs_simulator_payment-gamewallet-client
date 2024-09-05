namespace Payment.GameWallet;

public interface IDepositHandlerFactory
{
    IDepositHandler GetDepositHandler(string channelCode);
}