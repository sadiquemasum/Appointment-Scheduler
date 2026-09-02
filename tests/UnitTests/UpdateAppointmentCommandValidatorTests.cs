using Application.Appointments.UpdateAppointment;

namespace UnitTests;

public class UpdateAppointmentCommandValidatorTests
{
    private readonly UpdateAppointmentCommandValidator _validator = new();

    [Fact]
    public void Validate_Fails_WhenCustomerNameIsEmpty()
    {
        var command = new UpdateAppointmentCommand(
            Guid.NewGuid(), "", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAppointmentCommand.CustomerName));
    }

    [Fact]
    public void Validate_Fails_WhenEndIsBeforeStart()
    {
        var start = DateTimeOffset.UtcNow;
        var command = new UpdateAppointmentCommand(
            Guid.NewGuid(), "Jane Doe", null, null,
            start, start.AddMinutes(-10), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAppointmentCommand.End));
    }

    [Fact]
    public void Validate_Fails_WhenEmailFormatIsInvalid()
    {
        var command = new UpdateAppointmentCommand(
            Guid.NewGuid(), "Jane Doe", null, "not-an-email",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAppointmentCommand.CustomerEmail));
    }

    [Fact]
    public void Validate_Succeeds_WithAllValidFields()
    {
        var command = new UpdateAppointmentCommand(
            Guid.NewGuid(), "Jane Doe", "+46701234567", "jane@example.com",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), "Notes");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}