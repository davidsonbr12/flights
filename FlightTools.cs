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
            var lat = s[6].ValueKind == JsonValueKind.Null ? (double?)null : s[6].GetDouble();
            var lon = s[5].ValueKind == JsonValueKind.Null ? (double?)null : s[5].GetDouble();
            var alt = s[7].ValueKind == JsonValueKind.Null ? (double?)null : s[7].GetDouble();
            var spd = s[9].ValueKind == JsonValueKind.Null ? (double?)null : s[9].GetDouble();
            var heading = s[10].ValueKind == JsonValueKind.Null ? (double?)null : s[10].GetDouble();
            var verticalRate = s[11].ValueKind == JsonValueKind.Null ? (double?)null : s[11].GetDouble();

            return $"{callsign} | lat={lat?.ToString("F2") ?? "?"} lon={lon?.ToString("F2") ?? "?"} alt={alt?.ToString("F0") ?? "?"}m spd={spd?.ToString("F0") ?? "?"}m/s heading={heading?.ToString("F0") ?? "?"}° vrate={verticalRate?.ToString("F1") ?? "?"}m/s";
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
    
    [McpServerTool, Description("Searches for airborne flights within a radius of a geographic center point.")]
    public async Task<string> SearchFlightsByArea(
        [Description("Latitude of the center point (-90 to 90)")] double centerLat,
        [Description("Longitude of the center point (-180 to 180)")] double centerLon,
        [Description("Search radius in kilometers (must be > 0)")] double radiusKm)
    {
        if (centerLat < -90 || centerLat > 90)
            return "Invalid centerLat: must be between -90 and 90.";
        if (centerLon < -180 || centerLon > 180)
            return "Invalid centerLon: must be between -180 and 180.";
        if (radiusKm <= 0)
            return "Invalid radiusKm: must be greater than 0.";

        var deltaLat = radiusKm / 111.32;
        var deltaLon = radiusKm / (111.32 * Math.Cos(centerLat * Math.PI / 180));
        var minLat = Math.Max(-90, centerLat - deltaLat);
        var maxLat = Math.Min(90, centerLat + deltaLat);
        var minLon = Math.Max(-180, centerLon - deltaLon);
        var maxLon = Math.Min(180, centerLon + deltaLon);

        try
        {
            var url = FormattableString.Invariant($"https://opensky-network.org/api/states/all?lamin={minLat}&lomin={minLon}&lamax={maxLat}&lomax={maxLon}");
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
                var lat = s[6].ValueKind == JsonValueKind.Null ? (double?)null : s[6].GetDouble();
                var lon = s[5].ValueKind == JsonValueKind.Null ? (double?)null : s[5].GetDouble();
                var alt = s[7].ValueKind == JsonValueKind.Null ? (double?)null : s[7].GetDouble();
                var spd = s[9].ValueKind == JsonValueKind.Null ? (double?)null : s[9].GetDouble();
                var latStr = lat.HasValue ? $"{lat:F2}" : "?";
                var lonStr = lon.HasValue ? $"{lon:F2}" : "?";
                var altStr = alt.HasValue ? $"{alt:F0}m" : "?";
                var spdStr = spd.HasValue ? $"{spd:F0}m/s" : "?";
                lines.AppendLine($"{callsign} ({icao24}) | lat={latStr} lon={lonStr} alt={altStr} spd={spdStr}");
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