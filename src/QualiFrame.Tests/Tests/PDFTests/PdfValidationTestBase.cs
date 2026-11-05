using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using QualiFrame.Framework.Core.DependencyInjection;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Framework.Reporting;
using QualiFrame.Pdf.Core.Engine.Integration;
using QualiFrame.Tests.Validators.Pdf;

namespace QualiFrame.Tests.Tests.PDFTests;

/// <summary>
/// Base for PDF validation tests. Registers QualiFrame + PDF services and provides reporting + validator.
/// Thread-safe for parallel runs; each test gets its own service scope.
/// </summary>
[TestFixture]
[Allure.NUnit.AllureNUnit]
public abstract class PdfValidationTestBase
{
    private IServiceProvider? _serviceProvider;

    protected IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("SetUp has not run.");
    protected IReportingService Reporting => (ServiceProvider.GetService(typeof(IReportingService)) as IReportingService)!;
    protected IPdfTextValidator PdfValidator => (ServiceProvider.GetService(typeof(IPdfTextValidator)) as IPdfTextValidator)!;
    protected IPdfTextExtractor PdfTextExtractor => (ServiceProvider.GetService(typeof(IPdfTextExtractor)) as IPdfTextExtractor)!;

    [SetUp]
    public virtual void SetUp()
    {
        QualiFrame.Framework.Utilities.Logging.Logger.Initialize();
        QualiFrame.Framework.Utilities.Logging.Logger.SetTestId(NUnit.Framework.TestContext.CurrentContext.Test.Name);

        var services = new ServiceCollection();
        services.AddQualiFrameServices();
        services.AddQualiFramePdfServices();
        services.AddTransient<IPdfTextExtractor, PdfTextExtractor>();
        services.AddTransient<IPdfTextValidator, PdfTextValidator>();
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public virtual void TearDown()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();
        _serviceProvider = null;
    }

    /// <summary>
    /// Logs a step and reports it via QF reporting and Allure (no Console.WriteLine).
    /// </summary>
    protected void LogStep(string stepName, string? detail = null)
    {
        var msg = string.IsNullOrEmpty(detail) ? stepName : $"{stepName}: {detail}";

        // Allure structured step
        var allure = EnhancedAllureReporter.GetInstance();
        allure.StartStep(stepName);

        try
        {
            Reporting.AddLog(LogLevel.Info, msg);
            QualiFrame.Framework.Utilities.Logging.Logger.Information("{Step}", msg);
            allure.CompleteStep(true);
        }
        catch (Exception ex)
        {
            allure.CompleteStep(false, ex);
            throw;
        }
    }

    /// <summary>
    /// Attaches PDF path and optional text preview to the report via QF abstraction.
    /// </summary>
    protected void AttachPdfArtifact(string pdfPath, string? textPreview = null, int maxPreviewChars = 2000)
    {
        Reporting.AddLog(LogLevel.Info, $"PDF under test: {pdfPath}");
        if (!string.IsNullOrEmpty(textPreview))
        {
            var preview = textPreview.Length <= maxPreviewChars ? textPreview : textPreview.AsSpan(0, maxPreviewChars).ToString() + "...";
            Reporting.AddLog(LogLevel.Info, $"Extracted text preview ({preview.Length} chars): {preview}");
        }
    }

    /// <summary>
    /// Wraps NUnit assertions in an Allure step so assertion success/failure is visible in the report.
    /// </summary>
    protected void AssertWithAllure(string stepName, Action assertion)
    {
        var allure = EnhancedAllureReporter.GetInstance();
        allure.StartStep(stepName);

        try
        {
            assertion();
            allure.CompleteStep(true);
        }
        catch (AssertionException ex)
        {
            Reporting.AddLog(LogLevel.Error, $"{stepName} FAILED: {ex.Message}");
            allure.CompleteStep(false, ex);
            throw;
        }
        catch (Exception ex)
        {
            allure.CompleteStep(false, ex);
            throw;
        }
    }
}
