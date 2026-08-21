using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.domain.Models.Entities;
using ahis.template.domain.Models.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using ahis.template.application.Shared.Errors;
using ahis.template.domain.SharedKernel;
using ahis.template.domain.Models.ViewModels.CountryVM;

namespace ahis.template.application.Features.CountryFeatures.Command
{
    public class AddCountryCommand : IRequest<Result<CountryVM>>
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string CountryFullname { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CountryShortname { get; set; } = null!;

        [StringLength(1000)]
        public string? CountryDescription { get; set; } = null;

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode2 { get; set; } = null!;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CountryCode3 { get; set; } = null!;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CountryIsoCode { get; set; } = null!;
    }

    public class AddCountryCommandHandler : IRequestHandler<AddCountryCommand, Result<CountryVM>>
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

            // Basic validation using data annotations
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(command, serviceProvider: null, items: null);
            if (!Validator.TryValidateObject(command, validationContext, validationResults, validateAllProperties: true))
            {
                var error = new ValidationError("Validation failed for AddCountryCommand.");
                foreach (var vr in validationResults)
                {
                    var memberName = vr.MemberNames.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(memberName))
                        memberName = "Model"; // fallback key so ModelState contains visible entry

                    error.WithMetadata(memberName, vr.ErrorMessage ?? "Invalid value");
                }

                _logger.LogWarning("Validation failed while handling AddCountryCommand: {Errors}", string.Join("; ", validationResults.Select(x => x.ErrorMessage)));
                return Result.Fail<CountryVM>(error);
            }

            // Normalize inputs
            command.CountryFullname = command.CountryFullname.Trim();
            command.CountryShortname = command.CountryShortname.Trim();
            command.CountryCode2 = command.CountryCode2.Trim().ToUpperInvariant();
            command.CountryCode3 = command.CountryCode3.Trim().ToUpperInvariant();
            command.CountryIsoCode = command.CountryIsoCode.Trim().ToUpperInvariant();

            // Business validation: uniqueness - check for existing country and attach metadata for conflicted fields
            var existing = await _countryRepository.FirstOrDefaultAsync(c =>
                c.CountryFullname == command.CountryFullname ||
                c.CountryCode2 == command.CountryCode2 ||
                c.CountryCode3 == command.CountryCode3 ||
                c.CountryIsoCode == command.CountryIsoCode,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (existing != null)
            {
                var error = new ValidationError("Country with same name or code already exists.");

                if (!string.IsNullOrWhiteSpace(existing.CountryFullname) && string.Equals(existing.CountryFullname.Trim(), command.CountryFullname, StringComparison.OrdinalIgnoreCase))
                    error.WithMetadata(nameof(command.CountryFullname), "Country fullname already exists.");

                if (!string.IsNullOrWhiteSpace(existing.CountryCode2) && string.Equals(existing.CountryCode2.Trim(), command.CountryCode2, StringComparison.OrdinalIgnoreCase))
                    error.WithMetadata(nameof(command.CountryCode2), "Country code (2) already exists.");

                if (!string.IsNullOrWhiteSpace(existing.CountryCode3) && string.Equals(existing.CountryCode3.Trim(), command.CountryCode3, StringComparison.OrdinalIgnoreCase))
                    error.WithMetadata(nameof(command.CountryCode3), "Country code (3) already exists.");

                if (!string.IsNullOrWhiteSpace(existing.CountryIsoCode) && string.Equals(existing.CountryIsoCode.Trim(), command.CountryIsoCode, StringComparison.OrdinalIgnoreCase))
                    error.WithMetadata(nameof(command.CountryIsoCode), "Country ISO code already exists.");

                _logger.LogWarning("Attempt to add duplicate country '{CountryFullname}' (Id: {Id}).", command.CountryFullname, existing.Id);

                return Result.Fail<CountryVM>(error);
            }

            // Map AddCountryCommand (DTO) into Country entity
            Country countryEntity = new Country
            {
                CountryFullname = command.CountryFullname,
                CountryShortname = command.CountryShortname,
                CountryDescription = command.CountryDescription,
                CountryCode2 = command.CountryCode2,
                CountryCode3 = command.CountryCode3,
                CountryIsoCode = command.CountryIsoCode
            };

            await _countryRepository.AddAsync(countryEntity, cancellationToken);

            // Persist via UnitOfWork so caller can coordinate multiple repos if needed
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Country '{CountryFullname}' added successfully (Id: {Id}).", countryEntity.CountryFullname, countryEntity.Id);

            var countryVM = new CountryVM
            {
                CountryFullname = countryEntity.CountryFullname,
                CountryShortname = countryEntity.CountryShortname,
                CountryDescription = countryEntity.CountryDescription,
                CountryCode2 = countryEntity.CountryCode2,
                CountryCode3 = countryEntity.CountryCode3,
                CountryIsoCode = countryEntity.CountryIsoCode
            };

            return Result.Ok(countryVM).WithSuccess("Country has been added successfully.");

        }
    }
}
