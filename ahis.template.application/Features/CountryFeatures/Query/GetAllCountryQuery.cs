using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.domain.Models.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using ahis.template.domain.Models.ViewModels.CountryVM;

namespace ahis.template.application.Features.CountryFeatures.Query
{
    public class GetAllCountryQuery : IRequest<Result<List<CountryVM>>>
    {
    }

    public class GetAllCountryQueryHandler : IRequestHandler<GetAllCountryQuery, Result<List<CountryVM>>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly ILogger<GetAllCountryQueryHandler> _logger;

        public GetAllCountryQueryHandler(ICountryRepository countryRepository, ILogger<GetAllCountryQueryHandler> logger)
        {
            _countryRepository = countryRepository;
            _logger = logger;
        }

        public async Task<Result<List<CountryVM>>> Handle(GetAllCountryQuery query, CancellationToken cancellationToken)
        {

            _logger.LogInformation("Handling GetAllCountryQueryHandler");

            // Get data from repository
            var countryEntity = await _countryRepository.GetAllAsync(true, cancellationToken);

            // Manual map entity to view model
            var countries = countryEntity
                .Select(c => new CountryVM
                {
                    CountryFullname = c.CountryFullname,
                    CountryShortname = c.CountryShortname,
                    CountryDescription = c.CountryDescription,
                    CountryCode2 = c.CountryCode2,
                    CountryCode3 = c.CountryCode3,
                    CountryIsoCode = c.CountryIsoCode
                }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} countries", countries.Count);

            var result = Result.Ok(countries);

            if (countries.Count == 0)
            {
                result.WithSuccess("No country data found.");
            }

            return result;


        }
    }
}
