using NUnit.Framework;
using QualiFrame.Mocking.Contracts;

namespace QualiFrame.Mocking.Fixtures;

/// <summary>
/// NUnit fixture for contract validation tests: provides validator and spec loader.
/// </summary>
public class ContractValidationFixture
{
    public ContractValidator Validator { get; } = new ContractValidator();
    public OpenApiSpecLoader SpecLoader { get; } = new OpenApiSpecLoader();
    public SchemaValidationService SchemaValidation { get; } = new SchemaValidationService();
}
