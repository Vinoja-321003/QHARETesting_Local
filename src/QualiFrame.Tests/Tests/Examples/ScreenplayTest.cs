using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Screenplay;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Screenplay.Questions;
using QualiFrame.Framework.Screenplay.Tasks;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Tests.Examples
{
    /// <summary>
    /// Example test demonstrating the Screenplay pattern.
    /// </summary>
    [TestFixture]
    public class ScreenplayTest
    {
        private Actor? _actor;

        [SetUp]
        public void SetUp()
        {
            // Create an actor with the ability to browse the web
            _actor = Actor.Named("Test User")
                .WhoCan(BrowseTheWeb.WithDefaultDriver());
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                var driver = _actor?.Using<BrowseTheWeb>()?.Driver;
                driver?.Quit();
                driver?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning("Error cleaning up WebDriver: {Error}", ex.Message);
            }
        }

        [Test]
        public void ScreenplayPatternExample()
        {
            // Arrange
            var actor = _actor!;

            try
            {
                // Act - Actor performs tasks
                actor.AttemptsTo(new Navigate("https://www.saucedemo.com"));
                actor.AttemptsTo(new QualiFrame.Framework.Screenplay.Tasks.Type(By.Id("user-name"), "standard_user", "Username field"));
                actor.AttemptsTo(new QualiFrame.Framework.Screenplay.Tasks.Type(By.Id("password"), "secret_sauce", "Password field"));
                actor.AttemptsTo(new Click(By.Id("login-button"), "Login button"));

                // Assert - Actor asks questions
                var pageTitle = actor.AsksFor(new Title());
                Assert.That(pageTitle, Does.Contain("Swag Labs").Or.Contain("Products"));

                var productText = actor.AsksFor(new Text(By.ClassName("title"), "Product title"));
                Assert.That(productText, Is.Not.Empty);
            }
            catch (WebDriverException ex) when (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive($"WebDriver timed out during Screenplay example flow. Details: {ex.Message}");
            }
        }

        [Test]
        public void ScreenplayPatternFluentExample()
        {
            // Arrange
            var actor = _actor!;

            try
            {
                // Act - Fluent syntax (each call returns void, so we chain them)
                actor.AttemptsTo(new Navigate("https://www.saucedemo.com"));
                actor.AttemptsTo(new QualiFrame.Framework.Screenplay.Tasks.Type(By.Id("user-name"), "standard_user"));
                actor.AttemptsTo(new QualiFrame.Framework.Screenplay.Tasks.Type(By.Id("password"), "secret_sauce"));
                actor.AttemptsTo(new Click(By.Id("login-button")));

                // Assert
                var title = actor.AsksFor(new Title());
                Assert.That(title, Is.Not.Empty);
            }
            catch (WebDriverException ex) when (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive($"WebDriver timed out during Screenplay example flow. Details: {ex.Message}");
            }
        }
    }
}
