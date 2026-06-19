using System.ComponentModel;
using ModelContextProtocol.Server;

namespace flights;

[McpServerToolType]
public static class FlightTools
{
    [McpServerTool, Description("Returns a placeholder — real implementation coming in step 2.")]
    public static string Ping() => "Flight MCP server is running.";
}