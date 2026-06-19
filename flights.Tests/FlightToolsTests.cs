using System.Net;
using flights;

namespace flights.Tests;

public class FlightToolsTests
{
    private static HttpClient MakeClient(HttpStatusCode status, string body) =>
        new HttpClient(new FakeHandler(status, body));

    private static HttpClient MakeThrowingClient(Exception ex) =>
        new HttpClient(new ThrowingHandler(ex));

    private const string SingleFlightJson = """
        {
          "time": 1000000,
          "states": [
            ["abc123", "FLIGHT1 ", "US", 1000, 1000, -73.5, 40.7, 10000, false, 250.0, 180.0, 0.5, null, null, null, false, 0]
          ]
        }
        """;

    private const string NullStatesJson = """{"time": 1000000, "states": null}""";
    private const string EmptyStatesJson = """{"time": 1000000, "states": []}""";

    [Fact]
    public async Task ReturnsFlightList_WhenFlightsExist()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, SingleFlightJson));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.Contains("FLIGHT1", result);
        Assert.Contains("abc123", result);
    }

    [Fact]
    public async Task ReturnsNoFlights_WhenStatesIsNull()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, NullStatesJson));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.Equal("No flights found in that area.", result);
    }

    [Fact]
    public async Task ReturnsNoFlights_WhenStatesIsEmpty()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, EmptyStatesJson));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.Equal("No flights found in that area.", result);
    }

    [Fact]
    public async Task ReturnsError_WhenCenterLatBelowMinus90()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, NullStatesJson));
        var result = await tools.SearchFlightsByArea(-91.0, 0.0, 100.0);

        Assert.Contains("Invalid centerLat", result);
    }

    [Fact]
    public async Task ReturnsError_WhenCenterLatAbove90()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, NullStatesJson));
        var result = await tools.SearchFlightsByArea(91.0, 0.0, 100.0);

        Assert.Contains("Invalid centerLat", result);
    }

    [Fact]
    public async Task ReturnsError_WhenCenterLonBelowMinus180()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, NullStatesJson));
        var result = await tools.SearchFlightsByArea(0.0, -181.0, 100.0);

        Assert.Contains("Invalid centerLon", result);
    }

    [Fact]
    public async Task ReturnsError_WhenCenterLonAbove180()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, NullStatesJson));
        var result = await tools.SearchFlightsByArea(0.0, 181.0, 100.0);

        Assert.Contains("Invalid centerLon", result);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public async Task ReturnsError_WhenRadiusIsNotPositive(double radius)
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, NullStatesJson));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, radius);

        Assert.Contains("Invalid radiusKm", result);
    }

    [Fact]
    public async Task ReturnsHttpError_WhenApiReturnsNonSuccess()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.InternalServerError, ""));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.Contains("Failed to reach OpenSky API", result);
    }

    [Fact]
    public async Task ReturnsUnexpectedError_WhenNetworkThrows()
    {
        var tools = new FlightTools(MakeThrowingClient(new HttpRequestException("timeout")));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.Contains("Failed to reach OpenSky API", result);
    }

    [Fact]
    public async Task SendsCorrectBboxUrl_ForKnownCenterAndRadius()
    {
        Uri? capturedUri = null;
        var handler = new CapturingHandler(HttpStatusCode.OK, NullStatesJson, uri => capturedUri = uri);
        var tools = new FlightTools(new HttpClient(handler));

        await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.NotNull(capturedUri);
        var query = capturedUri!.Query;

        // deltaLat = 100 / 111.32 ≈ 0.898, deltaLon = 100 / (111.32 * cos(40°)) ≈ 1.173
        var parsed = query.TrimStart('?')
            .Split('&')
            .ToDictionary(p => p.Split('=')[0], p => double.Parse(p.Split('=')[1], System.Globalization.CultureInfo.InvariantCulture));

        Assert.InRange(parsed["lamin"], 39.0, 40.0); // ~39.10
        Assert.InRange(parsed["lamax"], 40.0, 41.0); // ~40.90
        Assert.InRange(parsed["lomin"], -75.5, -74.0); // ~-75.17
        Assert.InRange(parsed["lomax"], -74.0, -72.5); // ~-72.83
    }

    [Fact]
    public async Task ReturnsUnexpectedError_WhenResponseBodyIsMalformedJson()
    {
        var tools = new FlightTools(MakeClient(HttpStatusCode.OK, "not json"));
        var result = await tools.SearchFlightsByArea(40.0, -74.0, 100.0);

        Assert.Contains("Unexpected error", result);
    }
}

class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
}

class ThrowingHandler(Exception exception) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw exception;
}

class CapturingHandler(HttpStatusCode status, string body, Action<Uri> capture) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        capture(request.RequestUri!);
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
