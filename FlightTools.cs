using System.ComponentModel;
using ModelContextProtocol.Server;

namespace flights;

[McpServerToolType]
public static class FlightTools
{
    [McpServerTool, Description("Get the current status of a flight by its ICAO 24-bit address.")]
    public static string GetFlightStatus(
        [Description("The ICAO 24-bit hex address of the aircraft, a0f3c1")] string icao24)
    {
        return $"Flight {icao24}: hardcoded placeholder, HTTP coming next.";
    }
}