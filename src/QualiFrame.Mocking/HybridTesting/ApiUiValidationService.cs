using QualiFrame.Mocking.Contracts;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.HybridTesting;

/// <summary>
/// Validates API contract (OpenAPI) in hybrid tests.
/// </summary>
public class ApiUiValidationService
{
    private readonly ContractValidator _contractValidator;
    private readonly LoggingHelper _log;

    public ApiUiValidationService(ContractValidator? contractValidator = null, LoggingHelper? log = null)
    {
        _contractValidator = contractValidator ?? new ContractValidator();
        _log = log ?? new LoggingHelper();
    }

    /// <summary>
    /// Validates that the API response conforms to the OpenAPI spec (status + body schema).
    /// </summary>
    public ContractValidationResult ValidateApiResponse(
        string specPath,
        string path,
        string method,
        int statusCode,
        string? responseBody)
    {
        var result = _contractValidator.ValidateResponseAgainstSpec(specPath, path, method, statusCode, responseBody);
        if (!result.IsValid)
            _log.Warn("Contract validation failed", ("Message", result.Message), ("Path", path), ("Method", method));
        return result;
    }

    /// <summary>
    /// Validates using an in-memory OpenAPI document.
    /// </summary>
    public ContractValidationResult ValidateApiResponse(
        Microsoft.OpenApi.Models.OpenApiDocument document,
        string path,
        string method,
        int statusCode,
        string? responseBody)
    {
        return _contractValidator.ValidateResponse(document, path, method, statusCode, responseBody);
    }
}
