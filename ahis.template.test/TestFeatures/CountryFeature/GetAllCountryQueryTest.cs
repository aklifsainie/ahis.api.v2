using ahis.template.application.Features.CountryFeatures.Query;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.domain.Models.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ahis.template.test.TestFeatures.CountryFeature
{
    public class GetAllCountryQueryTest
    {

        [Fact]
        public async Task Handle_ResturnSuccessResponse()
        {
            // Arrange
            var mockRepo = new Mock<ICountryRepository>();
            var mockLogger = new Mock<ILogger<GetAllCountryQueryHandler>>();

            mockRepo.Setup(repo => repo.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Country>
                {
                    new Country
                    {
                        Id = 1,
                        CountryFullname = "Malaysia",
                        CountryCode2 = "MY",
                        CountryCode3 = "MYS",
                        CountryDescription = "Malaysia Boleh",
                        CountryShortname = "MAS"
                    }
                });

            var handler = new GetAllCountryQueryHandler(mockRepo.Object, mockLogger.Object);

            // Act
            var result = await handler.Handle(new GetAllCountryQuery(), CancellationToken.None);

            // Assert - FluentResults API
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("Malaysia", result.Value[0].CountryFullname);
        }

        [Fact]
        public async Task Handle_ResturnSuccessResponse_NoData()
        {
            // Arrange
            var mockRepo = new Mock<ICountryRepository>();
            var mockLogger = new Mock<ILogger<GetAllCountryQueryHandler>>();

            mockRepo.Setup(repo => repo.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Country>());

            var handler = new GetAllCountryQueryHandler(mockRepo.Object, mockLogger.Object);

            // Act
            var result = await handler.Handle(new GetAllCountryQuery(), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
            Assert.Equal("No country data found.", result.Successes.First().Message);
        }
    }
}
