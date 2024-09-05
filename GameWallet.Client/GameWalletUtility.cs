using System.Text.Json;
using System.Text.Json.Serialization;

namespace Payment.GameWallet.Client;

public class GameWalletUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}
