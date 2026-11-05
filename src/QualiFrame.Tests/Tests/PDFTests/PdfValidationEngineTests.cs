using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using QualiFrame.Framework.Core.DependencyInjection;
using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Config;
using QualiFrame.Pdf.Core.Engine.Engine;
using QualiFrame.Pdf.Core.Engine.Integration;
using QualiFrame.Tests.Utilities;

namespace QualiFrame.Tests.Tests.PDFTests;

/// <summary>
/// Minimal NUnit test for the extensible PDF Validation Engine: load profile, validate BasePDF vs ComparePDF, produce report + artifacts.
/// </summary>
[TestFixture]
[Category("PDF")]
[Allure.NUnit.Attributes.AllureSuite("PDF Validation Engine")]
[Allure.NUnit.Attributes.AllureFeature("Extensible PDF Validation")]
public class PdfValidationEngineTests
{
    private IServiceProvider _serviceProvider = null!;
    private string _profilePath = null!;
    private string _basePdfPath = null!;
    private string _comparePdfPath = null!;
    private string _artifactsPath = null!;

    [SetUp]
    public void SetUp()
    {
        QualiFrame.Framework.Utilities.Logging.Logger.Initialize();
        QualiFrame.Framework.Utilities.Logging.Logger.SetTestId(TestContext.CurrentContext.Test.Name);

        var services = new ServiceCollection();
        services.AddQualiFrameServices();
        services.AddQualiFramePdfValidationEngine();
        _serviceProvider = services.BuildServiceProvider();

        _profilePath = TestPathResolver.ResolveTestPath("Configuration/pdf.validation.json");
        _basePdfPath = TestPathResolver.ResolveTestPath("Data/FileValidation/BasePDF.pdf");
        _comparePdfPath = TestPathResolver.ResolveTestPath("Data/FileValidation/ComparePDF.pdf");
        _artifactsPath = Path.Combine(
            TestPathResolver.GetProjectRoot(),
            "src", "QualiFrame.Tests", "TestResults", "PdfValidationEngine",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();
    }

    [Test]
    [Allure.NUnit.Attributes.AllureStory("Profile-based validation")]
    [Allure.NUnit.Attributes.AllureDescription("Load pdf.validation.json, validate BasePDF.pdf vs ComparePDF.pdf, produce report and artifacts.")]
    public async Task Validate_LoadProfile_BaseVsCompare_ProducesReportAndArtifacts()
    {
        Assert.That(File.Exists(_profilePath), $"Profile not found: {_profilePath}");
        if (!File.Exists(_basePdfPath))
        {
            Assert.Ignore($"Base PDF not found: {_basePdfPath}. Add Data/FileValidation/BasePDF.pdf to run.");
        }
        if (!File.Exists(_comparePdfPath))
        {
            Assert.Ignore($"Compare PDF not found: {_comparePdfPath}. Add Data/FileValidation/ComparePDF.pdf to run.");
        }

        var profile = PdfValidationProfileLoader.LoadFromFile(_profilePath);
        var engine = _serviceProvider.GetRequiredService<PdfValidationEngine>();
        var reporter = _serviceProvider.GetRequiredService<IValidationReporter>();

        var report = await engine.ValidateAsync(
            _basePdfPath,
            profile,
            candidatePdfPath: _comparePdfPath,
            artifactsOutputPath: _artifactsPath,
            CancellationToken.None).ConfigureAwait(false);

        await reporter.ReportAsync(report, _artifactsPath, CancellationToken.None).ConfigureAwait(false);

        Assert.That(report, Is.Not.Null);
        Assert.That(report.ProfileName, Is.EqualTo(profile.ProfileName));
        Assert.That(report.BasePdfPath, Is.EqualTo(_basePdfPath));
        Assert.That(report.CandidatePdfPath, Is.EqualTo(_comparePdfPath));
        Assert.That(report.ValidatorResults, Is.Not.Empty);
        Assert.That(File.Exists(Path.Combine(_artifactsPath, "report.json")), "report.json artifact should exist");
    }
}
