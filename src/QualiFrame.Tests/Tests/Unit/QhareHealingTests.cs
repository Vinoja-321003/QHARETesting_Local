using System.Collections.ObjectModel;
using System.Drawing;
using System.Net;
using System.Text;
using Allure.Net.Commons;
using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Tests.Tests.Unit
{
    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class QhareHealingTests
    {
        [TearDown]
        public void TearDown()
        {
            QhareHealingContext.Unbind();
        }

        [Test]
        public void Provider_ShouldPreferExplicitQhareOverHealeniumEnabled()
        {
            Assert.That(
                SelfHealingProviderResolver.Resolve("Qhare", healeniumEnabled: true, qhareEnabled: true),
                Is.EqualTo(SelfHealingProvider.Qhare));
            Assert.That(
                SelfHealingProviderResolver.Resolve("Healenium", healeniumEnabled: true, qhareEnabled: true),
                Is.EqualTo(SelfHealingProvider.Healenium));
            Assert.That(
                SelfHealingProviderResolver.Resolve("Off", healeniumEnabled: true, qhareEnabled: true),
                Is.EqualTo(SelfHealingProvider.None));
            Assert.That(
                SelfHealingProviderResolver.Resolve(null, healeniumEnabled: true, qhareEnabled: true),
                Is.EqualTo(SelfHealingProvider.Healenium));
            Assert.That(
                SelfHealingProviderResolver.Resolve("Qhare", healeniumEnabled: true, qhareEnabled: false),
                Is.EqualTo(SelfHealingProvider.None));
        }

        [Test]
        public void LocatorPicker_DescribeAndToBy_ShouldRoundTripId()
        {
            var (type, value) = QhareLocatorPicker.Describe(By.Id("signInBtn"));
            Assert.That(type, Is.EqualTo("id"));
            Assert.That(value, Is.EqualTo("signInBtn"));
            Assert.That(QhareLocatorPicker.ToBy(type, value).ToString(), Is.EqualTo(By.Id("signInBtn").ToString()));
        }

        [Test]
        public void LocatorPicker_ToBy_ShouldEscapeSingleQuotesInTestId()
        {
            var by = QhareLocatorPicker.ToBy("testid", "user's-button");
            Assert.That(by.ToString(), Is.EqualTo(By.CssSelector("[data-testid='user\\'s-button']").ToString()));
        }

        [Test]
        public void LocatorPicker_LogicalName_ShouldSkipWaitHelpers()
        {
            var name = QhareLocatorPicker.LogicalName(
                By.Id("main-"),
                "QualiFrame.Tests.PageObjects.ShopifyHomePage",
                "WaitForElementToBeVisible");

            Assert.That(name, Is.EqualTo("ShopifyHomePage.main"));
        }

        [Test]
        public void LocatorPicker_LogicalName_ShouldUsePageObjectMethod()
        {
            var name = QhareLocatorPicker.LogicalName(
                By.Id("main-"),
                "QualiFrame.Tests.PageObjects.ShopifyHomePage",
                "WaitForPageToLoad");

            Assert.That(name, Is.EqualTo("ShopifyHomePage.WaitForPageToLoad"));
        }

        [Test]
        public void LocatorPicker_Pick_ShouldHonorManifestStrategyOrder()
        {
            var response = new QhareHealResponse
            {
                Status = "HEALED",
                Candidate = new QhareCandidate
                {
                    PreferredLocator = new QhareLocator { Type = "css", Value = "#signInBtn" },
                    Locators =
                    {
                        new QhareLocator { Type = "css", Value = "#signInBtn" },
                        new QhareLocator { Type = "id", Value = "signInBtn" }
                    }
                }
            };

            var picked = QhareLocatorPicker.Pick(response, new[] { "id", "css", "name", "xpath" });
            Assert.That(picked.ToString(), Is.EqualTo(By.Id("signInBtn").ToString()));
        }

        [Test]
        public void Client_Heal_ShouldPostApiV1Heal()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, """{"status":"HEALED","confidence":0.94}""");
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { ProjectId = "retail-web" }, http);

            var response = client.Heal(new { projectId = "retail-web" });

            Assert.That(response.Status, Is.EqualTo("HEALED"));
            Assert.That(handler.Paths, Does.Contain("/api/v1/heal"));
        }

        [Test]
        public void Wrapper_ShouldUseHealedLocatorWhenStatusIsHealed()
        {
            var original = By.Id("loginButton");
            var healed = By.Id("signInBtn");
            var driver = new FakeDriver();
            driver.Add(healed, new FakeElement());

            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("HEALED", "id", "signInBtn"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
            using var wrapper = new QhareDriverWrapper(driver, new QhareConfiguration { FailClosed = true }, client);

            var element = wrapper.FindElement(original);

            Assert.That(element, Is.Not.Null);
            Assert.That(handler.Paths, Does.Contain("/api/v1/heal"));
            Assert.That(handler.Paths, Does.Contain("/api/v1/telemetry/events"));
        }

        [Test]
        public void Wrapper_ShouldNotApplySuggestedCandidate()
        {
            var original = By.Id("loginButton");
            var driver = new FakeDriver();
            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("SUGGESTED", "id", "signInBtn"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
            using var wrapper = new QhareDriverWrapper(driver, new QhareConfiguration { FailClosed = true }, client);

            Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(original)));
            Assert.That(handler.Paths, Does.Contain("/api/v1/heal"));
            Assert.That(driver.FindCount(By.Id("signInBtn")), Is.EqualTo(0));
        }

        [Test]
        public void Wrapper_ShouldFailClosedWhenCoreIsDown()
        {
            var driver = new FakeDriver();
            var handler = new RecordingHandler(HttpStatusCode.InternalServerError, "boom");
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
            using var wrapper = new QhareDriverWrapper(driver, new QhareConfiguration { FailClosed = true }, client);

            Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(By.Id("loginButton"))));
        }

        [Test]
        public void Wrapper_ShouldDeferTelemetryUntilPassingStep()
        {
            QhareHealingContext.Bind(new QhareHealingScope { TestId = "LOGIN_001", ExecutionId = "exec-1" });
            QhareHealingContext.Current!.BeginStep("When the user logs in");

            var original = By.Id("loginButton");
            var healed = By.Id("signInBtn");
            var driver = new FakeDriver();
            driver.Add(healed, new FakeElement());

            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("HEALED", "id", "signInBtn"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration(), http);
            using var wrapper = new QhareDriverWrapper(driver, new QhareConfiguration(), client);

            wrapper.FindElement(original);
            Assert.That(handler.Paths, Does.Not.Contain("/api/v1/telemetry/events"));

            QhareHealingContext.CommitBoundStep(stepPassed: false);
            Assert.That(handler.Paths, Does.Not.Contain("/api/v1/telemetry/events"));

            QhareHealingContext.Current.BeginStep("When the user logs in");
            wrapper.FindElement(original);
            QhareHealingContext.CommitBoundStep(stepPassed: true);
            Assert.That(handler.Paths, Does.Contain("/api/v1/telemetry/events"));
        }

        [Test]
        public void Wrapper_ShouldFlushFailedTelemetryWhenStepFails()
        {
            QhareHealingContext.Bind(new QhareHealingScope { TestId = "LOGIN_001", ExecutionId = "exec-1" });
            QhareHealingContext.Current!.BeginStep("When the user logs in");

            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("UNRESOLVED", "id", "missing"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
            using var wrapper = new QhareDriverWrapper(new FakeDriver(), new QhareConfiguration { FailClosed = true }, client);

            Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(By.Id("loginButton"))));
            Assert.That(handler.Paths, Does.Not.Contain("/api/v1/telemetry/events"));

            QhareHealingContext.CommitBoundStep(stepPassed: false);
            Assert.That(handler.Paths, Does.Contain("/api/v1/telemetry/events"));
        }

        [Test]
        public void Wrapper_FindElementHealPayload_ShouldUseFindAction()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("UNRESOLVED", "id", "missing"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
            using var wrapper = new QhareDriverWrapper(new FakeDriver(), new QhareConfiguration { FailClosed = true }, client);

            Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(By.Id("loginButton"))));
            Assert.That(handler.Bodies.Any(body => body.Contains("\"action\":\"find\"")), Is.True);
            Assert.That(handler.Bodies.Any(body => body.Contains("\"action\":\"click\"")), Is.False);
        }

        [Test]
        public void Wrapper_ShouldNotCallHealTwiceForTheSameLocatorInAStep()
        {
            QhareHealingContext.Bind(new QhareHealingScope { TestId = "LOGIN_001" });
            QhareHealingContext.Current!.BeginStep("When the user logs in");

            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("UNRESOLVED", "id", "missing"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
            using var wrapper = new QhareDriverWrapper(new FakeDriver(), new QhareConfiguration { FailClosed = true }, client);

            Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(By.Id("loginButton"))));
            Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(By.Id("loginButton"))));
            Assert.That(handler.Paths.Count(p => p == "/api/v1/heal"), Is.EqualTo(1));
        }

        [Test]
        public void Wrapper_ShouldAttachHealedTransactionToActiveAllureStep()
        {
            var lifecycle = AllureLifecycle.Instance;
            var testUuid = Guid.NewGuid().ToString();
            var attachmentSources = new List<string>();
            lifecycle.StartTestCase(new TestResult
            {
                uuid = testUuid,
                name = "QHARE trace",
                fullName = "QhareHealingTests.HealedTransaction"
            });
            lifecycle.StartStep(new StepResult { name = "When the user logs in" });
            try
            {
                QhareHealingContext.Bind(new QhareHealingScope { TestId = "LOGIN_001", ExecutionId = "exec-1" });
                QhareHealingContext.Current!.BeginStep("When the user logs in");

                var original = By.Id("loginButton");
                var healed = By.Id("signInBtn");
                var driver = new FakeDriver();
                driver.Add(healed, new FakeElement());
                var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("HEALED", "id", "signInBtn"));
                var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
                using var client = new QhareClient(new QhareConfiguration(), http);
                using var wrapper = new QhareDriverWrapper(driver, new QhareConfiguration(), client);

                Assert.That(wrapper.FindElement(original), Is.Not.Null);

                lifecycle.UpdateTestCase(test =>
                {
                    var transaction = test.steps.Single(step => step.name.Contains("QHARE HEALED", StringComparison.Ordinal));
                    Assert.That(transaction.name, Does.Contain("loginButton"));
                    Assert.That(transaction.name, Does.Contain("signInBtn"));
                    Assert.That(transaction.status, Is.EqualTo(Status.passed));
                    Assert.That(transaction.attachments.Single().name, Is.EqualTo("QHARE healing transaction"));
                    Assert.That(test.description, Does.Contain("outcome: HEALED"));
                    Assert.That(test.labels.Any(label => label.name == "tag" && label.value == "QHARE"), Is.True);
                    attachmentSources.AddRange(transaction.attachments.Select(attachment => attachment.source));
                });
            }
            finally
            {
                StopAllureProbe(lifecycle, testUuid, attachmentSources);
            }
        }

        [Test]
        public void Wrapper_ShouldAttachUnresolvedTransactionToActiveAllureStep()
        {
            var lifecycle = AllureLifecycle.Instance;
            var testUuid = Guid.NewGuid().ToString();
            var attachmentSources = new List<string>();
            lifecycle.StartTestCase(new TestResult
            {
                uuid = testUuid,
                name = "QHARE unresolved trace",
                fullName = "QhareHealingTests.UnresolvedTransaction"
            });
            lifecycle.StartStep(new StepResult { name = "When the user logs in" });
            try
            {
                var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("UNRESOLVED", "id", "missing"));
                var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
                using var client = new QhareClient(new QhareConfiguration { FailClosed = true }, http);
                using var wrapper = new QhareDriverWrapper(new FakeDriver(), new QhareConfiguration { FailClosed = true }, client);

                Assert.Throws<NoSuchElementException>((Action)(() => wrapper.FindElement(By.Id("loginButton"))));

                lifecycle.UpdateTestCase(test =>
                {
                    var transaction = test.steps.Single(step => step.name.Contains("QHARE UNRESOLVED", StringComparison.Ordinal));
                    Assert.That(transaction.status, Is.EqualTo(Status.failed));
                    Assert.That(transaction.statusDetails.message, Is.EqualTo("test"));
                    Assert.That(test.description, Does.Contain("outcome: UNRESOLVED"));
                    Assert.That(test.labels.Any(label => label.name == "tag" && label.value == "QHARE"), Is.True);
                    attachmentSources.AddRange(transaction.attachments.Select(attachment => attachment.source));
                });
            }
            finally
            {
                StopAllureProbe(lifecycle, testUuid, attachmentSources);
            }
        }

        private static void StopAllureProbe(AllureLifecycle lifecycle, string testUuid, IReadOnlyList<string> attachmentSources)
        {
            try
            {
                if (lifecycle.Context.HasStep)
                {
                    lifecycle.StopStep();
                }
            }
            catch (InvalidOperationException)
            {
                // The probe step was already closed.
            }

            try
            {
                if (lifecycle.Context.HasTest)
                {
                    lifecycle.UpdateTestCase(test =>
                    {
                        test.status = Status.passed;
                        test.fullName ??= "QhareHealingTests.AllureProbe";
                    });
                    lifecycle.StopTestCase();
                }
            }
            catch (Exception)
            {
                // Probe cleanup must not hide the assertion result.
            }

            var resultsDir = lifecycle.ResultsDirectory;
            if (string.IsNullOrWhiteSpace(resultsDir) || !Directory.Exists(resultsDir))
            {
                return;
            }

            foreach (var name in attachmentSources.Append($"{testUuid}-result.json"))
            {
                var path = Path.Combine(resultsDir, name);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void Context_ShouldReportValidatedHealsForPassedAndFailedSteps()
        {
            QhareHealingContext.Bind(new QhareHealingScope { TestId = "LOGIN_001" });
            var scope = QhareHealingContext.Current!;
            scope.BeginStep("When User clicks AddToCart button");
            scope.RecordValidatedHeal(new QhareHealRecord
            {
                Step = scope.Step!,
                OriginalLocator = "By.Id: add-to-cart-sauce-labs-backpack",
                HealedLocator = "By.Id: add-to-cart",
                Status = "HEALED",
                Confidence = 0.87,
                Reason = "Candidate auto-healed"
            });

            var failedStepHeals = QhareHealingContext.ConsumeStepHealsForReporting(stepPassed: false);
            Assert.That(failedStepHeals, Has.Count.EqualTo(1), "Heals must be reportable even when the step fails");
            Assert.That(scope.ScenarioHeals, Has.Count.EqualTo(1));

            scope.BeginStep("When User clicks AddToCart button");
            scope.RecordValidatedHeal(new QhareHealRecord
            {
                Step = scope.Step!,
                OriginalLocator = "By.Id: add-to-cart-sauce-labs-backpack",
                HealedLocator = "By.Id: add-to-cart",
                Status = "HEALED",
                Confidence = 0.87
            });

            var passedStepHeals = QhareHealingContext.ConsumeStepHealsForReporting(stepPassed: true);
            Assert.That(passedStepHeals, Has.Count.EqualTo(1));
            Assert.That(scope.ScenarioHeals, Has.Count.EqualTo(2));
            Assert.That(passedStepHeals[0].Summary, Does.Contain("add-to-cart-sauce-labs-backpack"));
            Assert.That(passedStepHeals[0].Summary, Does.Contain("add-to-cart"));
        }

        [Test]
        public void Wrapper_ShouldRecordValidatedHealOnScope()
        {
            QhareHealingContext.Bind(new QhareHealingScope { TestId = "CART_001" });
            QhareHealingContext.Current!.BeginStep("When User clicks AddToCart button");

            var original = By.Id("loginButton");
            var healed = By.Id("signInBtn");
            var driver = new FakeDriver();
            driver.Add(healed, new FakeElement());

            var handler = new RecordingHandler(HttpStatusCode.OK, HealJson("HEALED", "id", "signInBtn"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
            using var client = new QhareClient(new QhareConfiguration(), http);
            using var wrapper = new QhareDriverWrapper(driver, new QhareConfiguration(), client);

            wrapper.FindElement(original);

            Assert.That(QhareHealingContext.Current.StepHeals, Has.Count.EqualTo(1));
            Assert.That(QhareHealingContext.Current.StepHeals[0].OriginalLocator, Does.Contain("loginButton"));
            Assert.That(QhareHealingContext.Current.StepHeals[0].HealedLocator, Does.Contain("signInBtn"));
            Assert.That(QhareHealingContext.Current.StepHeals[0].Status, Is.EqualTo("HEALED"));
        }

        [Test]
        public void TestConfiguration_GetQhareConfiguration_ShouldReadShippedDefaults()
        {
            var config = TestConfiguration.Instance.GetQhareConfiguration();
            Assert.That(config.BaseUrl, Does.StartWith("http://localhost:3001"));
            Assert.That(config.ProjectId, Is.EqualTo("selenium-csharp-integration"));
            Assert.That(config.Mode, Is.EqualTo("AUTO_HEAL"));
            Assert.That(config.Enabled, Is.True);
            Assert.That(TestConfiguration.Instance.ActiveSelfHealingProvider, Is.EqualTo(SelfHealingProvider.Qhare));
        }

        [Test]
        public void ExtentEvidence_ShouldMarkStepAndScenarioWhenHealsPublished()
        {
            ExtentReportManager.CreateFeature("QHARE Extent Evidence Feature");
            ExtentReportManager.CreateScenario("QHARE Extent Evidence Feature", "Add product to cart");
            var step = ExtentReportManager.CreateStep("When User clicks AddToCart button");
            Assert.That(step, Is.Not.Null);

            var heals = new[]
            {
                new QhareHealRecord
                {
                    Step = "When User clicks AddToCart button",
                    OriginalLocator = "By.Id: add-to-cart-sauce-labs-backpack",
                    HealedLocator = "By.Id: add-to-cart",
                    Status = "HEALED",
                    Confidence = 0.91,
                    Reason = "Candidate auto-healed"
                }
            };

            Assert.DoesNotThrow((Action)(() => QhareExtentEvidence.PublishPassingStepHeals(heals)));
            Assert.That(step!.Model.Name, Does.Contain(QhareExtentEvidence.HealedMarker));
            Assert.That(step.Model.HasChildren, Is.True);

            Assert.DoesNotThrow((Action)(() => QhareExtentEvidence.AnnotateScenario(heals)));
            var scenario = ExtentReportManager.GetCurrentScenario();
            Assert.That(scenario, Is.Not.Null);
            Assert.That(scenario!.Model.Name, Does.Contain(QhareExtentEvidence.HealedMarker));
        }

        private static string HealJson(string status, string type, string value) =>
            $$"""
            {
              "status": "{{status}}",
              "confidence": 0.94,
              "candidate": {
                "preferredLocator": { "type": "{{type}}", "value": "{{value}}" },
                "locators": [{ "type": "{{type}}", "value": "{{value}}" }],
                "score": 0.94
              },
              "reason": "test"
            }
            """;

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;

            public RecordingHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            public List<string> Paths { get; } = new();
            public List<string> Bodies { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
                if (request.Content != null)
                {
                    Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
                }

                return new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                };
            }
        }

        private sealed class FakeDriver : IWebDriver
        {
            private readonly Dictionary<string, IWebElement> _elements = new(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _findCounts = new(StringComparer.Ordinal);
            private readonly FakeTimeouts _timeouts = new();

            public string Url { get; set; } = "https://example.test/login";
            public string Title => "Login";
            public string PageSource { get; set; } = "<html><button id=\"signInBtn\">Sign In</button></html>";
            public string CurrentWindowHandle => "w1";
            public ReadOnlyCollection<string> WindowHandles => new(new List<string> { "w1" });

            public void Add(By locator, IWebElement element) => _elements[Key(locator)] = element;

            public int FindCount(By locator) => _findCounts.TryGetValue(Key(locator), out var count) ? count : 0;

            public IWebElement FindElement(By locator)
            {
                var key = Key(locator);
                _findCounts[key] = FindCount(locator) + 1;
                if (_elements.TryGetValue(key, out var element))
                {
                    return element;
                }

                throw new NoSuchElementException(key);
            }

            public ReadOnlyCollection<IWebElement> FindElements(By locator)
            {
                try
                {
                    return new ReadOnlyCollection<IWebElement>(new List<IWebElement> { FindElement(locator) });
                }
                catch (NoSuchElementException)
                {
                    return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
                }
            }

            public void Close() { }
            public void Quit() { }
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            public IOptions Manage() => new FakeOptions(_timeouts);
            public INavigation Navigate() => throw new NotSupportedException();
            public ITargetLocator SwitchTo() => throw new NotSupportedException();

            private static string Key(By locator) => locator.ToString() ?? string.Empty;
        }

        private sealed class FakeElement : IWebElement
        {
            public string TagName => "button";
            public string Text => "Sign In";
            public bool Enabled => true;
            public bool Selected => false;
            public bool Displayed => true;
            public Point Location => default;
            public Size Size => default;
            public void Clear() { }
            public void Click() { }
            public void SendKeys(string text) { }
            public void Submit() { }
            public string GetAttribute(string attributeName) => string.Empty;
            public string GetCssValue(string propertyName) => string.Empty;
            public string GetDomAttribute(string attributeName) => string.Empty;
            public string GetDomProperty(string propertyName) => string.Empty;
            public ISearchContext GetShadowRoot() => throw new NotSupportedException();
            public IWebElement FindElement(By by) => throw new NoSuchElementException();
            public ReadOnlyCollection<IWebElement> FindElements(By by) => new(new List<IWebElement>());
        }

        private sealed class FakeOptions : IOptions
        {
            private readonly FakeTimeouts _timeouts;
            public FakeOptions(FakeTimeouts timeouts) => _timeouts = timeouts;
            public ITimeouts Timeouts() => _timeouts;
            public ICookieJar Cookies => throw new NotSupportedException();
            public IWindow Window => throw new NotSupportedException();
            public ILogs Logs => throw new NotSupportedException();
            public INetwork Network => throw new NotSupportedException();
        }

        private sealed class FakeTimeouts : ITimeouts
        {
            public TimeSpan ImplicitWait { get; set; }
            public TimeSpan PageLoad { get; set; }
            public TimeSpan AsynchronousJavaScript { get; set; }
        }
    }
}
