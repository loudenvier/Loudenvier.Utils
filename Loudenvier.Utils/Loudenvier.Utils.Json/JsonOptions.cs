using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Loudenvier.Utils.Json;

public static class JsonOptions
{
    public static JsonSerializerOptions Compact { get; } = CreateCompact();

    private static JsonSerializerOptions CreateCompact() {
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        options.MakeReadOnly();
        return options;
    }
}