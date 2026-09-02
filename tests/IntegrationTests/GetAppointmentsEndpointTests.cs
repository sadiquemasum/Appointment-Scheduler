using System.Net;
using System.Net.Http.Json;
using Application.Appointments.CreateAppointment;

namespace IntegrationTests;

public class GetAppointmentsEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetAppointmentsEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsOnlyAppointmentsWithinDateRange()
    {
        var inRange = new CreateAppointmentCommand(
            "In Range", null, null,
            new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 1, 9, 30, 0, TimeSpan.Zero), null);
        var outOfRange = new CreateAppointmentCommand(
            "Out Of Range", null, null,
            new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 20, 9, 30, 0, TimeSpan.Zero), null);

        await _client.PostAsJsonAsync("/api/appointments", inRange);
        await _client.PostAsJsonAsync("/api/appointments", outOfRange);

        var response = await _client.GetAsync(
            "/api/appointments?from=2026-10-01T00:00:00%2B00:00&to=2026-10-05T00:00:00%2B00:00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AppointmentResponse>>();

        Assert.NotNull(body);
        Assert.Contains(body!, a => a.CustomerName == "In Range");
        Assert.DoesNotContain(body!, a => a.CustomerName == "Out Of Range");
    }

    [Fact]
    public async Task Get_ReturnsAllAppointments_WhenNoFiltersProvided()
    {
        var command = new CreateAppointmentCommand(
            "No Filter Test", null, null,
            new DateTimeOffset(2027, 3, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 3, 1, 10, 30, 0, TimeSpan.Zero), null);

        await _client.PostAsJsonAsync("/api/appointments", command);

        var response = await _client.GetAsync("/api/appointments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AppointmentResponse>>();

        Assert.NotNull(body);
        Assert.Contains(body!, a => a.CustomerName == "No Filter Test");
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenFromParameterIsMalformed()
    {
        var response = await _client.GetAsync("/api/appointments?from=not-a-date");

        // ASP.NET Core's model binding fails to parse the malformed value
        // and rejects the request before it reaches our handler.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record AppointmentResponse(Guid Id, string CustomerName, DateTimeOffset Start, DateTimeOffset End);
}