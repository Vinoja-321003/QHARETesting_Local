using NUnit.Framework;
using QualiFrame.Mocking.HybridTesting;
using QualiFrame.Mocking.Server;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.Fixtures;

/// <summary>
/// NUnit fixture for hybrid API + UI tests: provides orchestrator and backend switcher.
/// </summary>
public class HybridTestFixture
{
    private HybridTestOrchestrator? _orchestrator;
    private BackendRouteSwitcher? _routeSwitcher;

    public HybridTestOrchestrator Orchestrator => _orchestrator ??= new HybridTestOrchestrator();
    public BackendRouteSwitcher RouteSwitcher => _routeSwitcher ??= new BackendRouteSwitcher();

    /// <summary>
    /// Runs a hybrid flow with the given stubs and test action.
    /// </summary>
    public void ExecuteMockedFlow(Action<WireMockServerManager> registerStubs, Action<string> testAction, string? swaggerPath = null)
    {
        Orchestrator.ExecuteMockedUiFlow(registerStubs, testAction, swaggerPath);
    }

    [TearDown]
    public void TearDown()
    {
        _routeSwitcher?.ResetAsync().GetAwaiter().GetResult();
    }
}
