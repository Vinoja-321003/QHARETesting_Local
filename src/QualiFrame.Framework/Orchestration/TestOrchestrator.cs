using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Orchestration
{
    /// <summary>
    /// Orchestrates test execution with dependency management and parallel execution.
    /// </summary>
    public class TestOrchestrator
    {
        private readonly TestDependencyGraph _dependencyGraph;
        private readonly TestAnalytics _analytics;
        private readonly Dictionary<string, Func<Task<bool>>> _testActions;
        private readonly Dictionary<string, TestExecutionResult> _executionResults;

        public TestOrchestrator()
        {
            _dependencyGraph = new TestDependencyGraph();
            _analytics = new TestAnalytics();
            _testActions = new Dictionary<string, Func<Task<bool>>>();
            _executionResults = new Dictionary<string, TestExecutionResult>();
        }

        /// <summary>
        /// Registers a test with its execution action.
        /// </summary>
        public void RegisterTest(string testName, Func<Task<bool>> testAction)
        {
            _testActions[testName] = testAction;
            _dependencyGraph.AddTest(testName);
            Logger.Debug("Test registered: {TestName}", testName);
        }

        /// <summary>
        /// Registers a test dependency.
        /// </summary>
        public void RegisterDependency(string testName, string prerequisite)
        {
            _dependencyGraph.AddDependency(testName, prerequisite);
        }

        /// <summary>
        /// Executes all tests respecting dependencies.
        /// </summary>
        public async Task<OrchestrationResult> ExecuteAllAsync(int maxParallelism = 4)
        {
            Logger.Information("Starting test orchestration with max parallelism: {MaxParallelism}", maxParallelism);

            if (!_dependencyGraph.ValidateGraph())
            {
                throw new InvalidOperationException("Dependency graph contains cycles");
            }

            var executionOrder = _dependencyGraph.GetExecutionOrder();
            var executed = new HashSet<string>();
            var totalStartTime = DateTime.UtcNow;

            foreach (var test in executionOrder)
            {
                if (executed.Contains(test))
                {
                    continue;
                }

                var parallelizable = GetParallelizableAtLevel(test, executed);
                
                if (parallelizable.Count == 1)
                {
                    await ExecuteTestAsync(parallelizable[0]);
                    executed.Add(parallelizable[0]);
                }
                else
                {
                    var tasks = parallelizable
                        .Take(maxParallelism)
                        .Select(t => ExecuteTestAsync(t))
                        .ToArray();

                    await Task.WhenAll(tasks);

                    foreach (var t in parallelizable.Take(maxParallelism))
                    {
                        executed.Add(t);
                    }
                }
            }

            var totalDuration = DateTime.UtcNow - totalStartTime;

            var result = new OrchestrationResult
            {
                TotalTests = _executionResults.Count,
                Passed = _executionResults.Values.Count(r => r.Status == "Passed"),
                Failed = _executionResults.Values.Count(r => r.Status == "Failed"),
                Skipped = _executionResults.Values.Count(r => r.Status == "Skipped"),
                TotalDuration = totalDuration,
                ExecutionResults = _executionResults.Values.ToList()
            };

            Logger.Information("Test orchestration completed: {Passed}/{Total} passed in {Duration}ms",
                result.Passed, result.TotalTests, totalDuration.TotalMilliseconds);

            return result;
        }

        /// <summary>
        /// Executes a single test.
        /// </summary>
        public async Task<TestExecutionResult> ExecuteTestAsync(string testName)
        {
            if (!_testActions.ContainsKey(testName))
            {
                throw new ArgumentException($"Test not registered: {testName}", nameof(testName));
            }

            var prerequisites = _dependencyGraph.GetPrerequisites(testName);
            foreach (var prerequisite in prerequisites)
            {
                if (!_executionResults.ContainsKey(prerequisite) ||
                    _executionResults[prerequisite].Status != "Passed")
                {
                    var skippedResult = new TestExecutionResult
                    {
                        TestName = testName,
                        Status = "Skipped",
                        Duration = TimeSpan.Zero,
                        ErrorMessage = $"Prerequisite '{prerequisite}' did not pass"
                    };

                    _executionResults[testName] = skippedResult;
                    _analytics.RecordTestExecution(testName, "Skipped", TimeSpan.Zero, skippedResult.ErrorMessage);
                    return skippedResult;
                }
            }

            var startTime = DateTime.UtcNow;
            Logger.Information("Executing test: {TestName}", testName);

            try
            {
                var testAction = _testActions[testName];
                var passed = await testAction();

                var duration = DateTime.UtcNow - startTime;
                var status = passed ? "Passed" : "Failed";

                var result = new TestExecutionResult
                {
                    TestName = testName,
                    Status = status,
                    Duration = duration,
                    ErrorMessage = passed ? null : "Test assertion failed"
                };

                _executionResults[testName] = result;
                _analytics.RecordTestExecution(testName, status, duration, result.ErrorMessage);

                Logger.Information("Test {TestName} {Status} in {Duration}ms", testName, status, duration.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var result = new TestExecutionResult
                {
                    TestName = testName,
                    Status = "Failed",
                    Duration = duration,
                    ErrorMessage = ex.Message
                };

                _executionResults[testName] = result;
                _analytics.RecordTestExecution(testName, "Failed", duration, ex.Message);

                Logger.Error(ex, "Test {TestName} failed: {Error}", testName, ex.Message);
                return result;
            }
        }

        /// <summary>
        /// Gets execution results for a test.
        /// </summary>
        public TestExecutionResult? GetResult(string testName)
        {
            return _executionResults.ContainsKey(testName) ? _executionResults[testName] : null;
        }

        /// <summary>
        /// Gets all execution results.
        /// </summary>
        public Dictionary<string, TestExecutionResult> GetAllResults()
        {
            return new Dictionary<string, TestExecutionResult>(_executionResults);
        }

        /// <summary>
        /// Generates analytics report.
        /// </summary>
        public AnalyticsReport GenerateAnalyticsReport()
        {
            return _analytics.GenerateReport();
        }

        private List<string> GetParallelizableAtLevel(string test, HashSet<string> executed)
        {
            var result = new List<string> { test };

            var prerequisites = _dependencyGraph.GetPrerequisites(test);
            var allPrerequisitesMet = prerequisites.All(p => executed.Contains(p));

            if (allPrerequisitesMet)
            {
                var candidates = _testActions.Keys
                    .Where(t => !executed.Contains(t) && t != test)
                    .Where(t =>
                    {
                        var tPrereqs = _dependencyGraph.GetPrerequisites(t);
                        return tPrereqs.All(p => executed.Contains(p));
                    })
                    .ToList();

                result.AddRange(candidates);
            }

            return result;
        }
    }

    /// <summary>
    /// Test execution result.
    /// </summary>
    public class TestExecutionResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Orchestration result.
    /// </summary>
    public class OrchestrationResult
    {
        public int TotalTests { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<TestExecutionResult> ExecutionResults { get; set; } = new List<TestExecutionResult>();
    }
}
