using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using FluentValidation;
using FluentResults;
using ahis.template.application.Shared.Errors;

namespace ahis.template.application.Shared.Mediator
{
    public class SimpleMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public SimpleMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            // Run FluentValidation validators if any
            var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
            var enumerableValidatorType = typeof(IEnumerable<>).MakeGenericType(validatorType);
            var validatorsObj = _serviceProvider.GetService(enumerableValidatorType) as IEnumerable;

            if (validatorsObj != null)
            {
                var failures = new List<FluentValidation.Results.ValidationFailure>();

                foreach (var validator in validatorsObj)
                {
                    // call ValidateAsync dynamically
                    dynamic dynValidator = validator!;
                    var validationResult = await dynValidator.ValidateAsync((dynamic)request, cancellationToken);
                    if (!validationResult.IsValid)
                        failures.AddRange(validationResult.Errors);
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

                    throw new InvalidOperationException("SimpleMediator validation only supports handlers that return FluentResults.Result<T>.");
                }
            }

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            return await handler.Handle((dynamic)request, cancellationToken);
        }
    }
}
