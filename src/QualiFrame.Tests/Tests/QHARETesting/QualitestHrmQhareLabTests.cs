using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Qhare.Selenium;
using Qhare.Selenium.NUnit;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.WebDriver;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QualiFrame.Tests.Tests.Integration
{
    [TestFixture]
    [NonParallelizable]
    [QhareReport]
    [Allure.NUnit.AllureNUnit]
    [Allure.NUnit.Attributes.AllureFeature("Qualitest HRM QHARE Certification Lab")]
    public class QualitestHrmQhareLabTests : IntegrationTestBase
    {
        private const string LabUrl = "https://qualitest-hrm-ui-production.up.railway.app/qhare-lab";
        private static readonly By LabRoot = By.CssSelector("section.qhare-lab");
        private static readonly By AuthResult = By.Id("qhare-auth-result");
        private static readonly By UsernameInput = By.CssSelector("input[name='username']");
        private static readonly By PasswordInput = By.CssSelector("input[name='password']");
        private static readonly By BaselineLoginButton = By.Id("loginBtn");
        private QhareHealingScope? _qhareScope;
        [SuppressMessage(
            "NUnit.Analyzers",
            "NUnit1032",
            Justification = "The QHARE NUnit action emits the final result after per-test teardown.")]
        private Qhare.Selenium.QhareClient? _reportingClient;
        private bool _scenarioCreated;
        private IWebDriver NativeDriver =>
            Driver is QhareDriverWrapper qhareDriver ? qhareDriver.WrappedDriver : Driver;

        public enum ExpectedBehavior
        {
            NativeLogin,
            HealedLogin,
            NoLogin,
            RequiredLogin
        }

        public sealed record LabCase(
            string LinkLabel,
            string Href,
            string ExpectedUseCase,
            string? ExpectedVersion,
            ExpectedBehavior ExpectedBehavior,
            bool UsesFrames = false,
            bool RerenderBeforeClick = false,
            bool CrossOriginFrame = false);

        public static IEnumerable<TestCaseData> LabCases
        {
            get
            {
                var cases = new[]
                {
                    new LabCase("Baseline", "/qhare-lab", "baseline", null, ExpectedBehavior.NativeLogin),
                    new LabCase("UC-HL-001 Attribute change", "/qhare-lab?uc=UC-HL-001", "UC-HL-001", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-002 Text change", "/qhare-lab?uc=UC-HL-002", "UC-HL-002", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-003 Class change", "/qhare-lab?uc=UC-HL-003", "UC-HL-003", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-004 DOM structure", "/qhare-lab?uc=UC-HL-004", "UC-HL-004", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-EXT-SD-001 Open shadow", "/qhare-lab?uc=UC-EXT-SD-001", "UC-EXT-SD-001", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-EXT-SD-002 Closed shadow", "/qhare-lab?uc=UC-EXT-SD-002", "UC-EXT-SD-002", null, ExpectedBehavior.NoLogin),
                    new LabCase("UC-EXT-SD-003 Nested shadow", "/qhare-lab?uc=UC-EXT-SD-003", "UC-EXT-SD-003", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-005 History v1", "/qhare-lab?uc=UC-HL-005&version=1", "UC-HL-005", "1", ExpectedBehavior.NativeLogin),
                    new LabCase("UC-HL-005 History v2", "/qhare-lab?uc=UC-HL-005&version=2", "UC-HL-005", "2", ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-005 History v3", "/qhare-lab?uc=UC-HL-005&version=3", "UC-HL-005", "3", ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-006 Similar controls", "/qhare-lab?uc=UC-HL-006", "UC-HL-006", null, ExpectedBehavior.HealedLogin),
                    new LabCase("UC-HL-007 Duplicate controls", "/qhare-lab?uc=UC-HL-007", "UC-HL-007", null, ExpectedBehavior.NoLogin),
                    new LabCase("UC-HL-008 Different control", "/qhare-lab?uc=UC-HL-008", "UC-HL-008", null, ExpectedBehavior.NoLogin),
                    new LabCase("UC-EXT-ST-001 Stale recovery", "/qhare-lab?uc=UC-EXT-ST-001", "UC-EXT-ST-001", null, ExpectedBehavior.NativeLogin, RerenderBeforeClick: true),
                    new LabCase("UC-EXT-RD-001 Dynamic remount", "/qhare-lab?uc=UC-EXT-RD-001", "UC-EXT-RD-001", null, ExpectedBehavior.NativeLogin, RerenderBeforeClick: true),
                    new LabCase("UC-EXT-IF-001 Frame choice", "/qhare-lab?uc=UC-EXT-IF-001", "UC-EXT-IF-001", null, ExpectedBehavior.HealedLogin, UsesFrames: true),
                    new LabCase("UC-EXT-IF-002 Single frame", "/qhare-lab?uc=UC-EXT-IF-002", "UC-EXT-IF-002", null, ExpectedBehavior.HealedLogin, UsesFrames: true),
                    new LabCase("UC-EXT-IF-003 Nested frames", "/qhare-lab?uc=UC-EXT-IF-003", "UC-EXT-IF-003", null, ExpectedBehavior.HealedLogin, UsesFrames: true),
                    new LabCase("UC-EXT-IF-004 Dynamic frame", "/qhare-lab?uc=UC-EXT-IF-004", "UC-EXT-IF-004", null, ExpectedBehavior.HealedLogin, UsesFrames: true),
                    new LabCase("UC-EXT-IF-005 Cross-origin frame", "/qhare-lab?uc=UC-EXT-IF-005", "UC-EXT-IF-005", null, ExpectedBehavior.HealedLogin, UsesFrames: true, CrossOriginFrame: true)
                };

                foreach (var testCase in cases)
                {
                    yield return new TestCaseData(testCase).SetName($"QhareLab_{testCase.LinkLabel}");
                }
            }
        }

        public static IEnumerable<TestCaseData> RequiredLoginCases =>
            LabCases.Where(testCase => ((LabCase)testCase.Arguments[0]!).ExpectedBehavior != ExpectedBehavior.NoLogin);

        public static IEnumerable<TestCaseData> NegativeSafetyCases =>
            LabCases.Where(testCase => ((LabCase)testCase.Arguments[0]!).ExpectedBehavior == ExpectedBehavior.NoLogin);

        public static IEnumerable<TestCaseData> RequiredFailureCases =>
            CreateUnresolvedCase(ExpectedBehavior.RequiredLogin, "Required action unresolved - FAILED");

        public static IEnumerable<TestCaseData> OptionalUnresolvedCases =>
            CreateUnresolvedCase(ExpectedBehavior.NoLogin, "Optional action absent");

        public static IEnumerable<TestCaseData> RequiredBlockedCases =>
            CreateUnresolvedCase(ExpectedBehavior.RequiredLogin, "Required action unresolved - BLOCKED");

        private static IEnumerable<TestCaseData> CreateUnresolvedCase(ExpectedBehavior behavior, string name)
        {
            var sourceCase = LabCases
                .Select(testCase => (LabCase)testCase.Arguments[0]!)
                .Single(testCase => testCase.ExpectedUseCase == "UC-HL-008");
            yield return new TestCaseData(sourceCase with { ExpectedBehavior = behavior })
                .SetName($"QhareLab_{name}");
        }

        [OneTimeSetUp]
        public void InitializeQhareReportingClient()
        {
            var configuration = TestConfiguration.Instance.GetQhareConfiguration();
            _reportingClient = new Qhare.Selenium.QhareClient(
                configuration.BaseUrl,
                configuration.ProjectId,
                TimeSpan.FromSeconds(Math.Max(1, configuration.TimeoutSeconds)),
                configuration.ApiKey);
        }

        [SetUp]
        public override void SetUp()
        {
            Assert.That(
                TestConfiguration.Instance.ActiveSelfHealingProvider,
                Is.EqualTo(SelfHealingProvider.Qhare),
                "QHARE must be the active self-healing provider for the certification lab tests.");

            _qhareScope = new QhareHealingScope
            {
                TestId = TestContext.CurrentContext.Test.FullName,
                Feature = "Qualitest HRM QHARE Certification Lab"
            };
            QhareHealingContext.Bind(_qhareScope);

            try
            {
                TestSessionRegistry.Register(
                    _reportingClient!,
                    _qhareScope.ExecutionId,
                    _qhareScope.TestId);
                RunLifecycle.Track(_reportingClient!, _qhareScope.ExecutionId);

                base.SetUp();
                Assert.That(_qhareScope.Client, Is.Not.Null, "The QHARE wrapper did not attach its client.");
                ExtentReportManager.CreateScenario(_qhareScope.Feature, TestContext.CurrentContext.Test.Name);
                _scenarioCreated = true;
                LogReportStep("Initialize QHARE-enabled browser session", () =>
                    ExtentReportManager.LogPass("WebDriver session initialized with QHARE self-healing enabled."));
            }
            catch
            {
                QhareHealingContext.Unbind();
                throw;
            }
        }

        [OneTimeTearDown]
        public void DisposeQhareReportingClient()
        {
            _reportingClient?.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                if (_scenarioCreated)
                {
                    var result = TestContext.CurrentContext.Result;
                    QhareExtentEvidence.AnnotateScenario(_qhareScope!.ScenarioHeals);
                    switch (result.Outcome.Status)
                    {
                        case TestStatus.Passed:
                            ExtentReportManager.MarkScenarioAsPassed(
                                _qhareScope?.ScenarioHeals.Count > 0
                                    ? $"QHARE lab case passed with {_qhareScope.ScenarioHeals.Count} validated heal(s)."
                                    : "QHARE lab case passed without a locator heal.");
                            break;
                        case TestStatus.Skipped:
                        case TestStatus.Inconclusive:
                            ExtentReportManager.MarkScenarioAsSkipped(result.Message ?? "QHARE lab case skipped.");
                            break;
                        default:
                            ExtentReportManager.LogFail(result.Message ?? "QHARE lab case failed.");
                            ExtentReportManager.MarkScenarioAsFailed("QHARE lab case failed.");
                            break;
                    }

                    ExtentReportManager.EnsureFlushed();
                }
            }
            finally
            {
                try
                {
                    base.TearDown();
                }
                finally
                {
                    QhareHealingContext.Unbind();
                }
            }
        }

        [TestCaseSource(nameof(RequiredLoginCases))]
        [QhareTestIntent("REQUIRED_ACTION")]
        public void RequiredLoginControl_UsesBaselineLocatorAndValidatesQhare(LabCase testCase)
        {
            RunLabCase(testCase);
        }

        [TestCaseSource(nameof(NegativeSafetyCases))]
        [QhareTestIntent("NEGATIVE_SAFETY_VALIDATION", ExpectedHealingDecision = "REJECTED")]
        public void UnsafeLoginCandidate_IsRejected(LabCase testCase)
        {
            RunLabCase(testCase);
        }

        [TestCaseSource(nameof(RequiredFailureCases))]
        [Category("QHAREUnresolvedStatus")]
        [QhareTestIntent("REQUIRED_ACTION")]
        public void RequiredActionWithUnresolvedLocator_Fails(LabCase testCase)
        {
            RunLabCase(testCase);
        }

        [TestCaseSource(nameof(RequiredBlockedCases))]
        [Category("QHAREUnresolvedStatus")]
        [QhareTestIntent(
            "REQUIRED_ACTION",
            BlockingReason = "No safe locator is available for the required HRM login action.")]
        public void RequiredActionWithUnresolvedLocator_IsBlocked(LabCase testCase)
        {
            RunLabCase(testCase);
        }

        [TestCaseSource(nameof(OptionalUnresolvedCases))]
        [Category("QHAREUnresolvedStatus")]
        [QhareTestIntent("OPTIONAL_ACTION", Optional = true)]
        public void OptionalActionWithUnresolvedLocator_MayBeOmitted(LabCase testCase)
        {
            RunLabCase(testCase);
        }

        private void RunLabCase(LabCase testCase)
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            Driver.Manage().Window.Maximize();

            LogReportStep($"Open {testCase.LinkLabel} from the lab navigation", () =>
            {
                Driver.Navigate().GoToUrl(LabUrl);
                var link = By.XPath(
                    $"//nav[@aria-label='Certification use cases']//a[normalize-space()='{testCase.LinkLabel}']");
                new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                    .Until(driver => driver.FindElement(link).Displayed);
                Driver.FindElement(link).Click();

                var expectedVersion = testCase.ExpectedVersion ?? string.Empty;
                new WebDriverWait(Driver, TimeSpan.FromSeconds(15)).Until(driver =>
                {
                    var labRoot = driver.FindElement(LabRoot);
                    var actualUseCase = labRoot.GetAttribute("data-qhare-uc");
                    var actualVersion = labRoot.GetAttribute("data-qhare-version") ?? string.Empty;
                    return string.Equals(actualUseCase, testCase.ExpectedUseCase, StringComparison.Ordinal)
                        && string.Equals(actualVersion, expectedVersion, StringComparison.Ordinal);
                });
            });

            LogReportStep($"Click Login using the baseline locator for {testCase.LinkLabel}", () =>
            {
                var locator = GetBaselineLocator(testCase);
                if (testCase.UsesFrames && !TrySwitchToCredentialFrame(testCase))
                {
                    return;
                }

                if (testCase.RerenderBeforeClick)
                {
                    WaitForBaselineControlToBeReplaced(locator);
                }

                var clicked = false;
                try
                {
                    Driver.FindElement(locator).Click();
                    clicked = true;
                }
                catch (NoSuchElementException)
                {
                    if (testCase.ExpectedBehavior != ExpectedBehavior.NoLogin)
                    {
                        throw;
                    }
                }

                var resultText = testCase.UsesFrames
                    ? NativeDriver.FindElement(AuthResult).Text
                    : Driver.FindElement(AuthResult).Text;
                Driver.SwitchTo().DefaultContent();

                switch (testCase.ExpectedBehavior)
                {
                    case ExpectedBehavior.NativeLogin:
                        Assert.That(clicked, Is.True, "The baseline Login locator should resolve without healing.");
                        Assert.That(_qhareScope!.StepHeals, Is.Empty, "This case should not invoke QHARE healing.");
                        Assert.That(resultText, Is.EqualTo("Signed in"));
                        break;
                    case ExpectedBehavior.HealedLogin:
                        Assert.That(clicked, Is.True, "QHARE should resolve the baseline locator to a clickable Login control.");
                        Assert.That(_qhareScope!.StepHeals, Is.Not.Empty, "Expected a validated QHARE locator heal.");
                        Assert.That(resultText, Is.EqualTo("Signed in"), "The healed control must perform the Login action.");
                        break;
                    case ExpectedBehavior.NoLogin:
                        Assert.That(resultText, Is.Not.EqualTo("Signed in"),
                            "QHARE must not guess when the control is ambiguous, unavailable, or outside policy.");
                        Assert.That(_qhareScope!.StepHeals, Is.Empty,
                            "This case must not record a validated heal to an unsafe or ambiguous control.");
                        break;
                    case ExpectedBehavior.RequiredLogin:
                        Assert.That(clicked, Is.True, "The required Login action should complete.");
                        Assert.That(resultText, Is.EqualTo("Signed in"),
                            "The required Login action was not completed with a safe locator.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(testCase.ExpectedBehavior));
                }
            });
        }

        private bool TrySwitchToCredentialFrame(LabCase testCase)
        {
            var timeout = testCase.CrossOriginFrame ? TimeSpan.FromSeconds(8) : TimeSpan.FromSeconds(20);
            var wait = new WebDriverWait(NativeDriver, timeout)
            {
                PollingInterval = TimeSpan.FromMilliseconds(250)
            };

            int[]? framePath;
            try
            {
                framePath = wait.Until(_ =>
                {
                    try
                    {
                        NativeDriver.SwitchTo().DefaultContent();
                        return FindCredentialFramePath(NativeDriver, depth: 0);
                    }
                    catch (WebDriverException)
                    {
                        return null;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                framePath = null;
            }

            if (framePath == null)
            {
                NativeDriver.SwitchTo().DefaultContent();
                if (testCase.CrossOriginFrame)
                {
                    Assert.Ignore(
                        "The cross-origin login frame did not load a credential form. The lab serves that frame from the alternate host (127.0.0.1 or localhost), which is not available from this lab URL.");
                }

                Assert.Fail("Could not find a frame containing the lab credential form.");
                return false;
            }

            foreach (var frameIndex in framePath)
            {
                Driver.SwitchTo().Frame(frameIndex);
            }

            return true;
        }

        private void WaitForBaselineControlToBeReplaced(By locator)
        {
            var original = NativeDriver.FindElement(locator);
            var initialGeneration = original.GetAttribute("data-qhare-generation");
            ((IJavaScriptExecutor)Driver).ExecuteScript("window.__qhareRerender()");

            new WebDriverWait(NativeDriver, TimeSpan.FromSeconds(10)).Until(_ =>
                IsStale(original) && ReplacementControlIsReady(locator, initialGeneration));
        }

        private static bool IsStale(IWebElement element)
        {
            try
            {
                _ = element.Enabled;
                return false;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
        }

        private bool ReplacementControlIsReady(By locator, string? initialGeneration)
        {
            var buttons = NativeDriver.FindElements(locator);
            if (buttons.Count == 0)
            {
                return false;
            }

            var button = buttons[0];
            return button.Displayed
                && !string.Equals(
                    initialGeneration,
                    button.GetAttribute("data-qhare-generation"),
                    StringComparison.Ordinal);
        }

        private static By GetBaselineLocator(LabCase testCase)
        {
            return testCase.ExpectedUseCase switch
            {
                "UC-HL-001" => By.Id("loginBtn"),
                "UC-HL-002" => By.XPath("//button[normalize-space(.)='Login']"),
                "UC-HL-003" => By.CssSelector("button.btn-login"),
                "UC-HL-004" => By.CssSelector("#loginForm > button#loginBtn"),
                "UC-HL-006" => By.Id("loginBtn"),
                "UC-HL-007" => By.Id("loginBtn"),
                "UC-HL-008" => By.Id("loginBtn"),
                "UC-EXT-ST-001" => By.Id("loginBtn"),
                "UC-EXT-RD-001" => By.Id("loginBtn"),
                _ => BaselineLoginButton
            };
        }

        private static int[]? FindCredentialFramePath(IWebDriver driver, int depth)
        {
            if (depth > 5)
            {
                return null;
            }

            if (driver.FindElements(UsernameInput).Count > 0
                && driver.FindElements(PasswordInput).Count > 0)
            {
                return Array.Empty<int>();
            }

            var frames = driver.FindElements(By.TagName("iframe"));
            for (var index = 0; index < frames.Count; index++)
            {
                try
                {
                    driver.SwitchTo().Frame(index);
                }
                catch (WebDriverException)
                {
                    continue;
                }

                int[]? nestedPath;
                try
                {
                    nestedPath = FindCredentialFramePath(driver, depth + 1);
                }
                catch (WebDriverException)
                {
                    nestedPath = null;
                }
                finally
                {
                    try
                    {
                        driver.SwitchTo().ParentFrame();
                    }
                    catch (WebDriverException)
                    {
                        driver.SwitchTo().DefaultContent();
                    }
                }

                if (nestedPath != null)
                {
                    return new[] { index }.Concat(nestedPath).ToArray();
                }
            }

            return null;
        }

        private void LogReportStep(string stepName, Action action)
        {
            ExtentReportManager.CreateStep(stepName);
            _qhareScope?.BeginStep(stepName);

            try
            {
                action();
                var heals = PublishQhareStepEvidence(stepPassed: true);
                if (heals == 0)
                {
                    ExtentReportManager.LogPass($"{stepName} completed without QHARE self-healing.");
                }
            }
            catch (Exception exception) when (exception is not IgnoreException)
            {
                PublishQhareStepEvidence(stepPassed: false);
                ExtentReportManager.LogFail($"{stepName} failed: {exception.Message}");
                throw;
            }
        }

        private int PublishQhareStepEvidence(bool stepPassed)
        {
            if (_qhareScope == null)
            {
                return 0;
            }

            var heals = QhareHealingContext.ConsumeStepHealsForReporting(stepPassed);
            QhareExtentEvidence.PublishStepHeals(heals, stepPassed);
            QhareHealingContext.CommitBoundStep(stepPassed);
            return heals.Count;
        }
    }
}
