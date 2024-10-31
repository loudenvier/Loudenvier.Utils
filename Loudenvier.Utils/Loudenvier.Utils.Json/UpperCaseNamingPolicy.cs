namespace System.Text.Json;

public sealed class UpperCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToUpperInvariant();
}
