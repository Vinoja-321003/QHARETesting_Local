using System.Diagnostics;
using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Engine;

/// <summary>
/// Pipeline orchestrator: runs validators in deterministic order with rule/profile selection,
/// feature toggles, parallel-safe execution (no static shared state), and cancellation support.
/// </summary>
public sealed class PdfValidationEngine
{
    private readonly IPdfValidatorRegistry _registry;
    private readonly IPdfParser _parser;
    private readonly IPdfComparer? _comparer;

    public PdfValidationEngine(
        IPdfValidatorRegistry registry,
        IPdfParser parser,
        IPdfComparer? comparer = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _comparer = comparer;
    }

    /// <summary>
    /// Run validation: load profile, parse PDF(s), run enabled validators in order, aggregate report.
    /// </summary>
    public async Task<PdfValidationReport> ValidateAsync(
        string basePdfPath,
        IValidationProfile profile,
        string? candidatePdfPath = null,
        string? artifactsOutputPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(basePdfPath))
            throw new FileNotFoundException("Base PDF not found.", basePdfPath);
        if (candidatePdfPath != null && !File.Exists(candidatePdfPath))
            throw new FileNotFoundException("Candidate PDF not found.", candidatePdfPath);

        var parseOptions = ResolveParseOptions(profile);
        var compareOptions = ResolveCompareOptions(profile);

        PdfDocumentModel? baseModel = null;
        PdfDocumentModel? candidateModel = null;

        var validators = _registry.GetEnabledForProfile(profile);
        var needsBaseModel = validators.Any(v => v.Id != "Comparison" || candidatePdfPath == null);
        if (needsBaseModel)
        {
            baseModel = await _parser.ParseAsync(basePdfPath, parseOptions, cancellationToken).ConfigureAwait(false);
        }
        if (!string.IsNullOrEmpty(candidatePdfPath))
        {
            candidateModel = await _parser.ParseAsync(candidatePdfPath, parseOptions, cancellationToken).ConfigureAwait(false);
        }

        var context = new PdfValidationContext
        {
            BasePdfPath = basePdfPath,
            CandidatePdfPath = candidatePdfPath,
            BaseModel = baseModel,
            CandidateModel = candidateModel,
            Profile = profile,
            ParseOptions = parseOptions,
            CompareOptions = compareOptions
        };

        var results = new List<ValidatorResult>();
        var totalSw = Stopwatch.StartNew();

        foreach (var validator in validators)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            ValidatorResult runResult;
            try
            {
                runResult = await validator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                runResult = new ValidatorResult
                {
                    ValidatorId = validator.Id,
                    ValidatorName = validator.Name,
                    Status = ValidationStatus.Fail,
                    Message = ex.Message,
                    Findings = new[]
                    {
                        new Finding
                        {
                            Severity = FindingSeverity.Error,
                            Code = "VALIDATOR_ERROR",
                            Message = ex.Message
                        }
                    },
                    DurationMs = (long)sw.ElapsedMilliseconds
                };
            }
            sw.Stop();
            results.Add(new ValidatorResult
            {
                ValidatorId = runResult.ValidatorId,
                ValidatorName = runResult.ValidatorName,
                Status = runResult.Status,
                Findings = runResult.Findings,
                DurationMs = sw.ElapsedMilliseconds,
                Message = runResult.Message
            });
        }

        totalSw.Stop();
        var allFindings = results.SelectMany(r => r.Findings).ToList();
        var errorCount = allFindings.Count(f => f.Severity == FindingSeverity.Error);
        var warningCount = allFindings.Count(f => f.Severity == FindingSeverity.Warning);
        var overallStatus = errorCount > 0 ? ValidationStatus.Fail
            : warningCount > 0 ? ValidationStatus.Warning
            : ValidationStatus.Pass;

        var report = new PdfValidationReport
        {
            ProfileName = profile.ProfileName,
            BasePdfPath = basePdfPath,
            CandidatePdfPath = candidatePdfPath,
            RunUtc = DateTime.UtcNow,
            TotalDurationMs = totalSw.ElapsedMilliseconds,
            OverallStatus = overallStatus,
            TotalFindings = allFindings.Count,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            ValidatorResults = results,
            AllFindings = allFindings
        };

        return report;
    }

    private static PdfParseOptions ResolveParseOptions(IValidationProfile profile)
    {
        var opts = profile.GetValidatorOptions("Parsing") as Config.ParsingOptionsConfig;
        if (opts == null)
            return new PdfParseOptions();
        return new PdfParseOptions
        {
            IncludeStructure = opts.IncludeStructure,
            IncludeTables = opts.IncludeTables,
            IncludeImages = opts.IncludeImages,
            IncludeFontInfo = opts.IncludeFontInfo,
            IncludeMetadata = true,
            MaxPages = opts.MaxPages
        };
    }

    private static PdfCompareOptions ResolveCompareOptions(IValidationProfile profile)
    {
        var opts = profile.GetValidatorOptions("Comparison") as Config.ComparisonOptionsConfig;
        if (opts == null)
            return new PdfCompareOptions();
        return new PdfCompareOptions
        {
            TextTolerance = opts.TextTolerance,
            VisualSimilarityThreshold = opts.VisualSimilarityThreshold,
            LayoutTolerancePoints = opts.LayoutTolerancePoints,
            CompareText = opts.CompareText,
            CompareLayout = opts.CompareLayout,
            CompareVisual = opts.CompareVisual
        };
    }
}
