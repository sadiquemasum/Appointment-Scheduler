using Application.Appointments.CreateAppointment;

namespace UnitTests;

public class CreateAppointmentCommandValidatorTests
{
    private readonly CreateAppointmentCommandValidator _validator = new();

    [Fact]
    public void Validate_Fails_WhenCustomerNameIsEmpty()
    {
        var command = new CreateAppointmentCommand(
            "", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.CustomerName));
    }

    [Fact]
    public void Validate_Fails_WhenEndIsBeforeStart()
    {
        var start = DateTimeOffset.UtcNow;
        var command = new CreateAppointmentCommand(
            "Jane Doe", null, null, start, start.AddMinutes(-10), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.End));
    }

    [Fact]
    public void Validate_Fails_WhenEndEqualsStart()
    {
        var start = DateTimeOffset.UtcNow;
        var command = new CreateAppointmentCommand(
            "Jane Doe", null, null, start, start, null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenEmailFormatIsInvalid()
    {
        var command = new CreateAppointmentCommand(
            "Jane Doe", null, "not-an-email",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.CustomerEmail));
    }

    [Fact]
    public void Validate_Succeeds_WhenEmailIsNull()
    {
        var command = new CreateAppointmentCommand(
            "Jane Doe", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WithAllValidFields()
    {
        var command = new CreateAppointmentCommand(
            "Jane Doe", "+46701234567", "jane@example.com",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), "Some notes");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenCustomerNameExceedsMaxLength()
    {
        var command = new CreateAppointmentCommand(
            new string('A', 201), null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAppointmentCommand.CustomerName));
    }
}