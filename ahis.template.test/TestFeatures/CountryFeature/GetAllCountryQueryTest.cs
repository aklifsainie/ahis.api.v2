using System.Linq.Expressions;
using ahis.template.application.Features.CountryFeatures.Query;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace ahis.template.test.TestFeatures.CountryFeature
{
    public class GetAllCountryQueryTest
    {
        [Fact]
        public async Task Handle_WhenActiveCountriesExist_ReturnsCountriesSuccessfully()
        {
            // Arrange
            var mockRepository = new Mock<ICountryRepository>();
            var mockLogger =
                new Mock<ILogger<GetAllCountryQueryHandler>>();
            var mockAuditLogger = CreateAuditLoggerMock();

            var countries = new List<Country>
            {
                new()
                {
                    Id = 2,
                    CountryFullname = "Singapore",
                    CountryShortname = "Singapore",
                    CountryDescription = "Republic of Singapore",
                    CountryCode2 = "SG",
                    CountryCode3 = "SGP",
                    CountryIsoCode = "702",
                    IsActive = true,
                    IsDelete = false
                },
                new()
                {
                    Id = 1,
                    CountryFullname = "Malaysia",
                    CountryShortname = "Malaysia",
                    CountryDescription = "Malaysia Boleh",
                    CountryCode2 = "MY",
                    CountryCode3 = "MYS",
                    CountryIsoCode = "458",
                    IsActive = true,
                    IsDelete = false
                },
                new()
                {
                    Id = 3,
                    CountryFullname = "Inactive Country",
                    CountryShortname = "Inactive",
                    CountryCode2 = "IC",
                    CountryCode3 = "ICT",
                    CountryIsoCode = "999",
                    IsActive = false,
                    IsDelete = false
                }
            };

            mockRepository
                .Setup(repository => repository.GetAsync(
                    It.IsAny<Expression<Func<Country, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((
                    Expression<Func<Country, bool>> predicate,
                    bool asNoTracking,
                    CancellationToken cancellationToken) =>
                {
                    return countries
                        .Where(country => !country.IsDelete)
                        .Where(predicate.Compile())
                        .ToList();
                });

            var handler = new GetAllCountryQueryHandler(
                mockRepository.Object,
                mockLogger.Object,
                mockAuditLogger.Object);

            // Act
            var result = await handler.Handle(
                new GetAllCountryQuery(),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);

            // Handler should order countries by fullname.
            Assert.Collection(
                result.Value,
                malaysia =>
                {
                    Assert.Equal(1, malaysia.CountryId);
                    Assert.Equal("Malaysia", malaysia.CountryFullname);
                    Assert.Equal("MY", malaysia.CountryCode2);
                    Assert.Equal("MYS", malaysia.CountryCode3);
                    Assert.Equal("458", malaysia.CountryIsoCode);
                },
                singapore =>
                {
                    Assert.Equal(2, singapore.CountryId);
                    Assert.Equal("Singapore", singapore.CountryFullname);
                    Assert.Equal("SG", singapore.CountryCode2);
                    Assert.Equal("SGP", singapore.CountryCode3);
                    Assert.Equal("702", singapore.CountryIsoCode);
                });

            mockRepository.Verify(
                repository => repository.GetAsync(
                    It.IsAny<Expression<Func<Country, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            mockAuditLogger.Verify(
                auditLogger => auditLogger.LogAsync(
                    AuditActionEnum.View,
                    nameof(Country),
                    "LIST",
                    It.Is<string?>(metadata =>
                        metadata != null &&
                        metadata.Contains("\"operation\":\"GetAll\"") &&
                        metadata.Contains("\"resultCount\":2")),
                    null,
                    null,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenNoActiveCountriesExist_ReturnsEmptySuccessfulResult()
        {
            // Arrange
            var mockRepository = new Mock<ICountryRepository>();
            var mockLogger =
                new Mock<ILogger<GetAllCountryQueryHandler>>();
            var mockAuditLogger = CreateAuditLoggerMock();

            mockRepository
                .Setup(repository => repository.GetAsync(
                    It.IsAny<Expression<Func<Country, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Country>());

            var handler = new GetAllCountryQueryHandler(
                mockRepository.Object,
                mockLogger.Object,
                mockAuditLogger.Object);

            // Act
            var result = await handler.Handle(
                new GetAllCountryQuery(),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);

            Assert.Contains(
                result.Successes,
                success =>
                    success.Message ==
                    "No active countries were found.");

            mockRepository.Verify(
                repository => repository.GetAsync(
                    It.IsAny<Expression<Func<Country, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            mockAuditLogger.Verify(
                auditLogger => auditLogger.LogAsync(
                    AuditActionEnum.View,
                    nameof(Country),
                    "LIST",
                    It.Is<string?>(metadata =>
                        metadata != null &&
                        metadata.Contains("\"operation\":\"GetAll\"") &&
                        metadata.Contains("\"resultCount\":0")),
                    null,
                    null,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static Mock<IAuditLogger> CreateAuditLoggerMock()
        {
            var mockAuditLogger = new Mock<IAuditLogger>();

            mockAuditLogger
                .Setup(auditLogger => auditLogger.LogAsync(
                    It.IsAny<AuditActionEnum>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mockAuditLogger;
        }
    }
}