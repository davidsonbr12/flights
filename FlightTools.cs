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
    public string GetFlightStatus(
        [Description("The ICAO 24-bit hex address of the aircraft, a0f3c1")] string icao24)
    {
        return $"Flight {icao24}: hardcoded placeholder, HTTP coming next.";
    }
}