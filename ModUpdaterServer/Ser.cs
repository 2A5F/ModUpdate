using ModUpdater.Model;
using System.Text.Json.Serialization;

namespace ModUpdaterServer.Model;

public class SerPack
{
    public IEnumerable<ConfigModel> configs { get; set; } = null!;
}

[JsonSerializable(typeof(SerPack), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class SerPackJsonContext : JsonSerializerContext { }
