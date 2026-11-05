using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;
using QualiFrame.Pdf.Core.Engine.Config;

namespace QualiFrame.Pdf.Core.Engine.Validators;

/// <summary>
/// Font validation: embedded fonts, font families, sizes, styles, subset fonts, missing fonts.
/// </summary>
public sealed class PdfFontValidator : IPdfValidator
{
    public string Id => "Font";
    public string Name => "PDF Font";

    public Task<ValidatorResult> ValidateAsync(PdfValidationContext request, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var findings = new List<Finding>();
        var model = request.BaseModel;
        if (model == null)
        {
            sw.Stop();
            return Task.FromResult(new ValidatorResult
            {
                ValidatorId = Id,
                ValidatorName = Name,
                Status = ValidationStatus.Skipped,
                Message = "No parsed document model available.",
                DurationMs = sw.ElapsedMilliseconds
            });
        }

        var opts = request.Profile.GetValidatorOptions(Id) as FontValidationOptionsConfig;
        var allowedFamilies = opts?.AllowedFontFamilies != null
            ? new HashSet<string>(opts.AllowedFontFamilies, StringComparer.OrdinalIgnoreCase)
            : null;
        var disallowSubset = opts?.DisallowSubsetFonts ?? false;
        var failOnMissing = opts?.FailOnMissingFonts ?? true;
        var minSize = opts?.MinFontSize;
        var maxSize = opts?.MaxFontSize;

        foreach (var page in model.Pages)
        {
            foreach (var block in page.Blocks)
            {
                foreach (var line in block.Lines)
                {
                    foreach (var span in line.Spans)
                    {
                        if (!string.IsNullOrEmpty(span.FontName))
                        {
                            if (allowedFamilies != null && !allowedFamilies.Contains(span.FontName))
                            {
                                findings.Add(new Finding
                                {
                                    Severity = FindingSeverity.Error,
                                    Code = "FONT_NOT_ALLOWED",
                                    Message = $"Font '{span.FontName}' is not in allowed list.",
                                    PageNumber = page.PageNumber,
                                    BoundingBox = span.Bounds,
                                    Actual = span.FontName
                                });
                            }
                            if (disallowSubset && span.SubsetFont)
                            {
                                findings.Add(new Finding
                                {
                                    Severity = FindingSeverity.Warning,
                                    Code = "FONT_SUBSET",
                                    Message = $"Subset font used: {span.FontName}.",
                                    PageNumber = page.PageNumber,
                                    BoundingBox = span.Bounds
                                });
                            }
                        }
                        else if (failOnMissing)
                        {
                            findings.Add(new Finding
                            {
                                Severity = FindingSeverity.Warning,
                                Code = "FONT_MISSING",
                                Message = "Font name not available for span.",
                                PageNumber = page.PageNumber,
                                BoundingBox = span.Bounds
                            });
                        }

                        if (span.FontSize.HasValue)
                        {
                            if (minSize.HasValue && span.FontSize.Value < minSize.Value)
                            {
                                findings.Add(new Finding
                                {
                                    Severity = FindingSeverity.Warning,
                                    Code = "FONT_SIZE_MIN",
                                    Message = $"Font size {span.FontSize} is below minimum {minSize}.",
                                    PageNumber = page.PageNumber,
                                    Actual = span.FontSize.Value.ToString("F1"),
                                    Expected = $">= {minSize}"
                                });
                            }
                            if (maxSize.HasValue && span.FontSize.Value > maxSize.Value)
                            {
                                findings.Add(new Finding
                                {
                                    Severity = FindingSeverity.Warning,
                                    Code = "FONT_SIZE_MAX",
                                    Message = $"Font size {span.FontSize} exceeds maximum {maxSize}.",
                                    PageNumber = page.PageNumber,
                                    Actual = span.FontSize.Value.ToString("F1"),
                                    Expected = $"<= {maxSize}"
                                });
                            }
                        }
                    }
                }
            }
        }

        sw.Stop();
        var status = findings.Any(f => f.Severity == FindingSeverity.Error) ? ValidationStatus.Fail
            : findings.Any(f => f.Severity == FindingSeverity.Warning) ? ValidationStatus.Warning
            : ValidationStatus.Pass;

        return Task.FromResult(new ValidatorResult
        {
            ValidatorId = Id,
            ValidatorName = Name,
            Status = status,
            Findings = findings,
            DurationMs = sw.ElapsedMilliseconds
        });
    }
}
