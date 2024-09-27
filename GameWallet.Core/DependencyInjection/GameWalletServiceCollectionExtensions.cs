using Microsoft.Extensions.DependencyInjection;

namespace Payment.GameWallet.DependencyInjection;

public static class GameWalletServiceCollectionExtensions
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
            .RegisterAllServiceUtilityTypes(typeof(GameWalletServiceCollectionExtensions).Assembly, ServiceLifetime.Transient)
            .AddSingleton<GameWalletHasher>();
    }
}
