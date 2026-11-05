using System.Net;
using System.Net.Http;
using System.Text;
using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Tests.Tests.Unit
{
    [TestFixture]
    public class HealeniumClientTests
    {
        [Test]
        public void LocatorMapper_SplitAndRoundTrip_ShouldPreserveId()
        {
            var original = By.Id("username");
            var (type, value) = HealeniumLocatorMapper.Split(original);

            Assert.That(value, Is.EqualTo("username"));
            Assert.That(type, Does.Contain("Id").IgnoreCase);

            var restored = HealeniumLocatorMapper.FromTypeAndValue(type, value);
            Assert.That(restored.ToString(), Is.EqualTo(original.ToString()));
        }

        [Test]
        public void LocatorMapper_CandidatesFromNode_ShouldPreferIdAndName()
        {
            var node = new HealeniumNode
            {
                Tag = "input",
                Id = "email",
                Other = new Dictionary<string, string> { ["name"] = "email", ["type"] = "email" },
                Classes = new List<string> { "form-control" }
            };

            var candidates = HealeniumLocatorMapper.CandidatesFromNode(node, 10);

            Assert.That(candidates.Any(c => c.ToString().Contains("email")), Is.True);
            Assert.That(candidates.Count, Is.GreaterThan(1));
        }

        [Test]
        public void LocatorMapper_ParseReportedLocator_ShouldReadHealeniumSelectorAllFormat()
        {
            var (type, value) = HealeniumLocatorMapper.ParseReportedLocator("By.CssSelector(body#sauce-demo)");

            Assert.That(type, Does.Contain("CssSelector").IgnoreCase);
            Assert.That(value, Is.EqualTo("body#sauce-demo"));
        }

        [Test]
        public void LocatorMapper_RelatedSelectors_ShouldMatchBrokenShopifyLocators()
        {
            var selectors = new List<HealeniumStoredSelector>
            {
                new() { Locator = "By.CssSelector(body#sauce-demo)", Url = "https://sauce-demo.myshopify.com/" },
                new() { Locator = "By.Id(main-menu)", Url = "https://sauce-demo.myshopify.com/" },
                new() { Locator = "By.CssSelector(#page-content a[id^='product-'][href*='/products/'])", Url = "https://sauce-demo.myshopify.com/" },
                new() { Locator = "By.xpath(//input[@id='user-name'])", Url = "https://www.saucedemo.com/" }
            };

            Assert.That(
                HealeniumLocatorMapper.LocatorSimilarity("By.CssSelector", "body#sauce-", "By.CssSelector", "body#sauce-demo"),
                Is.GreaterThanOrEqualTo(HealeniumLocatorMapper.RelatedLocatorMinSimilarity));
            Assert.That(
                HealeniumLocatorMapper.LocatorSimilarity("By.Id", "main-", "By.Id", "main-menu"),
                Is.GreaterThanOrEqualTo(HealeniumLocatorMapper.RelatedLocatorMinSimilarity));
            Assert.That(
                HealeniumLocatorMapper.LocatorSimilarity(
                    "By.CssSelector",
                    "#page-content a[id^='dummy-'][href*='/dummy/']",
                    "By.CssSelector",
                    "#page-content a[id^='product-'][href*='/products/']"),
                Is.GreaterThanOrEqualTo(HealeniumLocatorMapper.RelatedLocatorMinSimilarity));

            var related = HealeniumLocatorMapper.RankRelatedSelectors(
                selectors, "By.CssSelector", "body#sauce-", "https://sauce-demo.myshopify.com/");

            Assert.That(related, Has.Count.EqualTo(1));
            Assert.That(HealeniumLocatorMapper.Split(related[0].Locator).Value, Is.EqualTo("body#sauce-demo"));
        }

        [Test]
        public void Node_ShouldSerializeClassesAsSpaceSeparatedString()
        {
            var node = new HealeniumNode
            {
                Tag = "button",
                Classes = new List<string> { "btn", "primary" }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(node, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            Assert.That(json, Does.Contain("\"classes\":\"btn primary\""));
            Assert.That(json, Does.Not.Contain("\"classes\":["));
        }

        [Test]
        public void Client_InitReportAndSaveSelector_ShouldCallOfficialPaths()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, "healenium/report/test-session");
            var backend = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:7878/") };
            var imitator = new HttpClient(new RecordingHandler(HttpStatusCode.OK, "[]"))
            {
                BaseAddress = new Uri("http://localhost:8000/")
            };

            using var client = new HealeniumClient(new HealeniumConfiguration { Enabled = true }, backend, imitator);
            var report = client.InitReport("test-session");
            client.SaveSelector(new HealeniumSelectorRequest
            {
                Type = "By.Id",
                Locator = "username",
                ClassName = "LoginPage",
                MethodName = "EnterCredentials",
                Command = "findElement",
                Url = "https://example.test/"
            });

            Assert.That(report, Does.Contain("healenium/report"));
            Assert.That(handler.Paths, Does.Contain("/healenium/report/init/test-session"));
            Assert.That(handler.Paths, Does.Contain("/healenium"));
            Assert.That(handler.Methods, Does.Contain("POST"));
        }

        [Test]
        public void Client_SaveHealing_ShouldPostSessionKeyHeader()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, string.Empty);
            var backend = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:7878/") };
            var imitator = new HttpClient(new RecordingHandler(HttpStatusCode.OK, "[]"))
            {
                BaseAddress = new Uri("http://localhost:8000/")
            };

            using var client = new HealeniumClient(new HealeniumConfiguration { Enabled = true }, backend, imitator);
            var saved = client.SaveHealing(new HealeniumHealingRequest
            {
                Locator = "username",
                Type = "By.Id",
                Command = "findElement",
                Url = "https://example.test/",
                Results = new List<HealeniumHealingResult>
                {
                    new()
                    {
                        Locator = new HealeniumLocatorDto { Type = "By.CssSelector", Value = "input[type='email']" },
                        Score = 0.9
                    }
                },
                UsedResult = new HealeniumHealingResult
                {
                    Locator = new HealeniumLocatorDto { Type = "By.CssSelector", Value = "input[type='email']" },
                    Score = 0.9
                }
            }, "session-123");

            Assert.That(saved, Is.True);
            Assert.That(handler.Paths, Does.Contain("/healenium/healing"));
            Assert.That(handler.SessionKeys, Does.Contain("session-123"));
        }

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
            public List<string> Methods { get; } = new();
            public List<string> SessionKeys { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Methods.Add(request.Method.Method);
                Paths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
                if (request.Headers.TryGetValues("sessionKey", out var keys))
                {
                    SessionKeys.AddRange(keys);
                }

                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
