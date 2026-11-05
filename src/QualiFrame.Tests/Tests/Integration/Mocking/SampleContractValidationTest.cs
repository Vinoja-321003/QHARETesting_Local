using NUnit.Framework;
using QualiFrame.Mocking.Contracts;

namespace QualiFrame.Tests.Integration.Mocking;

[TestFixture]
[Category("Mocking")]
public class SampleContractValidationTest
{
    [Test]
    public void ValidateMockResponse_AgainstSwagger_Succeeds()
    {
        var validator = new ContractValidator();
        var specPath = ResolveContractPath("swagger.json");
        Assume.That(File.Exists(specPath), $"Spec not found: {specPath}");

        var responseBody = @"{""id"":1,""name"":""Jane"",""email"":""jane@example.com"",""active"":true}";
        var result = validator.ValidateResponseAgainstSpec(specPath, "/api/users/1", "GET", 200, responseBody);

        Assert.That(result.IsValid, Is.True, result.Message);
    }

    [Test]
    public void ValidateMockResponse_MissingRequiredField_Fails()
    {
        var validator = new ContractValidator();
        var specPath = ResolveContractPath("swagger.json");
        Assume.That(File.Exists(specPath));

        var responseBody = @"{""id"":1}";
        var result = validator.ValidateResponseAgainstSpec(specPath, "/api/users/1", "GET", 200, responseBody);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Message, Does.Contain("Required").Or.Contain("missing"));
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
