using System.Net;
using System.Net.Http.Json;
using Application.Appointments.CreateAppointment;

namespace IntegrationTests;

public class DeleteAppointmentEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeleteAppointmentEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Delete_RemovesAppointment_ThenReturns404OnSecondDelete()
    {
        var command = new CreateAppointmentCommand(
            "To Delete", null, null,
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 1, 10, 30, 0, TimeSpan.Zero), null);

        var createResponse = await _client.PostAsJsonAsync("/api/appointments", command);
        var created = await createResponse.Content.ReadFromJsonAsync<AppointmentResponse>();

        var firstDelete = await _client.DeleteAsync($"/api/appointments/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);

        var secondDelete = await _client.DeleteAsync($"/api/appointments/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_ForNonExistentId()
    {
        var response = await _client.DeleteAsync($"/api/appointments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record AppointmentResponse(Guid Id, string CustomerName, DateTimeOffset Start, DateTimeOffset End);
}
