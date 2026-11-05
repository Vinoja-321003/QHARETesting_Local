using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Orchestration
{
    /// <summary>
    /// Manages test dependencies and execution order.
    /// </summary>
    public class TestDependencyGraph
    {
        private readonly Dictionary<string, List<string>> _dependencies;
        private readonly Dictionary<string, List<string>> _dependents;
        private readonly HashSet<string> _tests;

        public TestDependencyGraph()
        {
            _dependencies = new Dictionary<string, List<string>>();
            _dependents = new Dictionary<string, List<string>>();
            _tests = new HashSet<string>();
        }

        /// <summary>
        /// Adds a test to the graph.
        /// </summary>
        public void AddTest(string testName)
        {
            _tests.Add(testName);
            if (!_dependencies.ContainsKey(testName))
            {
                _dependencies[testName] = new List<string>();
            }
            if (!_dependents.ContainsKey(testName))
            {
                _dependents[testName] = new List<string>();
            }
        }

        /// <summary>
        /// Adds a dependency: test depends on prerequisite.
        /// </summary>
        public void AddDependency(string testName, string prerequisite)
        {
            AddTest(testName);
            AddTest(prerequisite);

            if (!_dependencies[testName].Contains(prerequisite))
            {
                _dependencies[testName].Add(prerequisite);
            }

            if (!_dependents[prerequisite].Contains(testName))
            {
                _dependents[prerequisite].Add(testName);
            }

            Logger.Debug("Dependency added: {Test} depends on {Prerequisite}", testName, prerequisite);
        }

        /// <summary>
        /// Gets execution order using topological sort.
        /// </summary>
        public List<string> GetExecutionOrder()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var test in _tests)
            {
                if (!visited.Contains(test))
                {
                    if (HasCycle(test, visited, visiting))
                    {
                        throw new InvalidOperationException($"Circular dependency detected involving test: {test}");
                    }
                    TopologicalSort(test, visited, result);
                }
            }

            Logger.Information("Execution order determined: {Count} tests", result.Count);
            return result;
        }

        /// <summary>
        /// Gets tests that can be executed in parallel (no dependencies).
        /// </summary>
        public List<string> GetParallelizableTests()
        {
            return _tests
                .Where(test => !_dependencies.ContainsKey(test) || _dependencies[test].Count == 0)
                .ToList();
        }

        /// <summary>
        /// Gets prerequisites for a test.
        /// </summary>
        public List<string> GetPrerequisites(string testName)
        {
            return _dependencies.ContainsKey(testName)
                ? _dependencies[testName].ToList()
                : new List<string>();
        }

        /// <summary>
        /// Gets tests that depend on a given test.
        /// </summary>
        public List<string> GetDependents(string testName)
        {
            return _dependents.ContainsKey(testName)
                ? _dependents[testName].ToList()
                : new List<string>();
        }

        /// <summary>
        /// Checks if test has dependencies.
        /// </summary>
        public bool HasDependencies(string testName)
        {
            return _dependencies.ContainsKey(testName) && _dependencies[testName].Count > 0;
        }

        /// <summary>
        /// Validates the dependency graph for cycles.
        /// </summary>
        public bool ValidateGraph()
        {
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var test in _tests)
            {
                if (HasCycle(test, visited, visiting))
                {
                    return false;
                }
            }

            return true;
        }

        private void TopologicalSort(string test, HashSet<string> visited, List<string> result)
        {
            if (visited.Contains(test))
            {
                return;
            }

            visited.Add(test);

            if (_dependencies.ContainsKey(test))
            {
                foreach (var prerequisite in _dependencies[test])
                {
                    TopologicalSort(prerequisite, visited, result);
                }
            }

            result.Add(test);
        }

        private bool HasCycle(string test, HashSet<string> visited, HashSet<string> visiting)
        {
            if (visiting.Contains(test))
            {
                return true;
            }

            if (visited.Contains(test))
            {
                return false;
            }

            visiting.Add(test);

            if (_dependencies.ContainsKey(test))
            {
                foreach (var prerequisite in _dependencies[test])
                {
                    if (HasCycle(prerequisite, visited, visiting))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(test);
            visited.Add(test);

            return false;
        }
    }
}
