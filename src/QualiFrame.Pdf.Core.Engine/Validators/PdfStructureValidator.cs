using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;
using QualiFrame.Pdf.Core.Engine.Config;

namespace QualiFrame.Pdf.Core.Engine.Validators;

/// <summary>
/// Structural parsing validation: document tree (pages → blocks → lines → spans), tables, images, metadata.
/// </summary>
public sealed class PdfStructureValidator : IPdfValidator
{
    public string Id => "Structure";
    public string Name => "PDF Structure";

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

        var opts = request.Profile.GetValidatorOptions(Id) as StructureValidationOptionsConfig;

        if (model.PageCount == 0)
        {
            findings.Add(new Finding
            {
                Severity = FindingSeverity.Error,
                Code = "STRUCTURE_EMPTY",
                Message = "Document has no pages."
            });
        }

        if (opts?.RequirePageOrder == true && model.Pages.Count > 0)
        {
            for (var i = 0; i < model.Pages.Count; i++)
            {
                if (model.Pages[i].PageNumber != i + 1)
                {
                    findings.Add(new Finding
                    {
                        Severity = FindingSeverity.Warning,
                        Code = "STRUCTURE_PAGE_ORDER",
                        Message = $"Page order unexpected at index {i + 1}.",
                        PageNumber = model.Pages[i].PageNumber
                    });
                }
            }
        }

        if (opts?.MinBlocksPerPage is int minBlocks and > 0)
        {
            foreach (var page in model.Pages)
            {
                var blockCount = page.Blocks.Count;
                if (blockCount < minBlocks)
                {
                    findings.Add(new Finding
                    {
                        Severity = FindingSeverity.Warning,
                        Code = "STRUCTURE_MIN_BLOCKS",
                        Message = $"Page {page.PageNumber} has {blockCount} block(s), minimum expected {minBlocks}.",
                        PageNumber = page.PageNumber
                    });
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
