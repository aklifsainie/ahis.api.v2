using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.Entities;
using ahis.template.domain.Models.ViewModels.CountryVM;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ahis.template.application.Features.CountryFeatures.Query
{
    public sealed record GetCountryByIdQuery(int Id): IRequest<Result<CountryVM>>;

    public sealed class GetCountryByIdQueryHandler: IRequestHandler<GetCountryByIdQuery, Result<CountryVM>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<GetCountryByIdQueryHandler> _logger;
        private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

        public GetCountryByIdQueryHandler(ICountryRepository countryRepository, IAuditLogger auditLogger, ILogger<GetCountryByIdQueryHandler> logger)
        {
            _countryRepository = countryRepository;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Result<CountryVM>> Handle(GetCountryByIdQuery query, CancellationToken cancellationToken)
        {
            if (query.Id <= 0)
            {
                var validationError = new ValidationError("Validation failed for GetCountryByIdQuery.");
                validationError.WithMetadata(nameof(query.Id), "Country ID must be greater than zero.");
                return Result.Fail<CountryVM>(validationError);
            }

            _logger.LogDebug("Retrieving country with ID {CountryId}", query.Id);

            var country = await _countryRepository.FirstOrDefaultAsync(country => country.Id == query.Id && country.IsActive, asNoTracking: true, cancellationToken: cancellationToken);

            if (country is null)
            {
                _logger.LogInformation("Active country with ID {CountryId} was not found", query.Id);
                return Result.Fail<CountryVM>(new EntityNotFoundError(nameof(Country), query.Id));
            }

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

            var auditMetadata = JsonSerializer.Serialize(new { Operation = "GetById", RetrievedData = countryViewModel }, AuditJsonOptions);

            await _auditLogger.LogAsync(
                action: AuditActionEnum.View,
                entityName: nameof(Country),
                entityId: country.Id.ToString(),
                metadata: auditMetadata,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Country with ID {CountryId} retrieved successfully", country.Id);

            return Result.Ok(countryViewModel);
        }
    }
}