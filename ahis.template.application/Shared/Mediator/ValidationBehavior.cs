using FluentValidation;
using FluentResults;
using ahis.template.application.Shared.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace ahis.template.application.Shared.Mediator
{
    public class ValidationBehavior<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly IServiceProvider _serviceProvider;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, IServiceProvider serviceProvider)
        {
            _validators = validators;
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new FluentValidation.ValidationContext<TRequest>(request);
                var failures = new List<FluentValidation.Results.ValidationFailure>();

                foreach (var validator in _validators)
                {
                    var result = await validator.ValidateAsync(context, cancellationToken);
                    if (!result.IsValid)
                        failures.AddRange(result.Errors);
                }

                if (failures.Any())
                {
                    var error = new ValidationError("Validation failed.");
                    foreach (var f in failures)
                    {
                        error.WithMetadata(f.PropertyName ?? "Model", f.ErrorMessage);
                    }

                    var resultType = typeof(TResponse);

                    if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var valueType = resultType.GetGenericArguments()[0];
                        var failMethod = typeof(Result).GetMethod("Fail", new[] { typeof(IError) })!.MakeGenericMethod(valueType);
                        var resultFail = failMethod.Invoke(null, new object[] { error });
                        return (TResponse)resultFail!;
                    }

                    throw new InvalidOperationException("ValidationBehavior only supports handlers that return FluentResults.Result<T>.");
                }
            }

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            return await handler.Handle((dynamic)request, cancellationToken);
        }
    }
}
