using NUnit.Framework;
using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Server;

namespace QualiFrame.Tests.Integration.Mocking;

[TestFixture]
[Category("Mocking")]
public class SampleSwaggerDrivenMockTest
{
    [Test]
    public void GenerateMocksFromSpec_RegistersStubs_AndServesResponses()
    {
        var manager = new WireMockServerManager(new QualiFrame.Mocking.Config.MockServerConfig { Port = 9092 });
        manager.Start();

        try
        {
            var specPath = ResolveContractPath("swagger.json");
            Assert.That(File.Exists(specPath), $"Spec file not found: {specPath}");

            var generator = new OpenApiMockGenerator();
            var count = generator.GenerateMocksFromSpec(specPath, manager.RegisterStub,
                includePaths: new[] { "/api/users", "/api/orders" });

            Assert.That(count, Is.GreaterThanOrEqualTo(1));

            using var client = new HttpClient { BaseAddress = new Uri(manager.BaseUrl) };

            var getUsers = client.GetAsync("/api/users/1").GetAwaiter().GetResult();
            Assert.That(getUsers.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            var body = getUsers.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.That(body, Does.Contain("id").Or.Contain("name").Or.Contain("email"));

            var postOrder = client.PostAsync("/api/orders", new StringContent("{}", System.Text.Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            Assert.That(postOrder.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created).Or.EqualTo(System.Net.HttpStatusCode.OK));
        }
        finally
        {
            manager.Stop();
        }
    }

    private static string ResolveContractPath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Contracts", fileName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "QualiFrame.Mocking", "Contracts", fileName)
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
