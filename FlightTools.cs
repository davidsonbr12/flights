using System.ComponentModel;
using ModelContextProtocol.Server;

namespace flights;

[McpServerToolType]
public class FlightTools
{
    private readonly HttpClient _httpClient;

    public FlightTools(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    [McpServerTool, Description("Get the current status of a flight by its ICAO 24-bit address.")]
    public async Task<string> GetFlightStatus(
        [Description("The ICAO 24-bit hex address of the aircraft, a0f3c1")] string icao24)
    {
        var url = $"https://opensky-network.org/api/states/all?icao24={icao24}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}