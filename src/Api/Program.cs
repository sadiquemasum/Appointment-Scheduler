using Application.Appointments.CreateAppointment;
using Application.Appointments.GetAppointments;
using Application.Appointments.UpdateAppointment;
using Application.Appointments.DeleteAppointment;
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


app.MapGet("/api/appointments", async (
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken,
    DateTimeOffset? from = null,
    DateTimeOffset? to = null) =>
{
    var result = await mediator.Send(new GetAppointmentsQuery(from, to), cancellationToken);
    return Results.Ok(result);
});

app.MapPut("/api/appointments/{id:guid}", async (
    Guid id,
    UpdateAppointmentCommand command,
    [FromServices] IValidator<UpdateAppointmentCommand> validator,
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { message = "Route id and body id must match." });
    }

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var result = await mediator.Send(command, cancellationToken);

    if (result.NotFound)
    {
        return Results.NotFound();
    }

    if (!result.Success)
    {
        return Results.Conflict(new
        {
            message = "The proposed time conflicts with existing appointment(s).",
            conflicts = result.Conflicts.Select(c => new { c.Id, c.CustomerName, c.Start, c.End })
        });
    }

    return Results.Ok(result.Appointment);
});

app.MapDelete("/api/appointments/{id:guid}", async (
    Guid id,
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var deleted = await mediator.Send(new DeleteAppointmentCommand(id), cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();