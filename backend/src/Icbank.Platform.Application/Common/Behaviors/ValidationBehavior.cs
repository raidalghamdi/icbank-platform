using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Icbank.Platform.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behaviour that runs every registered <see cref="IValidator{T}"/> for the
/// incoming request before the handler executes (R-BE-034). A single request DTO can never reach
/// its handler unvalidated, because registration happens once for the whole pipeline rather than
/// per-controller.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.</summary>
    /// <param name="validators">All validators registered for <typeparamref name="TRequest"/>.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>Validates <paramref name="request"/> and either throws or forwards to <paramref name="next"/>.</summary>
    /// <param name="request">The incoming MediatR request.</param>
    /// <param name="next">The delegate that invokes the next behaviour or the handler itself.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The handler's response when validation passes.</returns>
    /// <exception cref="ValidationException">Thrown when one or more validators report a failure;
    /// caught by <c>GlobalExceptionMiddleware</c> (R-BE-051) and rendered as Problem Details.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        ValidationContext<TRequest> context = new(request);
        ValidationResult[] results = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
