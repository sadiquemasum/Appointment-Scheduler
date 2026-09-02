using System.Net;
using System.Net.Http.Json;
using Application.Appointments.CreateAppointment;
using Application.Appointments.UpdateAppointment;

namespace IntegrationTests;

public class UpdateAppointmentEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpdateAppointmentEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Put_ReturnsOk_WhenUpdateIsValid()
    {
        var create = new CreateAppointmentCommand(
            "Original Name", null, null,
            new DateTimeOffset(2027, 2, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 2, 1, 10, 30, 0, TimeSpan.Zero), null);

        var createResponse = await _client.PostAsJsonAsync("/api/appointments", create);
        var created = await createResponse.Content.ReadFromJsonAsync<AppointmentResponse>();

        var update = new UpdateAppointmentCommand(
            created!.Id, "Updated Name", null, null,
            new DateTimeOffset(2027, 2, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 2, 1, 11, 30, 0, TimeSpan.Zero), "Rescheduled");

        var response = await _client.PutAsJsonAsync($"/api/appointments/{created.Id}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.Equal("Updated Name", body!.CustomerName);
    }

    [Fact]
    public async Task Put_ReturnsConflict_WhenNewTimeOverlapsAnotherAppointment()
    {
        var first = new CreateAppointmentCommand(
            "First", null, null,
            new DateTimeOffset(2027, 2, 5, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 2, 5, 9, 30, 0, TimeSpan.Zero), null);
        var second = new CreateAppointmentCommand(
            "Second", null, null,
            new DateTimeOffset(2027, 2, 5, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 2, 5, 14, 30, 0, TimeSpan.Zero), null);

        await _client.PostAsJsonAsync("/api/appointments", first);
        var secondCreateResponse = await _client.PostAsJsonAsync("/api/appointments", second);
        var secondCreated = await secondCreateResponse.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Try to move "Second" into "First"'s slot
        var update = new UpdateAppointmentCommand(
            secondCreated!.Id, "Second", null, null,
            new DateTimeOffset(2027, 2, 5, 9, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 2, 5, 9, 45, 0, TimeSpan.Zero), null);

        var response = await _client.PutAsJsonAsync($"/api/appointments/{secondCreated.Id}", update);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_ReturnsNotFound_ForNonExistentId()
    {
        var nonExistentId = Guid.NewGuid();
        var update = new UpdateAppointmentCommand(
            nonExistentId, "Nobody", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var response = await _client.PutAsJsonAsync($"/api/appointments/{nonExistentId}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenRouteIdAndBodyIdMismatch()
    {
        var update = new UpdateAppointmentCommand(
            Guid.NewGuid(), "Someone", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var response = await _client.PutAsJsonAsync($"/api/appointments/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record AppointmentResponse(Guid Id, string CustomerName, DateTimeOffset Start, DateTimeOffset End);
}
