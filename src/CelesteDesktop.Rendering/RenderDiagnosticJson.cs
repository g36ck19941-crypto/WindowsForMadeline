using System.Text.Json;
using System.Text.Json.Serialization;

namespace CelesteDesktop.Rendering;

public static class RenderDiagnosticJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string SerializeLine(RenderDiagnosticEvent diagnosticEvent)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvent);
        return JsonSerializer.Serialize(diagnosticEvent, Options);
    }
}
