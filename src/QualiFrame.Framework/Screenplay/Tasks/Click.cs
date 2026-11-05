using System;
using OpenQA.Selenium;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay.Tasks
{
    /// <summary>
    /// Task to click on an element.
    /// </summary>
    public class Click : ITask
    {
        private readonly By _locator;
        private readonly string? _description;

        public Click(By locator, string? description = null)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _description = description;
        }

        public void PerformAs(IActor actor)
        {
            var browseAbility = actor.Using<BrowseTheWeb>();
            var driver = browseAbility.Driver;

            try
            {
                var element = driver.FindElement(_locator);
                element.Click();
                Logger.Information("Actor '{ActorName}' clicked on element: {Locator}", actor.Name, _description ?? _locator.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Actor '{ActorName}' failed to click on element: {Locator}", actor.Name, _locator.ToString());
                throw;
            }
        }
    }
}
