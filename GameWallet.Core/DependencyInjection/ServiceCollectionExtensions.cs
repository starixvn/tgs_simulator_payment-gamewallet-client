using System.Reflection;
using Payment.GameWallet;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameWallet(
       this IServiceCollection services,
       Action<GameWalletOptions> configureOptions,
       Action<IHttpClientBuilder> ConfigureAiClient)
    {
        ConfigureAiClient?.Invoke(services.AddHttpClient<GameWalletClient>());

        return services
            .AddOptions<GameWalletOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<GameWalletHasher>();
    }
}
