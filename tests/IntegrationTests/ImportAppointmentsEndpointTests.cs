using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests;

public class ImportAppointmentsEndpointTests : IDisposable
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ImportAppointmentsEndpointTests()
    {
        // Deliberately NOT using IClassFixture here - each test needs
        // its own isolated in-memory database, since import behavior
        // depends on exactly what's already been imported/created.
        // Sharing one factory across tests in this class caused
        // cross-test contamination (one test's imported data affected
        // another test's expected counts).
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Post_ImportsMockExternalEvents_AndIsIdempotentOnSecondRun()
    {
        var firstRun = await _client.PostAsync("/api/appointments/import", null);
        Assert.Equal(HttpStatusCode.OK, firstRun.StatusCode);

        var firstResult = await firstRun.Content.ReadFromJsonAsync<ImportResponse>();
        Assert.NotNull(firstResult);
        Assert.True(firstResult!.Imported > 0);

        var secondRun = await _client.PostAsync("/api/appointments/import", null);
        var secondResult = await secondRun.Content.ReadFromJsonAsync<ImportResponse>();

        Assert.Equal(0, secondResult!.Imported);
        Assert.True(secondResult.SkippedDuplicate > 0);
    }

    [Fact]
    public async Task Post_ReportsSkippedConflict_WhenExternalEventOverlapsExistingAppointment()
    {
        // The mock external API's "ext-003" event is defined at
        // 2026-09-01T10:00:00+02:00 in Program.cs. Must use the same
        // offset here, not TimeSpan.Zero (UTC) - same wall-clock digits
        // but a different actual instant otherwise, which silently
        // avoids the conflict this test is meant to trigger.
        var blocking = new Application.Appointments.CreateAppointment.CreateAppointmentCommand(
            "Jane Doe", null, null,
            new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 9, 1, 10, 30, 0, TimeSpan.FromHours(2)), null);

        await _client.PostAsJsonAsync("/api/appointments", blocking);

        var response = await _client.PostAsync("/api/appointments/import", null);
        var result = await response.Content.ReadFromJsonAsync<ImportResponse>();

        Assert.True(result!.SkippedConflict > 0);
        Assert.NotEmpty(result.ConflictDetails);
    }

    private record ImportResponse(int Imported, int SkippedDuplicate, int SkippedConflict, List<string> ConflictDetails);
}