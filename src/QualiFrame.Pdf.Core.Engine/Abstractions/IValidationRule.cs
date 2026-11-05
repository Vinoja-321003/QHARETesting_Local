using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Configurable rule that can be applied within a validation profile (e.g. allowed fonts, signature requirements).
/// </summary>
public interface IValidationRule
{
    string RuleId { get; }
    string Description { get; }
    bool IsEnabled { get; }
}
