using System.Text.Json.Serialization;

namespace EmmyLua.Configuration.Custom;

public class CustomDiagnostics
{
    [JsonPropertyName("buffIdValidation")]
    public BuffIdDiagnostics BuffIdConfig { get; set; } = new();
}
