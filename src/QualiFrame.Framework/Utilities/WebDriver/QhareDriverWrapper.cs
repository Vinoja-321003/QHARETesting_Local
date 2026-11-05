using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using OpenQA.Selenium;
using Qhare.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// IWebDriver facade for QHARE. Native FindElement runs first; only HEALED candidates replace the locator.
    /// </summary>
    public sealed class QhareDriverWrapper : IWebDriver, IWrapsDriver, IJavaScriptExecutor, ITakesScreenshot, IDisposable
    {
        private readonly IWebDriver _baseDriver;
        private readonly QhareClient _client;
        private readonly QhareConfiguration _config;
        private readonly bool _ownsClient;
        private bool _healingInProgress;
        private bool _disposed;

        public QhareDriverWrapper(IWebDriver baseDriver, QhareConfiguration? config = null)
            : this(baseDriver, config, null)
        {
        }

        public QhareDriverWrapper(IWebDriver baseDriver, QhareConfiguration? config, QhareClient? client)
        {
            _baseDriver = baseDriver ?? throw new ArgumentNullException(nameof(baseDriver));
            _config = config ?? new QhareConfiguration();
            if (client != null)
            {
                _client = client;
                _ownsClient = false;
            }
            else
            {
                _client = new QhareClient(_config);
                _ownsClient = true;
            }

            QhareHealingContext.AttachClient(_client);
            Logger.Information(
                "QHARE self-healing enabled. Core: {BaseUrl}. Project: {ProjectId}. Mode: {Mode}",
                _config.BaseUrl,
                _config.ProjectId,
                _config.Mode);
        }

        public IWebDriver WrappedDriver => _baseDriver;

        public IWebElement FindElement(By locator)
        {
            try
            {
                return _baseDriver.FindElement(locator);
            }
            catch (NoSuchElementException ex) when (!_healingInProgress)
            {
                var healed = TryHeal(locator, "findElement", ex);
                if (healed != null)
                {
                    return healed;
                }

                throw;
            }
        }

        public ReadOnlyCollection<IWebElement> FindElements(By locator)
        {
            var found = _baseDriver.FindElements(locator);
            if (found.Count > 0 || _healingInProgress)
            {
                return found;
            }

            try
            {
                var healed = TryHeal(locator, "findElements", new NoSuchElementException(locator.ToString()));
                if (healed != null)
                {
                    return new ReadOnlyCollection<IWebElement>(new List<IWebElement> { healed });
                }
            }
            catch (NoSuchElementException)
            {
                return found;
            }

            return found;
        }

        public string Url
        {
            get => _baseDriver.Url;
            set => _baseDriver.Url = value;
        }

        public string Title => _baseDriver.Title;
        public string PageSource => _baseDriver.PageSource;
        public string CurrentWindowHandle => _baseDriver.CurrentWindowHandle;
        public ReadOnlyCollection<string> WindowHandles => _baseDriver.WindowHandles;

        public void Close() => _baseDriver.Close();
        public void Quit() => _baseDriver.Quit();
        public IOptions Manage() => _baseDriver.Manage();
        public INavigation Navigate() => _baseDriver.Navigate();
        public ITargetLocator SwitchTo() => _baseDriver.SwitchTo();

        public object? ExecuteScript(string script, params object?[] args) =>
            ((IJavaScriptExecutor)_baseDriver).ExecuteScript(script, args);

        public object? ExecuteScript(PinnedScript script, params object?[] args) =>
            ((IJavaScriptExecutor)_baseDriver).ExecuteScript(script, args);

        public object? ExecuteAsyncScript(string script, params object?[] args) =>
            ((IJavaScriptExecutor)_baseDriver).ExecuteAsyncScript(script, args);

        public Screenshot GetScreenshot() =>
            ((ITakesScreenshot)_baseDriver).GetScreenshot();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _baseDriver.Dispose();
            if (_ownsClient)
            {
                _client.Dispose();
            }

            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private IWebElement? TryHeal(By original, string command, NoSuchElementException nativeError)
        {
            if (!_config.Enabled || IsMode("OFF"))
            {
                ExceptionDispatchInfo.Capture(nativeError).Throw();
                return null;
            }

            var cacheKey = original.ToString() ?? string.Empty;
            var scope = QhareHealingContext.Current;
            if (scope != null && scope.FailedLocators.Contains(cacheKey))
            {
                ExceptionDispatchInfo.Capture(nativeError).Throw();
                return null;
            }

            if (scope != null && scope.HealedLocators.TryGetValue(cacheKey, out var cached))
            {
                return _baseDriver.FindElement(cached);
            }

            _healingInProgress = true;
            TimeSpan? previousWait = null;
            try
            {
                previousWait = TryZeroImplicitWait();
                _client.EnsureManifest();

                QhareHealResponse response;
                try
                {
                    response = _client.Heal(BuildHealPayload(original, command, scope));
                }
                catch (Exception ex)
                {
                    Logger.Warning("QHARE heal request failed: {Message}", ex.Message);
                    RecordAllureTrace(scope, "FAILED", original, null, null, ex.Message, command);
                    if (_config.FailClosed)
                    {
                        ExceptionDispatchInfo.Capture(nativeError).Throw();
                    }

                    throw;
                }

                if (IsStatus(response.Status, "HEALED"))
                {
                    var healedBy = QhareLocatorPicker.Pick(response, _client.LocatorStrategies);
                    IWebElement element;
                    try
                    {
                        element = _baseDriver.FindElement(healedBy);
                    }
                    catch (NoSuchElementException)
                    {
                        var pierced = OpenShadowDom.Find(_baseDriver, healedBy);
                        if (pierced is null)
                        {
                            throw;
                        }

                        element = pierced;
                    }
                    var resolved = IsDisplayed(element);
                    QueueOrEmit(scope, resolved ? "heal.validated" : "heal.rejected", new
                    {
                        postCondition = resolved ? "passed" : "failed",
                        validationPolicy = "ELEMENT_RESOLVED",
                        originalLocator = original.ToString(),
                        healedLocator = healedBy.ToString(),
                        confidence = response.Confidence,
                        command
                    });
                    RecordAllureTrace(
                        scope,
                        resolved ? "HEALED" : "REJECTED",
                        original,
                        healedBy.ToString(),
                        response.Confidence,
                        response.Reason,
                        command);

                    if (!resolved)
                    {
                        LogHealingOutcome(
                            "QHARE candidate rejected (element not displayed) {Original} -> {Healed} (status {Status}, confidence {Confidence}, reason {Reason})",
                            original, healedBy, response);
                        scope?.FailedLocators.Add(cacheKey);
                        ExceptionDispatchInfo.Capture(nativeError).Throw();
                    }

                    if (scope != null)
                    {
                        scope.HealedLocators[cacheKey] = healedBy;
                        RecordValidatedHeal(scope, original, healedBy, response, "ELEMENT_RESOLVED");
                    }
                    LogHealingOutcome(
                        "QHARE healed locator {Original} -> {Healed} (status {Status}, confidence {Confidence}, reason {Reason})",
                        original, healedBy, response);

                    return element;
                }

                if (TryRecover(scope, original, cacheKey, command) is { } recovered)
                {
                    return recovered;
                }

                LogUnresolved(original, response);
                var topAlternative = DescribeTopAlternative(response);
                QueueOrEmit(scope, "heal.failed", new
                {
                    reason = response.Reason,
                    status = response.Status,
                    confidence = response.Confidence,
                    originalLocator = original.ToString(),
                    topAlternative
                });
                RecordAllureTrace(
                    scope,
                    string.IsNullOrWhiteSpace(response.Status) ? "UNRESOLVED" : response.Status,
                    original,
                    topAlternative,
                    response.Confidence,
                    response.Reason,
                    command);
                scope?.FailedLocators.Add(cacheKey);
                ExceptionDispatchInfo.Capture(nativeError).Throw();
                return null;
            }
            catch (NoSuchElementException)
            {
                throw;
            }
            catch (Exception ex) when (_config.FailClosed)
            {
                Logger.Warning("QHARE healing aborted: {Message}", ex.Message);
                ExceptionDispatchInfo.Capture(nativeError).Throw();
                return null;
            }
            finally
            {
                RestoreImplicitWait(previousWait);
                _healingInProgress = false;
            }
        }

        private IWebElement? TryRecover(QhareHealingScope? scope, By original, string cacheKey, string command)
        {
            try
            {
                var recovered = _client.Recover(BuildRecoverPayload(scope));
                if (!IsStatus(recovered.Status, "RECOVERED"))
                {
                    return null;
                }

                ExecuteRecoveryActions(recovered.Actions);
                var element = _baseDriver.FindElement(original);
                if (scope != null)
                {
                    scope.HealedLocators[cacheKey] = original;
                    scope.RecordValidatedHeal(new QhareHealRecord
                    {
                        Step = scope.Step ?? string.Empty,
                        OriginalLocator = original.ToString() ?? string.Empty,
                        HealedLocator = original.ToString() ?? string.Empty,
                        Status = "RECOVERED",
                        Confidence = 1.0,
                        Reason = "QHARE recovery restored the original locator.",
                        ValidationPolicy = "RECOVERED_THEN_ORIGINAL"
                    });
                }
                QueueOrEmit(scope, "heal.validated", new
                {
                    postCondition = "passed",
                    validationPolicy = "RECOVERED_THEN_ORIGINAL",
                    originalLocator = original.ToString()
                });
                RecordAllureTrace(scope, "RECOVERED", original, original.ToString(), null, "Recovered, then the original locator resolved.", command);
                return element;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.Debug("QHARE recovery skipped: {Message}", ex.Message);
                return null;
            }
        }

        private object BuildHealPayload(By by, string command, QhareHealingScope? scope)
        {
            var (locatorType, locatorValue) = QhareLocatorPicker.Describe(by);
            var (className, methodName) = HealeniumLocatorMapper.OriginCaller();
            var logicalName = QhareLocatorPicker.LogicalName(by, className, methodName);
            var action = command.StartsWith("find", StringComparison.OrdinalIgnoreCase) ? "find" : "click";
            var step = scope?.Step ?? methodName;
            var executionId = scope?.ExecutionId ?? Guid.NewGuid().ToString("N");
            var testId = string.IsNullOrWhiteSpace(scope?.TestId) ? methodName : scope!.TestId;

            return new
            {
                projectId = _client.ProjectId,
                executionId,
                tenantId = "qualiframe",
                framework = new
                {
                    name = "selenium",
                    language = "csharp",
                    version = _config.FrameworkVersion,
                    adapterVersion = QhareClient.AdapterVersion
                },
                test = new { testId, step, scenario = scope?.TestId ?? string.Empty },
                element = new
                {
                    logicalName,
                    originalLocator = locatorValue,
                    locatorType,
                    action,
                    pageObject = className,
                    fingerprint = new
                    {
                        logical_name = logicalName,
                        visualHash = VisualHash(logicalName, locatorValue)
                    }
                },
                page = new { url = SafeUrl(), title = SafeTitle() },
                contextRef = new { dom = QhareClient.TruncateDom(SafePageSource()) },
                mode = _config.Mode
            };
        }

        private object BuildRecoverPayload(QhareHealingScope? scope) => new
        {
            projectId = _client.ProjectId,
            executionId = scope?.ExecutionId ?? Guid.NewGuid().ToString("N"),
            tenantId = "qualiframe",
            framework = new
            {
                name = "selenium",
                language = "csharp",
                version = _config.FrameworkVersion,
                adapterVersion = QhareClient.AdapterVersion
            },
            test = new { testId = scope?.TestId ?? "FindElement", step = scope?.Step ?? "runtime-recovery" },
            page = new { url = SafeUrl(), title = SafeTitle() },
            contextRef = new { dom = QhareClient.TruncateDom(SafePageSource()) },
            mode = _config.Mode
        };

        private void ExecuteRecoveryActions(IEnumerable<QhareRecoveryAction> actions)
        {
            foreach (var action in actions)
            {
                var kind = action.Type ?? string.Empty;
                if (kind.Equals("wait", StringComparison.OrdinalIgnoreCase) && action.Ms > 0)
                {
                    Thread.Sleep(Math.Min(action.Ms, 5000));
                }
                else if (kind.Equals("press_escape", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _baseDriver.SwitchTo().ActiveElement().SendKeys(Keys.Escape);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("QHARE press_escape skipped: {Message}", ex.Message);
                    }
                }
                else if (kind.Equals("navigate_back", StringComparison.OrdinalIgnoreCase))
                {
                    _baseDriver.Navigate().Back();
                }
                else if (kind.Equals("click", StringComparison.OrdinalIgnoreCase) && action.Locator != null)
                {
                    var by = QhareLocatorPicker.ToBy(action.Locator.Type, action.Locator.Value ?? action.Locator.Name);
                    _baseDriver.FindElement(by).Click();
                }
            }
        }

        private void RecordAllureTrace(
            QhareHealingScope? scope,
            string outcome,
            By original,
            string? healedLocator,
            double? confidence,
            string? reason,
            string command)
        {
            QhareAllureTrace.Record(new QhareHealingTrace
            {
                Outcome = outcome,
                OriginalLocator = original.ToString() ?? string.Empty,
                HealedLocator = healedLocator,
                Confidence = confidence,
                Reason = reason,
                Command = command,
                Step = scope?.Step,
                TestId = scope?.TestId,
                ExecutionId = scope?.ExecutionId,
                Mode = _config.Mode
            });
        }

        private void QueueOrEmit(QhareHealingScope? scope, string type, object payload)
        {
            var executionId = scope?.ExecutionId ?? Guid.NewGuid().ToString("N");
            var testId = scope?.TestId ?? "FindElement";
            if (scope != null)
            {
                scope.QueueTelemetry(new PendingQhareTelemetry
                {
                    Type = type,
                    ExecutionId = executionId,
                    TestId = testId,
                    Payload = payload
                });
                return;
            }

            _client.Emit(type, executionId, testId, payload);
        }

        private TimeSpan? TryZeroImplicitWait()
        {
            try
            {
                var timeouts = _baseDriver.Manage().Timeouts();
                var previous = timeouts.ImplicitWait;
                timeouts.ImplicitWait = TimeSpan.Zero;
                return previous;
            }
            catch
            {
                return null;
            }
        }

        private void RestoreImplicitWait(TimeSpan? previous)
        {
            if (previous == null)
            {
                return;
            }

            try
            {
                _baseDriver.Manage().Timeouts().ImplicitWait = previous.Value;
            }
            catch
            {
                // ignore restore failures
            }
        }

        private static bool IsDisplayed(IWebElement element)
        {
            try
            {
                return element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        private void LogUnresolved(By original, QhareHealResponse response)
        {
            LogHealingOutcome(
                "QHARE did not auto-heal locator {Original} -> {Healed} (status {Status}, confidence {Confidence}, reason {Reason})",
                original,
                DescribeTopAlternative(response) ?? "(none)",
                response);
        }

        private void LogHealingOutcome(string template, By original, object healed, QhareHealResponse response)
        {
            if (!_config.LogHealingAttempts)
            {
                return;
            }

            Logger.Information(
                template,
                original,
                healed,
                response.Status,
                response.Confidence,
                string.IsNullOrWhiteSpace(response.Reason) ? "(none)" : response.Reason);
        }

        private static void RecordValidatedHeal(
            QhareHealingScope scope,
            By original,
            By healed,
            QhareHealResponse response,
            string validationPolicy)
        {
            scope.RecordValidatedHeal(new QhareHealRecord
            {
                Step = scope.Step ?? string.Empty,
                OriginalLocator = original.ToString() ?? string.Empty,
                HealedLocator = healed.ToString() ?? string.Empty,
                Status = string.IsNullOrWhiteSpace(response.Status) ? "HEALED" : response.Status,
                Confidence = response.Confidence,
                Reason = string.IsNullOrWhiteSpace(response.Reason) ? "QHARE auto-healed locator." : response.Reason,
                ValidationPolicy = validationPolicy
            });
        }

        private static string? DescribeTopAlternative(QhareHealResponse response)
        {
            var preferred = response.Candidate?.PreferredLocator;
            if (preferred != null && (!string.IsNullOrWhiteSpace(preferred.Value) || !string.IsNullOrWhiteSpace(preferred.Name)))
            {
                return $"{preferred.Type}={preferred.Value ?? preferred.Name}";
            }

            var alternative = response.Alternatives.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.Type) && !string.IsNullOrWhiteSpace(a.Value));
            return alternative == null ? null : $"{alternative.Type}={alternative.Value} (score {alternative.Score:0.###})";
        }

        private bool IsMode(string mode) =>
            string.Equals(_config.Mode, mode, StringComparison.OrdinalIgnoreCase);

        private static bool IsStatus(string? status, string expected) =>
            string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);

        private string SafeUrl()
        {
            try
            {
                return _baseDriver.Url ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string SafeTitle()
        {
            try
            {
                return _baseDriver.Title ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string SafePageSource()
        {
            try
            {
                if (OpenShadowDom.ContainsOpenShadow(_baseDriver))
                {
                    var captured = OpenShadowDom.Capture(_baseDriver);
                    if (!string.IsNullOrWhiteSpace(captured))
                    {
                        return captured;
                    }
                }
            }
            catch
            {
                // Pages without an open shadow root keep the native page source.
            }

            try
            {
                return _baseDriver.PageSource ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string VisualHash(string logicalName, string locator)
        {
            var material = $"{logicalName}|{locator}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
            return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
        }
    }
}
