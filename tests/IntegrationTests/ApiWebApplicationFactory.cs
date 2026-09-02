using Application.Common;
using Infrastructure;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real SQLite file-based registration...
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppointmentsDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            // ...and replace it with an in-memory SQLite connection that
            // lives only for the duration of this test run.
            _connection.Open();
            services.AddDbContext<AppointmentsDbContext>(options =>
                options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
            db.Database.EnsureCreated();

            // The mock external calendar endpoint lives inside this same app,
            // but WebApplicationFactory runs entirely in-memory with no real
            // socket listener. Redirect the external client's HttpClient to
            // use the TestServer's in-memory handler instead of a real
            // network connection.
            services.AddHttpClient<IExternalCalendarClient, ExternalCalendarClient>()
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
