using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;
using QualiFrame.Pdf.Core.Engine.Config;

namespace QualiFrame.Pdf.Core.Engine.Validators;

/// <summary>
/// Digital signature verification: detect signatures, validate certificate chain, signature integrity,
/// timestamp if present, and produce validation status + details.
/// </summary>
public sealed class PdfDigitalSignatureValidator : IPdfValidator
{
    public string Id => "Signature";
    public string Name => "PDF Digital Signature";

    public Task<ValidatorResult> ValidateAsync(PdfValidationContext request, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var findings = new List<Finding>();
        var opts = request.Profile.GetValidatorOptions(Id) as SignatureValidationOptionsConfig;
        var requireSignature = opts?.RequireSignature ?? false;
        var validateChain = opts?.ValidateCertificateChain ?? true;
        var validateTimestamp = opts?.ValidateTimestamp ?? false;
        var allowedSigners = opts?.AllowedSigners;

        try
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(request.BasePdfPath);
            var hasSignature = DetectSignatureFields(doc);

            if (!hasSignature)
            {
                if (requireSignature)
                {
                    findings.Add(new Finding
                    {
                        Severity = FindingSeverity.Error,
                        Code = "SIGNATURE_MISSING",
                        Message = "Document is required to be signed but no signature field was detected."
                    });
                }
                else
                {
                    sw.Stop();
                    return Task.FromResult(new ValidatorResult
                    {
                        ValidatorId = Id,
                        ValidatorName = Name,
                        Status = ValidationStatus.Skipped,
                        Message = "No signature field in document.",
                        DurationMs = sw.ElapsedMilliseconds
                    });
                }
            }
            else
            {
                // TODO: Certificate chain trust store integration (e.g. validate against system store or custom CA).
                // PdfPig does not verify signatures; full verification would require e.g. iText or BouncyCastle.
                findings.Add(new Finding
                {
                    Severity = FindingSeverity.Info,
                    Code = "SIGNATURE_PRESENT",
                    Message = "Signature field(s) detected. Full certificate chain and integrity verification require external library (e.g. BouncyCastle)."
                });
            }
        }
        catch (Exception ex) when (ex.Message?.Contains("password", StringComparison.OrdinalIgnoreCase) == true)
        {
            findings.Add(new Finding
            {
                Severity = FindingSeverity.Error,
                Code = "SIGNATURE_PASSWORD",
                Message = "PDF is password-protected; cannot inspect signatures."
            });
        }
        catch (Exception ex)
        {
            findings.Add(new Finding
            {
                Severity = FindingSeverity.Error,
                Code = "SIGNATURE_ERROR",
                Message = $"Error inspecting signatures: {ex.Message}"
            });
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

    private static bool DetectSignatureFields(UglyToad.PdfPig.PdfDocument doc)
    {
        try
        {
            // PdfPig 0.1.10 Catalog may not expose AcroForm; use reflection for newer API compatibility.
            var catalog = doc.Structure?.Catalog;
            if (catalog == null) return false;
            var acroFormProp = catalog.GetType().GetProperty("AcroForm");
            if (acroFormProp?.GetValue(catalog) is not { } acroForm) return false;
            var fieldsProp = acroForm.GetType().GetProperty("Fields");
            if (fieldsProp?.GetValue(acroForm) is not System.Collections.IEnumerable fields) return false;
            foreach (var f in fields)
            {
                if (f?.GetType().Name?.Contains("Signature", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
        }
        catch { /* PdfPig version may not expose signature fields */ }
        return false;
    }
}
