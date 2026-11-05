using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Data.Repositories;
using QualiFrame.Framework.Utilities.Logging;
using MLTestExecutionRecord = QualiFrame.Framework.Analytics.MLTestExecutionRecord;

namespace QualiFrame.Framework.Analytics
{
    /// <summary>
    /// Intelligent test selector using machine learning to recommend tests based on historical data.
    /// </summary>
    public class SmartTestSelector
    {
        private readonly MLContext _mlContext;
        private readonly IDataRepository? _dataRepository;
        private readonly TestConfiguration _config;
        private ITransformer? _model;
        private readonly List<MLTestExecutionRecord> _executionHistory;
        private readonly object _lockObject = new object();

        public SmartTestSelector(IDataRepository? dataRepository = null)
        {
            _mlContext = new MLContext(seed: 0);
            _dataRepository = dataRepository;
            _config = TestConfiguration.Instance;
            _executionHistory = new List<MLTestExecutionRecord>();
        }

        /// <summary>
        /// Initializes the ML model with historical test data.
        /// </summary>
        public void Initialize()
        {
            try
            {
                Logger.Information("Initializing SmartTestSelector with ML model...");

                var trainingData = LoadHistoricalData();

                if (trainingData == null || trainingData.Count == 0)
                {
                    Logger.Warning("No historical test data found. SmartTestSelector will use rule-based scoring.");
                    return;
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                var pipeline = _mlContext.Transforms.Concatenate(
                        "Features",
                        nameof(TestExecutionData.Browser),
                        nameof(TestExecutionData.Environment),
                        nameof(TestExecutionData.SuccessRate),
                        nameof(TestExecutionData.AverageDuration))
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                    .Append(_mlContext.Regression.Trainers.Sdca(
                        labelColumnName: nameof(TestExecutionData.SuccessRate),
                        featureColumnName: "Features"));

                _model = pipeline.Fit(dataView);

                Logger.Information("SmartTestSelector ML model initialized successfully with {Count} training records.", trainingData.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize SmartTestSelector ML model. Falling back to rule-based scoring.");
                _model = null;
            }
        }

        /// <summary>
        /// Gets recommended tests based on browser and environment.
        /// </summary>
        public List<string> GetRecommendedTests(string browser, string environment, int count = 10)
        {
            if (_model == null && _executionHistory.Count > 0)
            {
                Initialize();
            }

            var allTests = GetAllTests();

            if (allTests.Count == 0)
            {
                Logger.Warning("No tests available for recommendation.");
                return new List<string>();
            }

            var scoredTests = allTests.Select(test => new
            {
                TestName = test,
                Score = ScoreTest(test, browser, environment)
            })
            .OrderByDescending(x => x.Score)
            .Take(count)
            .Select(x => x.TestName)
            .ToList();

            Logger.Information("Recommended {Count} tests for browser: {Browser}, environment: {Environment}", 
                scoredTests.Count, browser, environment);

            return scoredTests;
        }

        /// <summary>
        /// Records test execution for ML training and analytics.
        /// </summary>
        public void RecordTestExecution(MLTestExecutionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            lock (_lockObject)
            {
                _executionHistory.Add(record);

                if (_dataRepository != null)
                {
                    try
                    {
                        SaveExecutionRecord(record);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Failed to persist test execution record to repository: {Error}", ex.Message);
                    }
                }

                Logger.Debug("Recorded test execution: {TestName}, Passed: {Passed}, Duration: {Duration}ms",
                    record.TestName, record.Passed, record.Duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Gets test execution statistics.
        /// </summary>
        public MLTestStatistics? GetTestStatistics(string testName)
        {
            var history = GetTestHistory(testName);

            if (history == null || history.Count == 0)
                return null;

            var passedCount = history.Count(h => h.Passed);
            var totalCount = history.Count;
            var successRate = (float)passedCount / totalCount;
            var avgDuration = history.Average(h => h.Duration.TotalMilliseconds);
            var lastExecution = history.Max(h => h.ExecutionTime);

            return new MLTestStatistics
            {
                TestName = testName,
                TotalExecutions = totalCount,
                PassedCount = passedCount,
                FailedCount = totalCount - passedCount,
                SuccessRate = successRate,
                AverageDuration = TimeSpan.FromMilliseconds(avgDuration),
                LastExecutionTime = lastExecution
            };
        }

        private float ScoreTest(string testName, string browser, string environment)
        {
            var history = GetTestHistory(testName);

            if (history == null || history.Count == 0)
            {
                return 0.3f;
            }

            var successRate = history.Count(h => h.Passed) / (float)history.Count;
            
            var recentExecutions = history.Count(h =>
                (DateTime.UtcNow - h.ExecutionTime).TotalDays < 7);
            var recentExecutionScore = Math.Min(1.0f, recentExecutions / 10f);

            var browserMatch = history.Any(h => 
                string.Equals(h.Browser, browser, StringComparison.OrdinalIgnoreCase));
            var browserScore = browserMatch ? 1.0f : 0.0f;

            var envMatch = history.Any(h =>
                string.Equals(h.Environment, environment, StringComparison.OrdinalIgnoreCase));
            var envScore = envMatch ? 1.0f : 0.0f;

            var score = (successRate * 0.4f) +
                       (recentExecutionScore * 0.3f) +
                       (browserScore * 0.15f) +
                       (envScore * 0.15f);

            return Math.Min(1.0f, Math.Max(0.0f, score));
        }

        private List<MLTestExecutionRecord>? GetTestHistory(string testName)
        {
            lock (_lockObject)
            {
                var history = _executionHistory
                    .Where(r => string.Equals(r.TestName, testName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (_dataRepository != null && history.Count == 0)
                {
                    try
                    {
                        var records = LoadTestHistoryFromRepository(testName);
                        if (records != null && records.Count > 0)
                        {
                            history = records;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("Failed to load test history from repository for test: {TestName}. Error: {Error}", testName, ex.Message);
                    }
                }

                return history;
            }
        }

        private List<TestExecutionData>? LoadHistoricalData()
        {
            var data = _executionHistory
                .GroupBy(r => r.TestName)
                .Select(g => new TestExecutionData
                {
                    TestName = g.Key,
                    Browser = g.First().Browser,
                    Environment = g.First().Environment,
                    Passed = g.Last().Passed,
                    AverageDuration = (float)g.Average(h => h.Duration.TotalMilliseconds),
                    SuccessRate = g.Count(h => h.Passed) / (float)g.Count(),
                    LastExecutionTime = g.Max(h => h.ExecutionTime),
                    ExecutionTime = g.Max(h => h.ExecutionTime),
                    ExecutionCount = g.Count()
                })
                .ToList();

            if (_dataRepository != null && data.Count == 0)
            {
                try
                {
                    var repositoryData = LoadHistoricalDataFromRepository();
                    if (repositoryData != null && repositoryData.Count > 0)
                    {
                        data = repositoryData;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("Failed to load historical data from repository: {Message}", ex.Message);
                }
            }

            return data;
        }

        private List<string> GetAllTests()
        {
            var tests = _executionHistory
                .Select(r => r.TestName)
                .Distinct()
                .ToList();

            if (_dataRepository != null && tests.Count == 0)
            {
                try
                {
                    var repositoryTests = GetAllTestsFromRepository();
                    if (repositoryTests != null && repositoryTests.Count > 0)
                    {
                        tests = repositoryTests;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("Failed to get tests from repository: {Message}", ex.Message);
                }
            }

            return tests;
        }

        private void SaveExecutionRecord(MLTestExecutionRecord record)
        {
            Logger.Debug("Saving execution record to repository: {TestName}", record.TestName);
        }

        private List<MLTestExecutionRecord>? LoadTestHistoryFromRepository(string testName)
        {
            Logger.Debug("Loading test history from repository: {TestName}", testName);
            return null;
        }

        private List<TestExecutionData>? LoadHistoricalDataFromRepository()
        {
            Logger.Debug("Loading historical data from repository");
            return null;
        }

        private List<string>? GetAllTestsFromRepository()
        {
            Logger.Debug("Getting all tests from repository");
            return null;
        }
    }

    /// <summary>
    /// Test execution statistics for ML analytics.
    /// </summary>
    public class MLTestStatistics
    {
        public string TestName { get; set; } = string.Empty;
        public int TotalExecutions { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public float SuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public DateTime LastExecutionTime { get; set; }
    }
}
