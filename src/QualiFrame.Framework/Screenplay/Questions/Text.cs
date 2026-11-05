using System;
using OpenQA.Selenium;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay.Questions
{
    /// <summary>
    /// Question to get the text of an element.
    /// </summary>
    public class Text : IQuestion<string>
    {
        private readonly By _locator;
        private readonly string? _description;

        public Text(By locator, string? description = null)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _description = description;
        }

        public string AnsweredBy(IActor actor)
        {
            var browseAbility = actor.Using<BrowseTheWeb>();
            var driver = browseAbility.Driver;

            try
            {
                var element = driver.FindElement(_locator);
                var text = element.Text;
                Logger.Debug("Actor '{ActorName}' retrieved text '{Text}' from element: {Locator}", 
                    actor.Name, text, _description ?? _locator.ToString());
                return text;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Actor '{ActorName}' failed to get text from element: {Locator}", actor.Name, _locator.ToString());
                throw;
            }
        }
    }
}
