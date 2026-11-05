using System.Threading.Tasks;
using NUnit.Framework;
using QualiFrame.Framework.Security;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Tests.Examples
{
    /// <summary>
    /// Example test demonstrating OWASP ZAP security testing.
    /// </summary>
    [TestFixture]
    public class SecurityTest
    {
        private SecurityTester? _securityTester;

        [SetUp]
        public void SetUp()
        {
            _securityTester = new SecurityTester();
        }

        [TearDown]
        public void TearDown()
        {
            _securityTester?.Dispose();
        }

        [Test]
        public async Task SecurityScanExample()
        {
            var tester = _securityTester!;
            if (!tester.IsAvailable())
            {
                Assert.Inconclusive("OWASP ZAP is disabled or the server at Security:OWASPZAP:ZapApiUrl is not reachable.");
            }

            var url = "https://www.saucedemo.com";

            // Act
            var results = await tester.RunSecurityScan(url, runSpider: true, runActiveScan: true);

            // Assert
            Logger.Information("Security scan completed. Total alerts: {Alerts}, High risk: {HighRisk}",
                results.Alerts.Count, results.Vulnerabilities.HighRisk);

            Assert.That(results.Vulnerabilities.HighRisk, Is.EqualTo(0), 
                "No high-risk vulnerabilities should be found");

            // Generate report
            var reportPath = await tester.GenerateReportAsync(results, "HTML");
            Logger.Information("Security report generated: {Path}", reportPath);
        }
    }
}
