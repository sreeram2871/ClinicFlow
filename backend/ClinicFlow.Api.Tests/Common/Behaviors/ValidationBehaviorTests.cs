using ClinicFlow.Api.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Common.Behaviors;

// A minimal fake command + validator, just for testing the behavior itself
// in isolation — not tied to any real feature.
public record FakeCommand(string Name) : IRequest<string>;

public class FakeCommandValidator : AbstractValidator<FakeCommand>
{
    public FakeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class ValidationBehaviorTests
{
    [Test]
    public async Task Handle_WithValidCommand_CallsNextAndReturnsResult()
    {
        var validators = new List<IValidator<FakeCommand>> { new FakeCommandValidator() };
        var behavior = new ValidationBehavior<FakeCommand, string>(validators);

        var wasNextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            wasNextCalled = true;
            return Task.FromResult("success");
        }

        var result = await behavior.Handle(new FakeCommand("Valid Name"), Next, CancellationToken.None);

        Assert.That(wasNextCalled, Is.True);
        Assert.That(result, Is.EqualTo("success"));
    }

    [Test]
    public void Handle_WithInvalidCommand_ThrowsArgumentExceptionAndNeverCallsNext()
    {
        var validators = new List<IValidator<FakeCommand>> { new FakeCommandValidator() };
        var behavior = new ValidationBehavior<FakeCommand, string>(validators);

        var wasNextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            wasNextCalled = true;
            return Task.FromResult("should never get here");
        }

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await behavior.Handle(new FakeCommand(""), Next, CancellationToken.None));

        Assert.That(wasNextCalled, Is.False);
    }
}