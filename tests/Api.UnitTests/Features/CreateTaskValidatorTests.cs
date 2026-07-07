using Api.Common;
using Api.Features.Tasks;
using Priority = Api.Domain.Priority;

namespace Api.UnitTests.Features;

public class CreateTaskValidatorTests
{
    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static CreateTaskValidator MakeValidator(DateTimeOffset? now = null) =>
        new(new FakeClock(now ?? new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceTitle_ReturnsTitleError(string title)
    {
        var errors = MakeValidator().Validate(new CreateTaskRequest(title, null, null, null));

        Assert.True(errors.ContainsKey("title"));
    }

    [Fact]
    public void Validate_TitleOver200Chars_ReturnsTitleError()
    {
        var title = new string('a', 201);

        var errors = MakeValidator().Validate(new CreateTaskRequest(title, null, null, null));

        Assert.True(errors.ContainsKey("title"));
    }

    [Fact]
    public void Validate_ValidTitle_ReturnsNoTitleError()
    {
        var errors = MakeValidator().Validate(new CreateTaskRequest("Valid title", null, null, null));

        Assert.False(errors.ContainsKey("title"));
    }

    [Fact]
    public void Validate_DescriptionOver2000Chars_ReturnsDescriptionError()
    {
        var description = new string('a', 2001);

        var errors = MakeValidator().Validate(new CreateTaskRequest("Title", description, null, null));

        Assert.True(errors.ContainsKey("description"));
    }

    [Fact]
    public void Validate_DescriptionAt2000Chars_ReturnsNoDescriptionError()
    {
        var description = new string('a', 2000);

        var errors = MakeValidator().Validate(new CreateTaskRequest("Title", description, null, null));

        Assert.False(errors.ContainsKey("description"));
    }

    [Fact]
    public void Validate_DueDateInThePast_ReturnsDueDateError()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var pastDueDate = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1);

        var errors = MakeValidator(now).Validate(new CreateTaskRequest("Title", null, null, pastDueDate));

        Assert.True(errors.ContainsKey("dueDate"));
    }

    [Fact]
    public void Validate_DueDateToday_ReturnsNoDueDateError()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var errors = MakeValidator(now).Validate(new CreateTaskRequest("Title", null, null, today));

        Assert.False(errors.ContainsKey("dueDate"));
    }

    [Fact]
    public void Validate_DueDateInTheFuture_ReturnsNoDueDateError()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var futureDueDate = DateOnly.FromDateTime(now.UtcDateTime).AddDays(1);

        var errors = MakeValidator(now).Validate(new CreateTaskRequest("Title", null, null, futureDueDate));

        Assert.False(errors.ContainsKey("dueDate"));
    }

    [Fact]
    public void Validate_NoDueDate_ReturnsNoDueDateError()
    {
        var errors = MakeValidator().Validate(new CreateTaskRequest("Title", null, null, null));

        Assert.False(errors.ContainsKey("dueDate"));
    }

    [Fact]
    public void Validate_AllFieldsValid_ReturnsNoErrors()
    {
        var errors = MakeValidator().Validate(
            new CreateTaskRequest("Title", "Description", Priority.HIGH, null));

        Assert.Empty(errors);
    }
}
