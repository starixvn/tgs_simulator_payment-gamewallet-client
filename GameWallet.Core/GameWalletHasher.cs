using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Payment.GameWallet;

public class GameWalletHasher(ILogger<GameWalletHasher> logger)
{
    private readonly ILogger<GameWalletHasher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string ComputeHash(string privateKey, IEnumerable<string> data)
    {
        var content = string.Join("", data);

        _logger.LogTrace("FullText: {content}", content);

        var enc = Encoding.UTF8;
        var hmac = new HMACSHA256(enc.GetBytes(content));
        hmac.Initialize();
        var buffer = enc.GetBytes(privateKey);

        return BitConverter.ToString(hmac.ComputeHash(buffer)).Replace("-", "").ToLower();
    }
}
