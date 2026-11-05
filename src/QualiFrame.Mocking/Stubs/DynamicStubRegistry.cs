using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Server;

namespace QualiFrame.Mocking.Stubs;

/// <summary>
/// Central registry for stub definitions; applies them to a WireMock server manager.
/// Supports registering domain stub modules and one-off stubs.
/// </summary>
public class DynamicStubRegistry
{
    private readonly List<StubDefinition> _stubs = new();
    private readonly List<Action<WireMockServerManager>> _modules = new();

    /// <summary>
    /// Registers a single stub definition (to be applied when Apply is called).
    /// </summary>
    public DynamicStubRegistry Register(StubDefinition stub)
    {
        _stubs.Add(stub);
        return this;
    }

    /// <summary>
    /// Registers a module that will add stubs to the given manager when Apply is called.
    /// </summary>
    public DynamicStubRegistry RegisterModule(Action<WireMockServerManager> module)
    {
        _modules.Add(module);
        return this;
    }

    /// <summary>
    /// Applies all registered stubs and modules to the given manager.
    /// </summary>
    public void Apply(WireMockServerManager manager)
    {
        foreach (var s in _stubs)
            manager.RegisterStub(s);
        foreach (var mod in _modules)
            mod(manager);
    }

    /// <summary>
    /// Returns all registered stub definitions (for remote/Docker registration).
    /// </summary>
    public IReadOnlyList<StubDefinition> GetStubDefinitions()
    {
        return _stubs.ToList();
    }

    /// <summary>
    /// Clears all stubs and modules.
    /// </summary>
    public void Clear()
    {
        _stubs.Clear();
        _modules.Clear();
    }
}
