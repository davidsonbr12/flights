# Flights MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) server that exposes real-time flight data from the [OpenSky Network](https://opensky-network.org) to AI assistants.

## Tools

### `GetFlightStatus`
Returns the current status of a specific aircraft by its ICAO 24-bit hex address.

**Input:** `icao24` — e.g. `a7f684`

**Output:** Callsign, position (lat/lon), altitude, speed, heading, and vertical rate.

### `SearchFlightsByArea`
Returns all airborne flights within a geographic bounding box (up to 10 results).

**Inputs:** `minLat`, `minLon`, `maxLat`, `maxLon`

**Output:** Callsign, ICAO24, position, altitude, and speed for each flight.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Running

```bash
dotnet run
```

The server communicates over stdio and is intended to be launched by an MCP client (e.g. Claude Code).

## Claude Code Integration

Add to your `.claude/settings.json`:

```json
{
  "mcpServers": {
    "flights": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/flights"]
    }
  }
}
```
