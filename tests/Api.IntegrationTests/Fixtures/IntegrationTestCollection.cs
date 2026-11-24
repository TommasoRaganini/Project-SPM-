using Xunit;

namespace Api.IntegrationTests.Fixtures;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}

