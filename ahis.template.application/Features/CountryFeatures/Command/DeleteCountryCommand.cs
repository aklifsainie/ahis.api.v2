using ahis.template.application.Interfaces.Repositories;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.Entities;
using ahis.template.domain.SharedKernel;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.CountryFeatures.Command
{
    public sealed record DeleteCountryCommand(int Id) : IRequest<Result<bool>>;

    public sealed class DeleteCountryCommandHandler : IRequestHandler<DeleteCountryCommand, Result<bool>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCountryCommandHandler> _logger;

        public DeleteCountryCommandHandler(ICountryRepository countryRepository, IUnitOfWork unitOfWork, ILogger<DeleteCountryCommandHandler> logger)
        {
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(DeleteCountryCommand command, CancellationToken cancellationToken)
        {
            if (command.Id <= 0)
            {
                var validationError = new ValidationError("The supplied country ID is invalid.");
                validationError.WithMetadata(nameof(command.Id), "Country ID must be greater than zero.");

                return Result.Fail<bool>(validationError);
            }

            var country = await _countryRepository.GetByIdAsync(command.Id, asNoTracking: false, cancellationToken: cancellationToken);

            if (country is null)
            {
                return Result.Fail<bool>(new EntityNotFoundError(nameof(Country), command.Id));
            }

            country.IsDelete = true;
            country.IsActive = false;

            /*
             * The entity is tracked, so calling Update() or SoftDelete()
             * is unnecessary. SaveChanges will detect these two changes.
             */

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Country {CountryId} soft-deleted successfully", country.Id);

            return Result.Ok(true).WithSuccess("Country has been deleted successfully.");
        }
    }
}