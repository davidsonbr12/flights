using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace flights;

[McpServerToolType]
public class FlightTools(HttpClient httpClient)
{
    [McpServerTool, Description("Get the current status of a flight by its ICAO 24-bit address.")]
    public async Task<string> GetFlightStatus(
        [Description("The ICAO 24-bit hex address of the aircraft, a0f3c1")] string icao24)
    {
        var url = $"https://opensky-network.org/api/states/all?icao24={icao24}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var states = doc.RootElement.GetProperty("states");

        if (states.ValueKind == JsonValueKind.Null)
            return $"No active flight found for {icao24}.";

        var s = states[0];
        var callsign = s[1].GetString()?.Trim() ?? "unknown";
        var lat = s[6].GetDouble();
        var lon = s[5].GetDouble();
        var alt = s[7].GetDouble();
        var spd = s[9].GetDouble();

        return $"{callsign} | lat={lat:F2} lon={lon:F2} alt={alt:F0}m spd={spd:F0}m/s";
    }
}