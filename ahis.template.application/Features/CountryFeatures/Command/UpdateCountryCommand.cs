using System.ComponentModel.DataAnnotations;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.Entities;
using ahis.template.domain.Models.ViewModels.CountryVM;
using ahis.template.domain.SharedKernel;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.CountryFeatures.Command
{
    public sealed class UpdateCountryCommand: IRequest<Result<CountryVM>>
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string CountryFullname { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CountryShortname { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? CountryDescription { get; set; }

        [Required]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Country code 2 must contain exactly two letters.")]
        public string CountryCode2 { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Country code 3 must contain exactly three letters.")]
        public string CountryCode3 { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "Country ISO code must contain exactly three digits.")]
        public string CountryIsoCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public sealed class UpdateCountryCommandHandler: IRequestHandler<UpdateCountryCommand, Result<CountryVM>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateCountryCommandHandler> _logger;

        public UpdateCountryCommandHandler(ICountryRepository countryRepository, IUnitOfWork unitOfWork, ILogger<UpdateCountryCommandHandler> logger)
        {
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<CountryVM>> Handle(UpdateCountryCommand command, CancellationToken cancellationToken)
        {
            var normalizedCommand = Normalize(command);
            var validationError = Validate(normalizedCommand);

            if (validationError is not null)
            {
                return Result.Fail<CountryVM>(validationError);
            }

            // Tracking must remain enabled because this entity will be updated.
            var country = await _countryRepository.GetByIdAsync(normalizedCommand.Id, asNoTracking: false, cancellationToken: cancellationToken);

            if (country is null)
            {
                return Result.Fail<CountryVM>(new EntityNotFoundError(nameof(Country), normalizedCommand.Id));
            }

            var conflictingCountries = await _countryRepository.GetAsync(existingCountry =>
                        existingCountry.Id != normalizedCommand.Id &&
                        (
                            existingCountry.CountryFullname == normalizedCommand.CountryFullname ||
                            existingCountry.CountryCode2 == normalizedCommand.CountryCode2 ||
                            existingCountry.CountryCode3 == normalizedCommand.CountryCode3 ||
                            existingCountry.CountryIsoCode == normalizedCommand.CountryIsoCode
                        ),
                    asNoTracking: true,
                    cancellationToken: cancellationToken);

            var duplicateError = BuildDuplicateError(normalizedCommand, conflictingCountries);

            if (duplicateError is not null)
            {
                return Result.Fail<CountryVM>(duplicateError);
            }

            country.CountryFullname = normalizedCommand.CountryFullname;
            country.CountryShortname = normalizedCommand.CountryShortname;
            country.CountryDescription = normalizedCommand.CountryDescription;
            country.CountryCode2 = normalizedCommand.CountryCode2;
            country.CountryCode3 = normalizedCommand.CountryCode3;
            country.CountryIsoCode = normalizedCommand.CountryIsoCode;
            country.IsActive = normalizedCommand.IsActive;

            /*
             * Do not call _countryRepository.Update(country).
             *
             * The entity was loaded with tracking enabled. EF Core will detect
             * the changed properties and only update the required values.
             */

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Country {CountryId} updated successfully", country.Id);

            var countryViewModel = new CountryVM
            {
                CountryId = country.Id,
                CountryFullname = country.CountryFullname,
                CountryShortname = country.CountryShortname,
                CountryDescription = country.CountryDescription,
                CountryCode2 = country.CountryCode2,
                CountryCode3 = country.CountryCode3,
                CountryIsoCode = country.CountryIsoCode,
                IsActive = country.IsActive
            };

            return Result.Ok(countryViewModel).WithSuccess("Country has been updated successfully.");
        }

        private static UpdateCountryCommand Normalize(UpdateCountryCommand command)
        {
            return new UpdateCountryCommand
            {
                Id = command.Id,

                CountryFullname = command.CountryFullname?.Trim() ?? string.Empty,
                CountryShortname = command.CountryShortname?.Trim() ?? string.Empty,
                CountryDescription = string.IsNullOrWhiteSpace(command.CountryDescription) ? null : command.CountryDescription.Trim(),
                CountryCode2 = command.CountryCode2?.Trim().ToUpperInvariant()?? string.Empty,
                CountryCode3 =command.CountryCode3?.Trim().ToUpperInvariant()?? string.Empty,
                CountryIsoCode =command.CountryIsoCode?.Trim()?? string.Empty,
                IsActive = command.IsActive
            };
        }

        private static ValidationError? Validate(UpdateCountryCommand command)
        {
            var error = new ValidationError("One or more country fields are invalid.");

            if (command.Id <= 0)
            {
                error.WithMetadata(nameof(command.Id), "Country ID must be greater than zero.");
            }

            var validationResults = new List<ValidationResult>();

            var context = new ValidationContext(command);

            Validator.TryValidateObject(command, context, validationResults, validateAllProperties: true);

            foreach (var validationResult in validationResults)
            {
                var memberNames = validationResult.MemberNames.Any() ? validationResult.MemberNames : ["Model"];

                foreach (var memberName in memberNames)
                {
                    error.WithMetadata(memberName, validationResult.ErrorMessage ?? $"{memberName} is invalid.");
                }
            }

            return error.Metadata.Count == 0 ? null : error;
        }

        private static ValidationError? BuildDuplicateError(UpdateCountryCommand command, IReadOnlyList<Country> conflictingCountries)
        {
            if (conflictingCountries.Count == 0)
            {
                return null;
            }

            var error = new ValidationError("A country with the same name or code already exists.");

            if (conflictingCountries.Any(country => string.Equals(country.CountryFullname, command.CountryFullname, StringComparison.OrdinalIgnoreCase)))
            {
                error.WithMetadata(nameof(command.CountryFullname), "Country fullname already exists.");
            }

            if (conflictingCountries.Any(country => string.Equals(country.CountryCode2, command.CountryCode2, StringComparison.OrdinalIgnoreCase)))
            {
                error.WithMetadata(nameof(command.CountryCode2), "Country code 2 already exists.");
            }

            if (conflictingCountries.Any(country => string.Equals(country.CountryCode3, command.CountryCode3, StringComparison.OrdinalIgnoreCase)))
            {
                error.WithMetadata(nameof(command.CountryCode3), "Country code 3 already exists.");
            }

            if (conflictingCountries.Any(country => string.Equals(country.CountryIsoCode, command.CountryIsoCode, StringComparison.OrdinalIgnoreCase)))
            {
                error.WithMetadata(nameof(command.CountryIsoCode), "Country ISO code already exists.");
            }

            return error;
        }
    }
}