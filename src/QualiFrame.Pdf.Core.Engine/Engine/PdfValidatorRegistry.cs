using QualiFrame.Pdf.Core.Engine.Abstractions;

namespace QualiFrame.Pdf.Core.Engine.Engine;

/// <summary>
/// Registry of plugin validators. Order is deterministic (registration order). No static shared state.
/// </summary>
public sealed class PdfValidatorRegistry : IPdfValidatorRegistry
{
    private readonly List<IPdfValidator> _validators = new();
    private static readonly string[] DefaultOrder = { "Comparison", "Structure", "Font", "Signature" };

    public PdfValidatorRegistry() { }

    /// <summary>
    /// Register a validator. Duplicate ids overwrite previous registration (last wins).
    /// </summary>
    public PdfValidatorRegistry Register(IPdfValidator validator)
    {
        if (validator == null) throw new ArgumentNullException(nameof(validator));
        var existing = _validators.FindIndex(v => string.Equals(v.Id, validator.Id, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
            _validators[existing] = validator;
        else
            _validators.Add(validator);
        return this;
    }

    public IReadOnlyList<IPdfValidator> GetAll()
    {
        return SortByDefaultOrder(_validators.ToList());
    }

    public IPdfValidator? GetById(string id)
    {
        return _validators.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IPdfValidator> GetEnabledForProfile(IValidationProfile profile)
    {
        var toggles = profile.ValidatorToggles;
        var enabled = _validators
            .Where(v => toggles.TryGetValue(v.Id, out var on) && on)
            .ToList();
        return SortByDefaultOrder(enabled);
    }

    private static IReadOnlyList<IPdfValidator> SortByDefaultOrder(List<IPdfValidator> list)
    {
        var order = DefaultOrder.ToList();
        list.Sort((a, b) =>
        {
            var ia = order.IndexOf(a.Id);
            var ib = order.IndexOf(b.Id);
            if (ia < 0) ia = order.Count;
            if (ib < 0) ib = order.Count;
            return ia.CompareTo(ib);
        });
        return list;
    }
}
