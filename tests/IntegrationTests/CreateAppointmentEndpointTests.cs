using System.Net;
using System.Net.Http.Json;
using Application.Appointments.CreateAppointment;

namespace IntegrationTests;

public class CreateAppointmentEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreateAppointmentEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ReturnsCreated_WhenAppointmentIsValid()
    {
        var command = new CreateAppointmentCommand(
            "Jane Doe", "+46701234567", "jane@example.com",
            new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 10, 30, 0, TimeSpan.Zero),
            "Integration test appointment");

        var response = await _client.PostAsJsonAsync("/api/appointments", command);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Deserialize into a plain anonymous-shaped record instead of the
        // internal CreateAppointmentResult type, which has private constructors
        // by design (a static-factory pattern) and isn't meant to be
        // deserialized directly.
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(body);
        Assert.Equal("Jane Doe", body!.CustomerName);
    }

    [Fact]
    public async Task Post_ReturnsConflict_WhenTimeOverlapsExisting()
    {
        var start = new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero);
        var command = new CreateAppointmentCommand(
            "First Customer", null, null, start, start.AddMinutes(30), null);

        // Create the first appointment
        await _client.PostAsJsonAsync("/api/appointments", command);

        // Try to create a second one overlapping the same slot
        var overlapping = new CreateAppointmentCommand(
            "Second Customer", null, null,
            start.AddMinutes(15), start.AddMinutes(45), null);

        var response = await _client.PostAsJsonAsync("/api/appointments", overlapping);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_ReturnsValidationProblem_WhenCustomerNameIsEmpty()
    {
        var command = new CreateAppointmentCommand(
            "", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var response = await _client.PostAsJsonAsync("/api/appointments", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record AppointmentResponse(
        Guid Id,
        string CustomerName,
        string? CustomerPhone,
        string? CustomerEmail,
        DateTimeOffset Start,
        DateTimeOffset End,
        string? Notes);
}