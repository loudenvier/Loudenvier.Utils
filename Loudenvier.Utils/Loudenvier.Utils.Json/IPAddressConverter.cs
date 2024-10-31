using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loudenvier.Utils.Json;

/// <summary>
/// System.Text.Json can't handle <see cref="IPAddress"/> (fails to (de)serialize <see cref="IPAddress.ScopeId"/>). 
/// This converter fixes that.
/// </summary>
public class IPAdressConverter : JsonConverter<IPAddress>
{
    public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
        => IPAddress.Parse(reader.GetString()!);
    
    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) 
        => writer.WriteStringValue(value.ToString());
}
