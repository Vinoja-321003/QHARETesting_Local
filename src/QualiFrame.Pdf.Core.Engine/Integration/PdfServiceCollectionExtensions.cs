using Microsoft.Extensions.DependencyInjection;
using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;
using QualiFrame.Pdf.Core.Engine.Engine;
using QualiFrame.Pdf.Core.Engine.Integration;
using QualiFrame.Pdf.Core.Engine.Parsing;
using QualiFrame.Pdf.Core.Engine.Reporting;
using QualiFrame.Pdf.Core.Engine.Reporting.Impl;
using QualiFrame.Pdf.Core.Engine.Validators;

namespace QualiFrame.Pdf.Core.Engine.Integration;

/// <summary>
/// Registers PDF module services with QualiFrame DI. Call when PdfAutomation is enabled.
/// </summary>
public static class PdfServiceCollectionExtensions
{
    /// <summary>
    /// Adds PDF automation services. All services are Transient/Scoped for thread-safe parallel execution.
    /// </summary>
    public static IServiceCollection AddQualiFramePdfServices(this IServiceCollection services)
    {
        services.AddTransient<IPdfExtractor, PdfExtractor>();
        services.AddTransient<IPdfComparator>(sp => new PdfComparator(sp.GetRequiredService<IPdfExtractor>()));
        services.AddTransient<IPdfDiffReportGenerator, PdfDiffReportGenerator>();
        services.AddTransient<Core.IPdfValidator, PdfValidator>();
        services.AddTransient<IPdfDownloadCapture>(_ => new PdfDownloadCapture(500, 60));
        services.AddTransient<IPdfReportingBridge, PdfReportingBridge>();
        return services;
    }

    /// <summary>
    /// Adds the extensible PDF Validation Engine (plugin validators, profile-based config, reporting).
    /// Requires <see cref="AddQualiFramePdfServices"/> and Framework <see cref="QualiFrame.Framework.Core.Services.IReportingService"/>.
    /// </summary>
    public static IServiceCollection AddQualiFramePdfValidationEngine(this IServiceCollection services)
    {
        services.AddQualiFramePdfServices();
        services.AddTransient<IPdfParser, PdfParser>();
        services.AddTransient<Abstractions.IPdfComparer>(sp => new PdfComparerAdapter(sp.GetRequiredService<IPdfComparator>()));
        services.AddSingleton<IPdfValidatorRegistry>(sp =>
        {
            var registry = new PdfValidatorRegistry();
            registry.Register(sp.GetRequiredService<PdfAdvancedComparisonValidator>());
            registry.Register(sp.GetRequiredService<PdfStructureValidator>());
            registry.Register(sp.GetRequiredService<PdfFontValidator>());
            registry.Register(sp.GetRequiredService<PdfDigitalSignatureValidator>());
            return registry;
        });
        services.AddTransient<PdfAdvancedComparisonValidator>();
        services.AddTransient<PdfStructureValidator>();
        services.AddTransient<PdfFontValidator>();
        services.AddTransient<PdfDigitalSignatureValidator>();
        services.AddTransient<PdfValidationEngine>();
        services.AddTransient<IValidationReporter, PdfValidationReporter>();
        return services;
    }
}

