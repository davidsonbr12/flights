# Flights MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) server that exposes real-time flight data from the [OpenSky Network](https://opensky-network.org) to AI assistants.

## Tools

### `GetFlightStatus`
Returns the current status of a specific aircraft by its ICAO 24-bit hex address.

**Input:** `icao24` — e.g. `a7f684`

**Output:** Callsign, position (lat/lon), altitude, speed, heading, and vertical rate.

### `SearchFlightsByArea`
Returns all airborne flights within a radius of a geographic center point (up to 10 results).

**Inputs:** `centerLat`, `centerLon`, `radiusKm`

**Output:** Callsign, ICAO24, position, altitude, and speed for each flight.

## Running with Docker

```bash
docker build -t flights-mcp .
docker run -i --rm flights-mcp
```

## Running without Docker

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
dotnet run
```

## Claude Code Integration

**Docker (recommended):**

```json
{
  "mcpServers": {
    "flights": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "flights-mcp"]
    }
  }
}
```

**Without Docker:**

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
