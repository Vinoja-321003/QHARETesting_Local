using System.Collections.ObjectModel;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// WebDriver wrapper that captures versioned locator evidence and heals from historical DOM snapshots.
    /// Official Healenium backend save/heal/report APIs remain in the loop for SHA reports.
    /// </summary>
    public class HealeniumDriverWrapper : IWebDriver, IWrapsDriver, IJavaScriptExecutor, ITakesScreenshot, IDisposable
    {
        private readonly IWebDriver _baseDriver;
        private readonly HealeniumClient? _client;
        private readonly HealeniumConfiguration _config;
        private readonly HealeniumEvidenceStore _evidence;
        private readonly bool _healingEnabled;
        private readonly string _sessionId;
        private readonly object _selectorCacheGate = new();
        private List<HealeniumStoredSelector>? _storedSelectors;
        private bool _disposed;
        private bool _healingInProgress;

        public HealeniumDriverWrapper(IWebDriver baseDriver, HealeniumConfiguration? config = null)
            : this(baseDriver, config, null)
        {
        }

        public HealeniumDriverWrapper(IWebDriver baseDriver, HealeniumConfiguration? config, HealeniumClient? client)
            : this(baseDriver, config, client, null)
        {
        }

        public HealeniumDriverWrapper(
            IWebDriver baseDriver,
            HealeniumConfiguration? config,
            HealeniumClient? client,
            HealeniumEvidenceStore? evidenceStore)
        {
            _baseDriver = baseDriver ?? throw new ArgumentNullException(nameof(baseDriver));
            _config = config ?? new HealeniumConfiguration();
            _healingEnabled = _config.Enabled;
            _sessionId = GetSessionId(_baseDriver) ?? Guid.NewGuid().ToString("N");
            _evidence = evidenceStore ?? HealeniumEvidenceStore.ForConfiguration(_config);

            if (_healingEnabled)
            {
                _client = client ?? new HealeniumClient(_config);
                Logger.Information(
                    "Healenium self-healing enabled. Backend URL: {BackendUrl}. Evidence: {EvidencePath}",
                    _config.BackendUrl,
                    _evidence.RootPath);
                try
                {
                    _client.InitReport(_sessionId);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Healenium report init failed: {Message}", ex.Message);
                }
            }
            else
            {
                Logger.Debug("Healenium self-healing disabled. Wrapper will pass through to base driver.");
            }
        }

        internal HealeniumEvidenceStore EvidenceStore => _evidence;

        public IWebElement FindElement(By locator)
        {
            try
            {
                var element = _baseDriver.FindElement(locator);
                if (_healingEnabled && !_healingInProgress)
                {
                    element = MaybeReplaceUnexpectedElement(locator, "findElement", element) ?? element;
                    RememberSuccess(locator, "findElement", element);
                }

                return element;
            }
            catch (NoSuchElementException ex) when (_healingEnabled && !_healingInProgress)
            {
                Logger.Warning("Element not found with locator: {Locator}. Attempting evidence-based healing...", locator);
                var healed = TryHeal(locator, "findElement");
                if (healed.Element != null)
                {
                    return healed.Element;
                }

                Logger.Error("Healing failed for locator: {Locator}", locator);
                throw new NoSuchElementException($"Element not found and healing unavailable for locator: {locator}", ex);
            }
        }

        public ReadOnlyCollection<IWebElement> FindElements(By locator)
        {
            var found = _baseDriver.FindElements(locator);
            if (found.Count > 0)
            {
                if (_healingEnabled && !_healingInProgress)
                {
                    RememberSuccess(locator, "findElements", found[0]);
                }

                return found;
            }

            if (!_healingEnabled || _healingInProgress)
            {
                return found;
            }

            Logger.Warning("Elements not found with locator: {Locator}. Attempting evidence-based healing...", locator);
            var healed = TryHeal(locator, "findElements");
            if (healed.Elements.Count > 0)
            {
                return new ReadOnlyCollection<IWebElement>(healed.Elements);
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
        public IWebDriver WrappedDriver => _baseDriver;

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

            _baseDriver?.Dispose();
            _client?.Dispose();
            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private IWebElement? MaybeReplaceUnexpectedElement(By locator, string command, IWebElement element)
        {
            var (type, value) = HealeniumLocatorMapper.Split(locator);
            var url = SafeUrl();
            var trusted = _evidence.GetTrustedBaseline(type, value, url);
            if (trusted?.TargetNode == null)
            {
                return element;
            }

            var path = HealeniumNodeInspector.ReadNodePath(_baseDriver, element);
            var similarity = HealeniumEvidenceScorer.Score(trusted.TargetNode, path.LastOrDefault(), trusted.NodePath, path);
            if (similarity >= HealeniumEvidenceScorer.UnexpectedElementThreshold)
            {
                return element;
            }

            Logger.Warning(
                "Locator {Locator} found an unexpected element (similarity {Similarity} vs baseline v{Version}). Attempting healing...",
                locator, similarity, trusted.Version);

            var healed = TryHeal(locator, command);
            if (healed.Element != null && healed.Score >= similarity && healed.Score >= _config.ScoreCap)
            {
                return healed.Element;
            }

            return element;
        }

        private void RememberSuccess(By locator, string command, IWebElement element)
        {
            try
            {
                var (className, methodName) = HealeniumLocatorMapper.OriginCaller();
                var capture = HealeniumNodeInspector.Capture(
                    locator, command, element, _baseDriver, SafeUrl(), className, methodName);

                SaveSelectorToBackend(capture, command);

                var scope = HealeniumEvidenceContext.Current;
                if (scope != null)
                {
                    var alreadyHealed = scope.PendingHealings.Any(h =>
                        string.Equals(h.Snapshot.LocatorType, capture.LocatorType, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(h.Snapshot.LocatorValue, capture.LocatorValue, StringComparison.Ordinal));
                    if (!alreadyHealed)
                    {
                        scope.QueueSuccess(capture);
                    }

                    return;
                }

                _evidence.CommitSuccess(capture, null);
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium success capture skipped: {Message}", ex.Message);
            }
        }

        private void SaveSelectorToBackend(PendingLocatorCapture capture, string command)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                _client.SaveSelector(new HealeniumSelectorRequest
                {
                    Type = capture.LocatorType,
                    Locator = capture.LocatorValue,
                    ClassName = capture.ClassName ?? "QualiFrame.Tests",
                    MethodName = capture.MethodName ?? "FindElement",
                    Command = command,
                    Url = capture.Url,
                    SessionId = _sessionId,
                    EnableHealing = true,
                    NodePath = capture.NodePath.Count == 0
                        ? new List<List<HealeniumNode>>()
                        : new List<List<HealeniumNode>> { capture.NodePath },
                    ElementIds = capture.ElementIds
                });
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium save selector skipped: {Message}", ex.Message);
            }
        }

        private (IWebElement? Element, List<IWebElement> Elements, double Score) TryHeal(By original, string command)
        {
            var empty = (Element: (IWebElement?)null, Elements: new List<IWebElement>(), Score: 0d);

            _healingInProgress = true;
            var previousWait = _baseDriver.Manage().Timeouts().ImplicitWait;
            try
            {
                _baseDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;

                var (type, value) = HealeniumLocatorMapper.Split(original);
                var (className, methodName) = HealeniumLocatorMapper.OriginCaller();
                var url = SafeUrl();
                var scoreCap = _config.ScoreCap > 0 ? _config.ScoreCap : HealeniumEvidenceScorer.DefaultScoreCap;
                var scope = HealeniumEvidenceContext.Current;

                var scored = ScoreHistoricalCandidates(type, value, url, original);
                EnrichFromBackendReference(scored, original, type, value, className, methodName, command, url);

                var ranked = scored
                    .GroupBy(c => c.Locator.ToString(), StringComparer.Ordinal)
                    .Select(g => g.OrderByDescending(c => c.Score).First())
                    .OrderByDescending(c => c.Score)
                    .ToList();

                var best = ranked.FirstOrDefault(c => c.Score >= scoreCap && c.Element != null);
                var attempt = new HealingAttemptRecord
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    OriginalLocatorType = type,
                    OriginalLocatorValue = value,
                    Url = url,
                    ScoreCap = scoreCap,
                    Feature = scope?.Feature,
                    Scenario = scope?.Scenario,
                    Step = scope?.Step,
                    Candidates = ranked.Take(8).Select(c =>
                    {
                        var dto = HealeniumLocatorMapper.ToDto(c.Locator);
                        return new HealingCandidateScore
                        {
                            LocatorType = dto.Type,
                            LocatorValue = dto.Value,
                            Score = c.Score,
                            MatchedFeatures = c.MatchedFeatures,
                            Selected = false
                        };
                    }).ToList()
                };

                if (best?.Element == null)
                {
                    attempt.HistoricalNode = _evidence.GetTrustedBaseline(type, value, url)?.TargetNode;
                    attempt.Reason = scored.Count == 0
                        ? "No historical locator evidence for this locator or a related locator on the same page. Run a successful find first so a baseline is stored."
                        : $"Best candidate score {(ranked.FirstOrDefault()?.Score ?? 0):0.00} is below threshold {scoreCap:0.00}.";
                    _evidence.RecordFailedAttempt(attempt);
                    Logger.Error("Healing failed for locator: {Locator}. {Reason}", original, attempt.Reason);
                    return empty;
                }

                var healedDto = HealeniumLocatorMapper.ToDto(best.Locator);
                attempt.Healed = true;
                attempt.HealedLocatorType = healedDto.Type;
                attempt.HealedLocatorValue = healedDto.Value;
                attempt.Confidence = best.Score;
                attempt.HistoricalVersionUsed = best.HistoricalVersion;
                attempt.HistoricalNode = best.HistoricalNode;
                attempt.CurrentNode = best.NodePath.LastOrDefault();
                foreach (var candidate in attempt.Candidates)
                {
                    candidate.Selected = HealeniumLocatorMapper.SameLocator(
                        new HealeniumLocatorDto { Type = candidate.LocatorType, Value = candidate.LocatorValue },
                        best.Locator);
                }

                var snapshot = HealeniumNodeInspector.Capture(
                    original, command, best.Element, _baseDriver, url, className, methodName);
                snapshot.StoredLocatorType = healedDto.Type;
                snapshot.StoredLocatorValue = healedDto.Value;
                snapshot.PreviousLocatorType = type;
                snapshot.PreviousLocatorValue = value;

                var pending = new PendingHealingCapture { Attempt = attempt, Snapshot = snapshot };
                if (scope != null)
                {
                    scope.QueueHealing(pending);
                }
                else
                {
                    _evidence.PromoteHealing(pending, null);
                }

                SaveHealingToBackend(type, value, className, methodName, command, url, best, best.Element);

                if (_config.LogHealingAttempts)
                {
                    Logger.Information(
                        "Locator healed with confidence {Confidence} from baseline v{Version}. Original: {Original}, Healed: {Healed}. Features: {Features}",
                        best.Score, best.HistoricalVersion, original, best.Locator, best.MatchedFeatures);
                }

                return (best.Element, new List<IWebElement> { best.Element }, best.Score);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to heal locator: {Locator}. Error: {Message}", original, ex.Message);
                return empty;
            }
            finally
            {
                try
                {
                    _baseDriver.Manage().Timeouts().ImplicitWait = previousWait;
                }
                catch
                {
                    // ignore restore failures
                }

                _healingInProgress = false;
            }
        }

        private List<ScoredHealingCandidate> ScoreHistoricalCandidates(
            string type,
            string value,
            string url,
            By original)
        {
            var scored = new List<ScoredHealingCandidate>();
            var baselines = _evidence.GetBaselinesNewestFirst(type, value, url);
            if (baselines.Count == 0)
            {
                baselines = _evidence.FindRelatedBaselines(type, value, url);
                if (baselines.Count > 0)
                {
                    Logger.Information(
                        "No exact locator evidence for {Locator}; using {Count} related baseline(s) from the same page.",
                        original, baselines.Count);
                }
            }

            var max = Math.Max(1, _config.MaxHealingAttempts);

            foreach (var baseline in baselines)
            {
                var historical = baseline.TargetNode ?? baseline.NodePath.LastOrDefault();
                if (historical == null)
                {
                    continue;
                }

                var locators = new List<By>(HealeniumLocatorMapper.CandidatesFromNode(historical, max * 3));
                locators.Add(HealeniumLocatorMapper.FromTypeAndValue(baseline.LocatorType, baseline.LocatorValue));

                foreach (var candidate in locators)
                {
                    if (candidate.ToString() == original.ToString())
                    {
                        continue;
                    }

                    IReadOnlyList<IWebElement> matches;
                    try
                    {
                        matches = _baseDriver.FindElements(candidate).Where(e => e.Displayed).ToList();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var match in matches.Take(3))
                    {
                        var path = HealeniumNodeInspector.ReadNodePath(_baseDriver, match);
                        var current = path.LastOrDefault();
                        var score = HealeniumEvidenceScorer.Score(historical, current, baseline.NodePath, path);
                        scored.Add(new ScoredHealingCandidate
                        {
                            Locator = candidate,
                            Score = score,
                            MatchedFeatures = HealeniumEvidenceScorer.DescribeMatches(historical, current, baseline.NodePath, path),
                            NodePath = path,
                            Attributes = HealeniumNodeInspector.ReadAttributes(match),
                            HistoricalVersion = baseline.Version,
                            HistoricalNode = historical,
                            Element = match
                        });
                    }
                }
            }

            return scored;
        }

        private void EnrichFromBackendReference(
            List<ScoredHealingCandidate> scored,
            By original,
            string type,
            string value,
            string className,
            string methodName,
            string command,
            string url)
        {
            if (_client == null)
            {
                return;
            }

            HealeniumReferenceElements? reference = null;
            try
            {
                reference = _client.GetReferenceElements(value, className, methodName, command, url);
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium get reference skipped: {Message}", ex.Message);
            }

            var targetNode = reference?.Paths?.LastOrDefault(path => path.Count > 0)?.LastOrDefault();
            if (targetNode == null)
            {
                EnrichFromRelatedStoredSelectors(scored, original, type, value, className, methodName, command, url);
                if (scored.Count == 0)
                {
                    Logger.Warning(
                        "No Healenium reference data for locator {Locator} on {Url}. Run a successful find first so the selector is stored.",
                        original, url);
                }

                return;
            }

            var max = Math.Max(1, _config.MaxHealingAttempts);
            var locators = new List<By>(HealeniumLocatorMapper.CandidatesFromNode(targetNode, max));
            foreach (var imitated in _client.Imitate(HealeniumLocatorMapper.ToDto(original), targetNode))
            {
                if (!string.IsNullOrWhiteSpace(imitated.Value))
                {
                    locators.Add(HealeniumLocatorMapper.FromDto(imitated));
                }
            }

            var unsuccessful = reference?.UnsuccessfulLocators ?? new List<HealeniumLocatorDto>();
            foreach (var candidate in locators)
            {
                if (unsuccessful.Any(u => HealeniumLocatorMapper.SameLocator(u, candidate)))
                {
                    continue;
                }

                IReadOnlyList<IWebElement> matches;
                try
                {
                    matches = _baseDriver.FindElements(candidate).Where(e => e.Displayed).ToList();
                }
                catch
                {
                    continue;
                }

                if (matches.Count != 1)
                {
                    continue;
                }

                var match = matches[0];
                var path = HealeniumNodeInspector.ReadNodePath(_baseDriver, match);
                var current = path.LastOrDefault();
                var score = HealeniumEvidenceScorer.Score(targetNode, current, reference?.Paths?.LastOrDefault(), path);
                if (score < 0.01)
                {
                    score = string.IsNullOrWhiteSpace(targetNode.Id) ? 0.8 : 0.95;
                }

                scored.Add(new ScoredHealingCandidate
                {
                    Locator = candidate,
                    Score = score,
                    MatchedFeatures = HealeniumEvidenceScorer.DescribeMatches(targetNode, current),
                    NodePath = path,
                    Attributes = HealeniumNodeInspector.ReadAttributes(match),
                    HistoricalVersion = 0,
                    HistoricalNode = targetNode,
                    Element = match
                });
            }
        }

        private void EnrichFromRelatedStoredSelectors(
            List<ScoredHealingCandidate> scored,
            By original,
            string type,
            string value,
            string className,
            string methodName,
            string command,
            string url)
        {
            var related = HealeniumLocatorMapper.RankRelatedSelectors(CachedSelectors(), type, value, url);
            if (related.Count == 0)
            {
                return;
            }

            Logger.Information(
                "No exact Healenium reference for {Locator}; trying {Count} related selector(s) stored for {Url}.",
                original, related.Count, url);

            foreach (var (similarity, locator, source) in related.Take(8))
            {
                IReadOnlyList<IWebElement> matches;
                try
                {
                    matches = _baseDriver.FindElements(locator).Where(e => e.Displayed).ToList();
                }
                catch
                {
                    continue;
                }

                if (matches.Count == 0)
                {
                    continue;
                }

                if (matches.Count > 1 &&
                    !string.Equals(command, "findElements", StringComparison.OrdinalIgnoreCase) &&
                    similarity < 0.7)
                {
                    continue;
                }

                var match = matches[0];
                var path = HealeniumNodeInspector.ReadNodePath(_baseDriver, match);
                var current = path.LastOrDefault();
                var (_, storedValue) = HealeniumLocatorMapper.ParseReportedLocator(source.Locator);
                var relatedReference = TryGetRelatedReference(source, storedValue, className, methodName, command, url);
                var historical = relatedReference?.Paths?.LastOrDefault(pathNodes => pathNodes.Count > 0)?.LastOrDefault();
                var score = historical == null
                    ? 0
                    : HealeniumEvidenceScorer.Score(historical, current, relatedReference?.Paths?.LastOrDefault(), path);
                if (score < HealeniumLocatorMapper.RelatedLocatorMinSimilarity)
                {
                    score = 0.88 + (0.11 * similarity);
                }

                scored.Add(new ScoredHealingCandidate
                {
                    Locator = locator,
                    Score = Math.Min(0.99, score),
                    MatchedFeatures = historical == null
                        ? $"related-locator similarity={similarity:0.00}"
                        : HealeniumEvidenceScorer.DescribeMatches(historical, current),
                    NodePath = path,
                    Attributes = HealeniumNodeInspector.ReadAttributes(match),
                    HistoricalVersion = 0,
                    HistoricalNode = historical ?? current,
                    Element = match
                });
            }
        }

        private HealeniumReferenceElements? TryGetRelatedReference(
            HealeniumStoredSelector source,
            string storedValue,
            string className,
            string methodName,
            string command,
            string url)
        {
            if (_client == null || string.IsNullOrWhiteSpace(storedValue))
            {
                return null;
            }

            try
            {
                return _client.GetReferenceElements(
                    storedValue,
                    string.IsNullOrWhiteSpace(source.ClassName) ? className : source.ClassName,
                    string.IsNullOrWhiteSpace(source.MethodName) ? methodName : source.MethodName,
                    string.IsNullOrWhiteSpace(source.Command) ? command : source.Command,
                    url);
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium related reference skipped: {Message}", ex.Message);
                return null;
            }
        }

        private IReadOnlyList<HealeniumStoredSelector> CachedSelectors()
        {
            if (_storedSelectors != null)
            {
                return _storedSelectors;
            }

            lock (_selectorCacheGate)
            {
                _storedSelectors ??= _client?.GetAllSelectors() ?? new List<HealeniumStoredSelector>();
                return _storedSelectors;
            }
        }

        private void SaveHealingToBackend(
            string type,
            string value,
            string className,
            string methodName,
            string command,
            string url,
            ScoredHealingCandidate best,
            IWebElement usedElement)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                var dto = HealeniumLocatorMapper.ToDto(best.Locator);
                var result = new HealeniumHealingResult
                {
                    Locator = dto,
                    Score = best.Score,
                    SuccessHealing = true
                };
                _client.SaveHealing(new HealeniumHealingRequest
                {
                    Locator = value,
                    Type = type,
                    ClassName = className,
                    MethodName = methodName,
                    Command = command,
                    Url = url,
                    PageContent = _baseDriver.PageSource,
                    Results = new List<HealeniumHealingResult> { result },
                    UsedResult = result,
                    Screenshot = TryScreenshot(),
                    ElementIds = HealeniumNodeInspector.CaptureElementIds(usedElement)
                }, _sessionId);
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium save healing skipped: {Message}", ex.Message);
            }
        }

        private byte[]? TryScreenshot()
        {
            try
            {
                return GetScreenshot().AsByteArray;
            }
            catch
            {
                return null;
            }
        }

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

        private static string? GetSessionId(IWebDriver driver)
        {
            if (driver is IWrapsDriver wrapsDriver)
            {
                driver = wrapsDriver.WrappedDriver;
            }

            return driver is IHasSessionId hasSessionId
                ? hasSessionId.SessionId?.ToString()
                : null;
        }
    }
}
