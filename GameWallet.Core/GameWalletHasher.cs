using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Payment.GameWallet;

public class GameWalletHasher(ILogger<GameWalletHasher> logger)
{
    private readonly ILogger<GameWalletHasher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string ComputeMd5(string content)
    {

        _logger.LogTrace("FullText: {Content}", content);

        var bytes = Encoding.UTF8.GetBytes(content);
        var md5 = MD5.HashData(bytes);

        return BitConverter.ToString(md5).Replace("-", "").ToLower();

    }

    public string EncodeParam(string content)
    {
        _logger.LogTrace("FullText: {PlainText}", content);

        var based64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        if (string.IsNullOrEmpty(based64))
            return string.Empty;

        var length = based64.Length;
        if (length < 100)
            throw new InvalidDataException("Invalid text to encode");
        var key = new Random().Next(1, 10);
        var param = based64.Insert(length - 10, $"{key}").Insert(key, Guid.NewGuid().ToString("n")[..1]);
        return HttpUtility.UrlEncode(param);
    }

    public string ComputeHash(string privateKey, IEnumerable<string> data)
    {
        var content = string.Join("", data);

        _logger.LogTrace("FullText: {Content}", content);

        var enc = Encoding.UTF8;
        var hmac = new HMACSHA256(enc.GetBytes(content));
        hmac.Initialize();
        var buffer = enc.GetBytes(privateKey);

        return BitConverter.ToString(hmac.ComputeHash(buffer)).Replace("-", "").ToLower();
    }
}
