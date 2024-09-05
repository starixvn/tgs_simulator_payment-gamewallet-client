using BigPayz.DepositHandler;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.GameWallet;

public static class PaymentConfigurationExtensions
{
    public static IServiceCollection AddBigPayzDepositHandler(this IServiceCollection services)
    {
        Dictionary<string, Type> depositHandlers = [];
        depositHandlers.Add("bank_transfer", typeof(BankTransfer));
        depositHandlers.Add("prompt_pay_qr", typeof(PromptPayQr));

        return services.AddSingleton(sp => ActivatorUtilities.CreateInstance<BankTransfer>(sp))
                        .AddSingleton(sp => ActivatorUtilities.CreateInstance<PromptPayQr>(sp))
                        .AddSingleton(sp=> ActivatorUtilities.CreateInstance<IDepositHandlerFactory>(sp, depositHandlers));

    }

}
