using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Contracts;
using QualiFrame.Mocking.Server;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.HybridTesting;

/// <summary>
/// Orchestrates hybrid flow: start mock server, register stubs, run test action, optionally validate contract.
/// </summary>
public class HybridTestOrchestrator
{
    private readonly MockServerLifecycle _lifecycle;
    private readonly ApiUiValidationService _validationService;
    private readonly EnvironmentToggleManager _toggle;
    private readonly LoggingHelper _log;

    public HybridTestOrchestrator(
        MockServerLifecycle? lifecycle = null,
        ApiUiValidationService? validationService = null,
        EnvironmentToggleManager? toggle = null,
        LoggingHelper? log = null)
    {
        _toggle = toggle ?? new EnvironmentToggleManager();
        var config = _toggle.GetMockServerConfig();
        _lifecycle = lifecycle ?? new MockServerLifecycle(config);
        _validationService = validationService ?? new ApiUiValidationService();
        _log = log ?? new LoggingHelper();
    }

    /// <summary>
    /// Executes a mocked UI flow: starts mock server, registers stubs, runs test action, optionally validates contract.
    /// </summary>
    public async Task ExecuteMockedUiFlowAsync(
        Action<WireMockServerManager> registerStubs,
        Func<string, Task> testAction,
        string? swaggerPath = null,
        string? validateContractPath = null,
        string? validateContractMethod = "GET",
        int? capturedResponseStatusCode = null,
        string? capturedResponseBody = null,
        CancellationToken cancellationToken = default)
    {
        if (!_toggle.IsMockingEnabled)
        {
            _log.Info("Mocking disabled, running test against real backend");
            var config = _toggle.GetMockServerConfig();
            await testAction(config.BaseUrl);
            return;
        }

        var baseUrl = await _lifecycle.StartAsync(
            generateFromOpenApi: !string.IsNullOrEmpty(swaggerPath),
            swaggerPath: swaggerPath,
            registerStaticStubs: registerStubs,
            cancellationToken: cancellationToken);

        try
        {
            await testAction(baseUrl);

            if (!string.IsNullOrEmpty(validateContractPath) && !string.IsNullOrEmpty(swaggerPath) && capturedResponseStatusCode.HasValue)
            {
                var result = _validationService.ValidateApiResponse(
                    swaggerPath, validateContractPath, validateContractMethod ?? "GET",
                    capturedResponseStatusCode.Value, capturedResponseBody);
                if (!result.IsValid)
                    throw new InvalidOperationException($"Contract validation failed: {result.Message}");
            }
        }
        finally
        {
            await _lifecycle.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Synchronous overload.
    /// </summary>
    public void ExecuteMockedUiFlow(
        Action<WireMockServerManager> registerStubs,
        Action<string> testAction,
        string? swaggerPath = null,
        string? validateContractPath = null,
        string? validateContractMethod = "GET",
        int? capturedResponseStatusCode = null,
        string? capturedResponseBody = null)
    {
        ExecuteMockedUiFlowAsync(
            registerStubs,
            url => { testAction(url); return Task.CompletedTask; },
            swaggerPath, validateContractPath, validateContractMethod,
            capturedResponseStatusCode, capturedResponseBody).GetAwaiter().GetResult();
    }
}
