using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Tests.Tests.Unit
{
    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class HealeniumEvidenceTests
    {
        private string _root = string.Empty;
        private HealeniumEvidenceStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "qualiframe-healenium-evidence", Guid.NewGuid().ToString("N"));
            _store = new HealeniumEvidenceStore(_root);
            HealeniumEvidenceContext.Unbind();
        }

        [TearDown]
        public void TearDown()
        {
            HealeniumEvidenceContext.Unbind();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Test]
        public void Scorer_ShouldPreferMatchingIdAndTag()
        {
            var historical = new HealeniumNode
            {
                Tag = "button",
                Id = "login-button",
                Other = new Dictionary<string, string> { ["type"] = "submit" },
                Classes = new List<string> { "btn" },
                InnerText = "Login"
            };
            var current = new HealeniumNode
            {
                Tag = "button",
                Id = "login-button",
                Other = new Dictionary<string, string> { ["type"] = "submit" },
                Classes = new List<string> { "btn" },
                InnerText = "Login"
            };
            var other = new HealeniumNode { Tag = "a", Id = "cart", InnerText = "Cart" };

            Assert.That(HealeniumEvidenceScorer.Score(historical, current), Is.GreaterThanOrEqualTo(0.85));
            Assert.That(HealeniumEvidenceScorer.Score(historical, other), Is.LessThan(0.4));
        }

        [Test]
        public void Scorer_ShouldMatchEvolvedLocatorBySharedAttributes()
        {
            var historical = new HealeniumNode
            {
                Tag = "button",
                Id = "login-button",
                Other = new Dictionary<string, string> { ["type"] = "submit" },
                InnerText = "Login"
            };
            var evolved = new HealeniumNode
            {
                Tag = "button",
                Other = new Dictionary<string, string> { ["data-testid"] = "login", ["type"] = "submit" },
                InnerText = "Login"
            };

            var score = HealeniumEvidenceScorer.Score(historical, evolved);
            Assert.That(score, Is.GreaterThanOrEqualTo(HealeniumEvidenceScorer.DefaultScoreCap));
            Assert.That(HealeniumEvidenceScorer.DescribeMatches(historical, evolved), Does.Contain("tag=button"));
        }

        [Test]
        public void Store_ShouldCreateVersionedBaselineOnSuccessfulCapture()
        {
            var capture = SampleCapture("By.CssSelector", "button[id='login-button']", "login-button");

            var record = _store.CommitSuccess(capture, null);

            Assert.That(record.TrustedVersion, Is.EqualTo(1));
            Assert.That(record.History, Has.Count.EqualTo(1));
            Assert.That(record.History[0].TargetNode!.Id, Is.EqualTo("login-button"));
            Assert.That(_store.GetTrustedBaseline(capture.LocatorType, capture.LocatorValue, capture.Url), Is.Not.Null);
        }

        [Test]
        public void Store_ShouldAppendHealedBaselineWithoutRemovingHistory()
        {
            var original = SampleCapture("By.CssSelector", "button[id='login-button']", "login-button");
            _store.CommitSuccess(original, null);

            var healed = new PendingHealingCapture
            {
                Attempt = new HealingAttemptRecord
                {
                    OriginalLocatorType = original.LocatorType,
                    OriginalLocatorValue = original.LocatorValue,
                    HealedLocatorType = "By.CssSelector",
                    HealedLocatorValue = "button[data-testid='login']",
                    Confidence = 0.91,
                    Url = original.Url,
                    Healed = true
                },
                Snapshot = SampleCapture("By.CssSelector", "button[id='login-button']", null, "login")
            };
            healed.Snapshot.NodePath[0].Other["data-testid"] = "login";

            var record = _store.PromoteHealing(healed, null);

            Assert.That(record.History, Has.Count.EqualTo(2));
            Assert.That(record.TrustedVersion, Is.EqualTo(2));
            Assert.That(record.History[0].LocatorValue, Is.EqualTo("button[id='login-button']"));
            Assert.That(record.History[1].LocatorValue, Is.EqualTo("button[data-testid='login']"));
            Assert.That(record.History[1].Source, Is.EqualTo("healed"));
            Assert.That(record.History[1].PreviousLocatorValue, Is.EqualTo("button[id='login-button']"));
        }

        [Test]
        public void Store_ShouldNotPromoteHealingWhenStepFails()
        {
            var original = SampleCapture("By.CssSelector", "#submit", "submit");
            _store.CommitSuccess(original, null);

            var scope = new HealeniumEvidenceScope { Feature = "Login", Scenario = "Submit" };
            HealeniumEvidenceContext.Bind(scope);
            scope.BeginStep("When user clicks submit");
            scope.QueueHealing(new PendingHealingCapture
            {
                Attempt = new HealingAttemptRecord
                {
                    OriginalLocatorType = original.LocatorType,
                    OriginalLocatorValue = original.LocatorValue,
                    HealedLocatorType = "By.CssSelector",
                    HealedLocatorValue = "#other",
                    Confidence = 0.92,
                    Healed = true,
                    Url = original.Url
                },
                Snapshot = SampleCapture("By.CssSelector", "#submit", "other")
            });

            _store.CommitBoundStep(stepSucceeded: false);

            var record = _store.Get(original.LocatorType, original.LocatorValue, original.Url);
            Assert.That(record!.TrustedVersion, Is.EqualTo(1));
            Assert.That(record.History, Has.Count.EqualTo(1));
        }

        [Test]
        public void Store_ShouldNotCreateNewVersionForSameFingerprint()
        {
            var capture = SampleCapture("By.Id", "main-menu", "main-menu");
            _store.CommitSuccess(capture, null);
            _store.CommitSuccess(capture, null);

            var record = _store.Get(capture.LocatorType, capture.LocatorValue, capture.Url);
            Assert.That(record!.History, Has.Count.EqualTo(1));
            Assert.That(record.TrustedVersion, Is.EqualTo(1));
        }

        [Test]
        public void Store_FailedAttempt_ShouldWriteAuditWithoutBaseline()
        {
            _store.RecordFailedAttempt(new HealingAttemptRecord
            {
                OriginalLocatorType = "By.Id",
                OriginalLocatorValue = "missing",
                Url = "https://example.test/app",
                Confidence = 0.2,
                ScoreCap = 0.5
            });

            Assert.That(_store.Get("By.Id", "missing", "https://example.test/app"), Is.Null);
            var audits = Directory.GetFiles(Path.Combine(_root, "audits"), "healing-*.jsonl");
            Assert.That(audits, Is.Not.Empty);
            Assert.That(File.ReadAllText(audits[0]), Does.Contain("missing"));
        }

        [Test]
        public void Store_ShouldFindRelatedBaselineOnSamePage()
        {
            var working = SampleCapture("By.CssSelector", "body#sauce-demo", "sauce-demo");
            working.Url = "https://sauce-demo.myshopify.com/";
            working.NodePath[0].Tag = "body";
            _store.CommitSuccess(working, null);

            var related = _store.FindRelatedBaselines("By.CssSelector", "body#sauce-", "https://sauce-demo.myshopify.com/");

            Assert.That(related, Has.Count.EqualTo(1));
            Assert.That(related[0].LocatorValue, Is.EqualTo("body#sauce-demo"));
        }

        [Test]
        public void NormalizeUrl_ShouldDropQueryAndFragment()
        {
            var normalized = HealeniumEvidenceStore.NormalizeUrl("https://shop.example/products/grey-jacket?variant=1#reviews");
            Assert.That(normalized, Is.EqualTo("https://shop.example/products/grey-jacket"));
        }

        private static PendingLocatorCapture SampleCapture(string type, string value, string? id, string? fallbackId = null)
        {
            var node = new HealeniumNode
            {
                Tag = "button",
                Id = id,
                InnerText = "Login",
                Other = new Dictionary<string, string>()
            };
            if (!string.IsNullOrEmpty(fallbackId) && string.IsNullOrEmpty(id))
            {
                node.Other["data-testid"] = fallbackId;
            }

            return new PendingLocatorCapture
            {
                LocatorType = type,
                LocatorValue = value,
                Url = "https://example.test/login",
                NodePath = new List<HealeniumNode> { node },
                Attributes = node.Other
            };
        }
    }
}
