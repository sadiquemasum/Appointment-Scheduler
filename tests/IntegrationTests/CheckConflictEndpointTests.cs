using System.Net;
using System.Net.Http.Json;
using Application.Appointments.CreateAppointment;

namespace IntegrationTests;

public class CheckConflictEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CheckConflictEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsHasConflictTrue_WhenOverlapExists()
    {
        var command = new CreateAppointmentCommand(
            "Existing", null, null,
            new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 1, 10, 30, 0, TimeSpan.Zero), null);

        await _client.PostAsJsonAsync("/api/appointments", command);

        var response = await _client.GetAsync(
            "/api/appointments/check-conflict?start=2026-12-01T10:15:00%2B00:00&end=2026-12-01T10:45:00%2B00:00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckConflictResponse>();

        Assert.True(body!.HasConflict);
    }

    [Fact]
    public async Task Get_ReturnsHasConflictFalse_ForFreeSlot()
    {
        var response = await _client.GetAsync(
            "/api/appointments/check-conflict?start=2027-01-01T10:00:00%2B00:00&end=2027-01-01T10:30:00%2B00:00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckConflictResponse>();

        Assert.False(body!.HasConflict);
    }

    [Fact]
    public async Task Get_ReturnsHasConflictFalse_WhenExcludingOwnAppointmentId()
    {
        var command = new Application.Appointments.CreateAppointment.CreateAppointmentCommand(
            "Self Exclude Test", null, null,
            new DateTimeOffset(2027, 4, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 4, 1, 10, 30, 0, TimeSpan.Zero), null);

        var createResponse = await _client.PostAsJsonAsync("/api/appointments", command);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedAppointmentResponse>();

        var response = await _client.GetAsync(
            $"/api/appointments/check-conflict?start=2027-04-01T10:00:00%2B00:00&end=2027-04-01T10:30:00%2B00:00&excludeId={created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckConflictResponse>();

        Assert.False(body!.HasConflict);
    }

    private record CreatedAppointmentResponse(Guid Id, string CustomerName, DateTimeOffset Start, DateTimeOffset End);

    private record CheckConflictResponse(bool HasConflict, List<object> Conflicts);
}