using FluentValidation;
using MediatR;

namespace ClinicFlow.Api.Common.Behaviors;

/// <summary>
/// Runs every registered FluentValidation validator for a command/query
/// before it reaches its handler. Without this, validators exist in DI
/// but are never actually invoked — this is what makes RuleFor(...) rules
/// genuinely enforced across every feature, in one place, automatically.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                var message = string.Join(" | ", failures.Select(f => f.ErrorMessage));
                throw new ArgumentException(message);
            }
        }

        return await next();
    }
}