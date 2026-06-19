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
        try
        {
            var url = $"https://opensky-network.org/api/states/all?icao24={icao24}";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var states = doc.RootElement.GetProperty("states");

            if (states.ValueKind == JsonValueKind.Null || states.GetArrayLength() == 0)
                return $"No active flight found for {icao24}.";

            var s = states[0];
            var callsign = s[1].GetString()?.Trim() ?? "unknown";
            var lat = s[6].GetDouble();
            var lon = s[5].GetDouble();
            var alt = s[7].GetDouble();
            var spd = s[9].GetDouble();
            var heading = s[10].GetDouble();
            var verticalRate = s[11].GetDouble();

            return $"{callsign} | lat={lat:F2} lon={lon:F2} alt={alt:F0}m spd={spd:F0}m/s heading={heading:F0}° vrate={verticalRate:F1}m/s";
        }
        catch (HttpRequestException ex)
        {
            return $"Failed to reach OpenSky API: {ex.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("Searches a geographic bounding box and returns all airborne flights in that area.")]
    public async Task<string> SearchFlightsByArea(
        [Description("Minimum latitude of the search area")] double minLat,
        [Description("Minimum longitude of the search area")] double minLon,
        [Description("Maximum latitude of the search area")] double maxLat,
        [Description("Maximum longitude of the search area")] double maxLon)
    {
        try
        {
            var url = $"https://opensky-network.org/api/states/all?lamin={minLat}&lomin={minLon}&lamax={maxLat}&lomax={maxLon}";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var states = doc.RootElement.GetProperty("states");

            if (states.ValueKind == JsonValueKind.Null || states.GetArrayLength() == 0)
                return "No flights found in that area.";

            var total = states.GetArrayLength();
            var cap = Math.Min(total, 10);
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Showing {cap} of {total} flights:");

            for (int i = 0; i < cap; i++)
            {
                var s = states[i];
                var icao24 = s[0].GetString() ?? "unknown";
                var callsign = s[1].GetString()?.Trim() ?? "unknown";
                var lat = s[6].GetDouble();
                var lon = s[5].GetDouble();
                var alt = s[7].GetDouble();
                var spd = s[9].GetDouble();
                lines.AppendLine($"{callsign} ({icao24}) | lat={lat:F2} lon={lon:F2} alt={alt:F0}m spd={spd:F0}m/s");
            }

            return lines.ToString();
        }
        catch (HttpRequestException ex)
        {
            return $"Failed to reach OpenSky API: {ex.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.Message}";
        }
    }
}