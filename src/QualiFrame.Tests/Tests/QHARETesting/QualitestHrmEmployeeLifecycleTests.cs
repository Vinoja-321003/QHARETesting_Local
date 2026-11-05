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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QualiFrame.Tests.Tests.Integration
{
    [TestFixture]
    [NonParallelizable]
    [QhareReport]
    public class QualitestHrmEmployeeLifecycleTests : IntegrationTestBase
    {
        private const string ApplicationUrl = "https://qualitest-hrm-ui-production.up.railway.app";

        private static readonly By UsernameField = By.CssSelector("input[data-testid='qualitesthrm-login-username-typo']");
        private static readonly By PasswordField = By.CssSelector("[data-testid='qualitesthrm-login-password']");
        private static readonly By LoginButton = By.CssSelector("[data-testid='qualitesthrm-login-submit']");
        private static readonly By DashboardHeading = By.CssSelector("main h1");
        private static readonly By EmployeeListHeading = By.XPath("//h1[normalize-space()='Employee List']");
        private static readonly By EmployeeSearchField = By.CssSelector("input[placeholder='Ex. John Doe']");
        private static readonly By NoEmployeeResults = By.XPath(
            "//tr[td[contains(normalize-space(.), 'No employees found matching the filter')]]");
        private static readonly By DeleteConfirmation = By.CssSelector("[role='dialog']");
        private QhareHealingScope? _qhareScope;
        [SuppressMessage(
            "NUnit.Analyzers",
            "NUnit1032",
            Justification = "The QHARE NUnit action reports test results before fixture teardown completes.")]
        private Qhare.Selenium.QhareClient? _reportingClient;
        private IWebDriver NativeDriver =>
            Driver is QhareDriverWrapper qhareDriver ? qhareDriver.WrappedDriver : Driver;

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

        [OneTimeTearDown]
        public void DisposeQhareReportingClient()
        {
            _reportingClient?.Dispose();
        }

        [SetUp]
        public override void SetUp()
        {
            Assert.That(
                TestConfiguration.Instance.ActiveSelfHealingProvider,
                Is.EqualTo(SelfHealingProvider.Qhare),
                "This test deliberately uses a stale locator and requires QHARE to be the active self-healing provider.");

            _qhareScope = new QhareHealingScope
            {
                TestId = TestContext.CurrentContext.Test.FullName,
                Feature = "Qualitest HRM Employee Lifecycle"
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
                Assert.That(
                    _qhareScope.Client,
                    Is.Not.Null,
                    "Expected the QHARE WebDriver wrapper to attach its client to the test healing context.");

                ExtentReportManager.CreateScenario(_qhareScope.Feature, TestContext.CurrentContext.Test.Name);
                ExtentReportManager.CreateStep("Initialize HRM browser session");
                ExtentReportManager.LogPass("WebDriver session initialized with QHARE self-healing enabled.");
            }
            catch
            {
                QhareHealingContext.Unbind();
                throw;
            }
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                var result = TestContext.CurrentContext.Result;
                ExtentReportManager.CreateStep("Test result");

                if (_qhareScope != null)
                {
                    QhareExtentEvidence.AnnotateScenario(_qhareScope.ScenarioHeals);
                }

                switch (result.Outcome.Status)
                {
                    case TestStatus.Passed:
                        var scenarioMessage = _qhareScope?.ScenarioHeals.Count > 0
                            ? $"HRM employee lifecycle test passed with {_qhareScope.ScenarioHeals.Count} validated QHARE heal(s)."
                            : "HRM employee lifecycle test passed without requiring a QHARE heal.";
                        ExtentReportManager.MarkScenarioAsPassed(scenarioMessage);
                        break;
                    case TestStatus.Skipped:
                    case TestStatus.Inconclusive:
                        ExtentReportManager.MarkScenarioAsSkipped(result.Message ?? "HRM employee lifecycle test was skipped.");
                        break;
                    default:
                        ExtentReportManager.LogFail(result.Message ?? "HRM employee lifecycle test failed.");
                        ExtentReportManager.MarkScenarioAsFailed("HRM employee lifecycle test failed.");
                        break;
                }

                ExtentReportManager.EnsureFlushed();
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

        [Test]
        [Category("HRM")]
        [Category("EndToEnd")]
        [QhareTestIntent("REQUIRED_ACTION")]
        public void Administrator_CanCreateFindAndDeleteEmployee()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            var employeeId = $"AUTO-{uniqueSuffix}";
            var firstName = $"Test{uniqueSuffix}";
            const string lastName = "Employee";
            var employeeCreated = false;

            try
            {
                Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
                Driver.Manage().Window.Maximize();
                LogReportStep("Sign in as the HRM administrator", () =>
                {
                    Driver.Navigate().GoToUrl($"{ApplicationUrl}/login");
                    wait.Until(driver => driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase));
                    wait.Until(driver => driver.FindElement(UsernameField).Displayed);
                    Driver.FindElement(UsernameField).SendKeys("admin");
                    Driver.FindElement(PasswordField).SendKeys("admin123");
                    Driver.FindElement(LoginButton).Click();

                    wait.Until(driver => driver.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));
                    wait.Until(driver =>
                        driver.FindElement(DashboardHeading).Text.Trim().Contains(
                            "Welcome to QualitestHRM Dashboard",
                            StringComparison.OrdinalIgnoreCase));
                });

                LogReportStep("Open the employee list", () =>
                {
                    var pimNavigation = By.CssSelector("a[href='/pim']");
                    wait.Until(driver => driver.FindElement(pimNavigation).Displayed);
                    Driver.FindElement(pimNavigation).Click();
                    wait.Until(driver => driver.FindElement(EmployeeListHeading).Displayed);
                });

                LogReportStep("Create a uniquely identified employee", () =>
                {
                    Driver.FindElement(By.CssSelector("[data-testid='qualitesthrm-pim-list-add-employee']")).Click();

                    var employeeIdField = By.CssSelector("input[placeholder='EMP1001']");
                    var firstNameField = By.CssSelector("input[placeholder='Jane']");
                    var lastNameField = By.CssSelector("input[placeholder='Doe']");
                    wait.Until(driver => driver.FindElement(employeeIdField).Displayed);

                    Driver.FindElement(employeeIdField).SendKeys(employeeId);
                    Driver.FindElement(firstNameField).SendKeys(firstName);
                    Driver.FindElement(lastNameField).SendKeys(lastName);
                    EnterTextWhenVisible(wait, By.CssSelector("input[formcontrolname='dateOfBirth']"), "1/1/1990");

                    ExpandSection(wait, "2. Contact Information");
                    EnterTextWhenVisible(
                        wait,
                        By.CssSelector("input[formcontrolname='personalEmail']"),
                        $"hrm.test.{uniqueSuffix}@example.com");
                    EnterTextWhenVisible(
                        wait,
                        By.CssSelector("input[formcontrolname='workEmail']"),
                        $"hrm.test.work.{uniqueSuffix}@example.com");

                    ExpandSection(wait, "3. Emergency Contact");
                    EnterTextWhenVisible(
                        wait,
                        By.CssSelector("input[formcontrolname='email']"),
                        $"hrm.test.emergency.{uniqueSuffix}@example.com");

                    ExpandSection(wait, "4. Employment Information");
                    EnterTextWhenVisible(wait, By.CssSelector("input[formcontrolname='dateOfJoining']"), "1/1/2020");
                    EnterTextWhenVisible(wait, By.CssSelector("input[formcontrolname='probationEndDate']"), "4/1/2020");

                    employeeCreated = true;
                    Driver.FindElement(By.CssSelector("[data-testid='qualitesthrm-pim-employee-form-submit']")).Click();
                    wait.Until(driver => !driver.Url.Contains("/pim/create", StringComparison.OrdinalIgnoreCase));
                });

                LogReportStep("Find the new employee and verify their details", () =>
                {
                    Driver.Navigate().GoToUrl($"{ApplicationUrl}/pim");
                    wait.Until(driver => driver.FindElement(EmployeeListHeading).Displayed);
                    var employeeRow = SearchForEmployee(wait, employeeId);
                    Assert.That(employeeRow, Is.Not.Null, $"Expected employee {employeeId} to appear in the employee list.");
                    var createdEmployeeRow = employeeRow
                        ?? throw new AssertionException($"Expected employee {employeeId} to appear in the employee list.");
                    Assert.That(createdEmployeeRow.Text, Does.Contain(firstName));
                    Assert.That(createdEmployeeRow.Text, Does.Contain(lastName));
                });

                LogReportStep("Delete the test employee", () =>
                {
                    var createdEmployeeRow = SearchForEmployee(wait, employeeId)
                        ?? throw new AssertionException($"Expected employee {employeeId} to remain in the employee list.");
                    DeleteEmployee(wait, createdEmployeeRow, employeeId);
                    employeeCreated = false;
                });

                LogReportStep("Log out of the HRM application", () =>
                {
                    Driver.FindElement(By.CssSelector("[data-testid='qualitesthrm-layout-logout']")).Click();
                    wait.Until(driver => driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase));
                    Assert.That(Driver.FindElement(UsernameField).Displayed, Is.True);
                });
            }
            finally
            {
                if (employeeCreated)
                {
                    CleanupEmployee(wait, employeeId);
                }
            }
        }

        [Test]
        [Category("HRM")]
        [Category("QHAREExecutionClassification")]
        [QhareTestIntent(
            "NEGATIVE_SAFETY_VALIDATION",
            ExpectedHealingDecision = "REJECTED")]
        public void NegativeSafety_UnresolvedLoginLocator_IsPassedWhenRejected()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            Driver.Navigate().GoToUrl($"{ApplicationUrl}/login");
            wait.Until(driver => driver.FindElement(UsernameField).Displayed);
            Driver.FindElement(UsernameField).SendKeys("admin");
            Driver.FindElement(PasswordField).SendKeys("admin123");

            AssertLocatorRemainsUnresolved(By.Id($"qhare-unsafe-login-submit-{Guid.NewGuid():N}"));
            Assert.That(Driver.Url, Does.Contain("/login"));
            Assert.That(NativeDriver.FindElement(LoginButton).Displayed, Is.True);
        }

        [Test]
        [Category("HRM")]
        [Category("QHAREExecutionClassification")]
        [QhareTestIntent("OPTIONAL_ACTION", Optional = true)]
        public void OptionalEmployeeLookup_UnresolvedLocator_IsPassedWhenAbsent()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            SignInAndOpenEmployeeList(wait);

            var absentEmployeeRow = GetEmployeeRowLocator($"OPTIONAL-{Guid.NewGuid():N}");
            Assert.That(NativeDriver.FindElements(absentEmployeeRow), Is.Empty);
            AssertLocatorRemainsUnresolved(absentEmployeeRow);
        }

        [Test]
        [Category("HRM")]
        [Category("QHAREExecutionClassification")]
        [QhareTestIntent("REQUIRED_ACTION")]
        public void RequiredEmployeeListAction_UnresolvedLocator_UsesNativeFallbackAndPasses()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            SignInAndOpenEmployeeList(wait);

            AssertLocatorRemainsUnresolved(By.Id($"qhare-required-list-heading-{Guid.NewGuid():N}"));
            Assert.That(NativeDriver.FindElement(EmployeeListHeading).Displayed, Is.True);
        }

        [Test]
        [Category("HRM")]
        [Category("QHAREExecutionClassification")]
        [QhareTestIntent("REQUIRED_ACTION")]
        public void RequiredEmployeeListAction_UnresolvedLocator_FailsWithoutBlockingEvidence()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            SignInAndOpenEmployeeList(wait);
            AssertLocatorRemainsUnresolved(By.Id($"qhare-required-list-heading-{Guid.NewGuid():N}"));

            Assert.Fail("The required HRM employee-list action did not resolve.");
        }

        [Test]
        [Category("HRM")]
        [Category("QHAREExecutionClassification")]
        [QhareTestIntent(
            "REQUIRED_ACTION",
            BlockingReason = "The required HRM employee-list action could not continue because no safe locator was available.")]
        public void RequiredEmployeeListAction_UnresolvedLocator_IsBlockedWithExplicitReason()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            SignInAndOpenEmployeeList(wait);
            AssertLocatorRemainsUnresolved(By.Id($"qhare-required-list-heading-{Guid.NewGuid():N}"));

            Assert.Fail("The required HRM employee-list workflow was blocked.");
        }

        [Test]
        [Category("HRM")]
        [Category("QHAREExecutionClassification")]
        [QhareTestIntent("OBSERVATION_ONLY")]
        public void UnresolvedOptionalObservation_CanBeSkipped()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            SignInAndOpenEmployeeList(wait);
            AssertLocatorRemainsUnresolved(By.Id($"qhare-observation-{Guid.NewGuid():N}"));

            Assert.Ignore("The optional HRM observation is unavailable and was skipped.");
        }

        private IWebElement? SearchForEmployee(WebDriverWait wait, string employeeId)
        {
            var searchField = Driver.FindElement(EmployeeSearchField);
            searchField.Clear();
            searchField.SendKeys(employeeId);
            searchField.SendKeys(Keys.Enter);

            var employeeRowLocator = GetEmployeeRowLocator(employeeId);
            wait.Until(driver =>
                driver.FindElements(employeeRowLocator).Any()
                || driver.FindElements(NoEmployeeResults).Any());

            return Driver.FindElements(employeeRowLocator).FirstOrDefault();
        }

        private void DeleteEmployee(WebDriverWait wait, IWebElement employeeRow, string employeeId)
        {
            employeeRow.FindElement(By.CssSelector("[data-testid='qualitesthrm-pim-list-row-delete']")).Click();
            wait.Until(driver => driver.FindElement(DeleteConfirmation).Displayed);
            Driver.FindElement(DeleteConfirmation)
                .FindElement(By.XPath(".//button[normalize-space()='Delete']"))
                .Click();

            var employeeRowLocator = GetEmployeeRowLocator(employeeId);
            wait.Until(driver => !driver.FindElements(employeeRowLocator).Any());
        }

        private void CleanupEmployee(WebDriverWait wait, string employeeId)
        {
            Driver.Navigate().GoToUrl($"{ApplicationUrl}/pim");
            wait.Until(driver => driver.FindElement(EmployeeListHeading).Displayed);
            var employeeRow = SearchForEmployee(wait, employeeId);

            if (employeeRow != null)
            {
                DeleteEmployee(wait, employeeRow, employeeId);
            }
        }

        private void ExpandSection(WebDriverWait wait, string sectionName)
        {
            var sectionLocator = By.XPath($"//mat-expansion-panel-header[normalize-space()='{sectionName}']");
            var sectionHeader = wait.Until(driver => driver.FindElement(sectionLocator));

            if (!string.Equals(sectionHeader.GetAttribute("aria-expanded"), "true", StringComparison.OrdinalIgnoreCase))
            {
                sectionHeader.Click();
                wait.Until(driver => string.Equals(
                    driver.FindElement(sectionLocator).GetAttribute("aria-expanded"),
                    "true",
                    StringComparison.OrdinalIgnoreCase));
            }
        }

        private void EnterTextWhenVisible(WebDriverWait wait, By locator, string value)
        {
            wait.Until(driver => driver.FindElement(locator).Displayed);
            Driver.FindElement(locator).SendKeys(value);
        }

        private void SignInAndOpenEmployeeList(WebDriverWait wait)
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            Driver.Navigate().GoToUrl($"{ApplicationUrl}/login");
            wait.Until(driver => driver.FindElement(UsernameField).Displayed);
            Driver.FindElement(UsernameField).SendKeys("admin");
            Driver.FindElement(PasswordField).SendKeys("admin123");
            Driver.FindElement(LoginButton).Click();
            wait.Until(driver => driver.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));

            Driver.Navigate().GoToUrl($"{ApplicationUrl}/pim");
            wait.Until(driver => driver.FindElement(EmployeeListHeading).Displayed);
        }

        private void AssertLocatorRemainsUnresolved(By locator)
        {
            try
            {
                _ = Driver.FindElement(locator);
            }
            catch (NoSuchElementException)
            {
                return;
            }

            Assert.Fail($"QHARE returned a candidate for an intentionally unresolved HRM locator: {locator}.");
        }

        private void LogReportStep(string stepName, Action action)
        {
            ExtentReportManager.CreateStep(stepName);
            _qhareScope?.BeginStep(stepName);

            try
            {
                action();
                var healCount = PublishQhareStepEvidence(stepPassed: true);
                if (healCount == 0)
                {
                    ExtentReportManager.LogPass($"{stepName} completed without QHARE self-healing.");
                }
            }
            catch (Exception exception)
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

        private static By GetEmployeeRowLocator(string employeeId)
        {
            return By.XPath($"//tr[td[normalize-space()='{employeeId}']]");
        }
    }
}
