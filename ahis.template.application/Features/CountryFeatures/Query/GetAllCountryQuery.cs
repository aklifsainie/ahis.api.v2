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
            var categoryEntity = await _countryRepository.GetAllAsync();

            // If no data found
            if (categoryEntity == null || !categoryEntity.Any())
            {
                _logger.LogWarning("No countries found.");

                // Return success with empty list but informative message
                return Result.Ok(new List<CountryVM>()).WithSuccess("No country data found.");
            }

            // Manual map entity to view model
            List<CountryVM> categoryVM = categoryEntity
                .Select(c => new CountryVM
                {
                    CountryFullname = c.CountryFullname,
                    CountryShortname = c.CountryShortname,
                    CountryDescription = c.CountryDescription,
                    CountryCode2 = c.CountryCode2,
                    CountryCode3 = c.CountryCode3
                })
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} countries", categoryVM.Count);

            return Result.Ok(categoryVM);


        }
    }
}
