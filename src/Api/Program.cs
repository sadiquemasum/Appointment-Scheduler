using Application.Appointments.CreateAppointment;
using FluentValidation;
using MediatR;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppointmentsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<Application.Common.IAppointmentRepository, Infrastructure.Repositories.AppointmentRepository>();
builder.Services.AddSingleton<Domain.Services.ConflictChecker>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Application.Appointments.CreateAppointment.CreateAppointmentCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(CreateAppointmentCommandValidator).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/appointments", async (
    CreateAppointmentCommand command,
    [FromServices] IValidator<CreateAppointmentCommand> validator,
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var result = await mediator.Send(command, cancellationToken);

    if (!result.Success)
    {
        return Results.Conflict(new
        {
            message = "The proposed time conflicts with existing appointment(s).",
            conflicts = result.Conflicts.Select(c => new { c.Id, c.CustomerName, c.Start, c.End })
        });
    }

    return Results.Created($"/api/appointments/{result.Appointment!.Id}", result.Appointment);
});

app.Run();