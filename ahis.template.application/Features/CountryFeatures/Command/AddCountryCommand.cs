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
    public sealed class AddCountryCommand: IRequest<Result<CountryVM>>
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string CountryFullname { get; init; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CountryShortname { get; init; } = string.Empty;

        [StringLength(1000)]
        public string? CountryDescription { get; init; }

        [Required]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Country code 2 must contain exactly two letters.")]
        public string CountryCode2 { get; init; } = string.Empty;

        [Required]
        [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Country code 3 must contain exactly three letters.")]
        public string CountryCode3 { get; init; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "Country ISO code must contain exactly three digits.")]
        public string CountryIsoCode { get; init; } = string.Empty;
    }

    public sealed class AddCountryCommandHandler : IRequestHandler<AddCountryCommand, Result<CountryVM>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddCountryCommandHandler> _logger;

        public AddCountryCommandHandler(ICountryRepository countryRepository, IUnitOfWork unitOfWork, ILogger<AddCountryCommandHandler> logger)
        {
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<CountryVM>> Handle(AddCountryCommand command, CancellationToken cancellationToken)
        {
            var normalizedCommand = Normalize(command);
            var validationError = Validate(normalizedCommand);

            if (validationError is not null)
            {
                _logger.LogInformation("Validation failed while adding a country");

                return Result.Fail<CountryVM>(validationError);
            }

            var conflictingCountries = await _countryRepository.GetAsync(country =>
                        country.CountryFullname == normalizedCommand.CountryFullname ||
                        country.CountryCode2 == normalizedCommand.CountryCode2 ||
                        country.CountryCode3 == normalizedCommand.CountryCode3 ||
                        country.CountryIsoCode == normalizedCommand.CountryIsoCode,
                        asNoTracking: true,
                        cancellationToken: cancellationToken);

            var duplicateError = BuildDuplicateError(normalizedCommand, conflictingCountries);

            if (duplicateError is not null)
            {
                _logger.LogInformation("Country creation rejected because one or more unique values already exist");

                return Result.Fail<CountryVM>(duplicateError);
            }

            var country = new Country
            {
                CountryFullname = normalizedCommand.CountryFullname,
                CountryShortname = normalizedCommand.CountryShortname,
                CountryDescription = normalizedCommand.CountryDescription,
                CountryCode2 = normalizedCommand.CountryCode2,
                CountryCode3 = normalizedCommand.CountryCode3,
                CountryIsoCode = normalizedCommand.CountryIsoCode
            };

            await _countryRepository.AddAsync(country, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var countryViewModel = new CountryVM
            {
                CountryId = country.Id,
                CountryFullname = country.CountryFullname,
                CountryShortname = country.CountryShortname,
                CountryDescription = country.CountryDescription,
                CountryCode2 = country.CountryCode2,
                CountryCode3 = country.CountryCode3,
                CountryIsoCode = country.CountryIsoCode
            };

            _logger.LogInformation("Country {CountryId} created successfully", country.Id);

            return Result.Ok(countryViewModel).WithSuccess("Country has been added successfully.");
        }

        private static AddCountryCommand Normalize(AddCountryCommand command)
        {
            return new AddCountryCommand
            {
                CountryFullname = command.CountryFullname.Trim(),
                CountryShortname = command.CountryShortname.Trim(),
                CountryDescription = string.IsNullOrWhiteSpace(command.CountryDescription) ? null : command.CountryDescription.Trim(),
                CountryCode2 = command.CountryCode2.Trim().ToUpperInvariant(),
                CountryCode3 = command.CountryCode3.Trim().ToUpperInvariant(),
                CountryIsoCode = command.CountryIsoCode.Trim()
            };
        }

        private static ValidationError? Validate(AddCountryCommand command)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(command);
            var isValid = Validator.TryValidateObject(command, validationContext, validationResults, validateAllProperties: true);

            if (isValid)
            {
                return null;
            }

            var error = new ValidationError("One or more country fields are invalid.");

            foreach (var validationResult in validationResults)
            {
                var memberNames = validationResult.MemberNames.Any() ? validationResult.MemberNames : ["Model"];

                foreach (var memberName in memberNames)
                {
                    error.WithMetadata(memberName, validationResult.ErrorMessage ?? $"{memberName} is invalid.");
                }
            }

            return error;
        }

        private static ValidationError? BuildDuplicateError(AddCountryCommand command, IReadOnlyList<Country> conflictingCountries)
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