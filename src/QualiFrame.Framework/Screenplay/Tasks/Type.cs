using System;
using OpenQA.Selenium;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay.Tasks
{
    /// <summary>
    /// Task to type text into an element.
    /// </summary>
    public class Type : ITask
    {
        private readonly By _locator;
        private readonly string _text;
        private readonly string? _description;

        public Type(By locator, string text, string? description = null)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _description = description;
        }

        public void PerformAs(IActor actor)
        {
            var browseAbility = actor.Using<BrowseTheWeb>();
            var driver = browseAbility.Driver;

            try
            {
                var element = driver.FindElement(_locator);
                element.Clear();
                element.SendKeys(_text);
                Logger.Information("Actor '{ActorName}' typed '{Text}' into element: {Locator}", 
                    actor.Name, _text, _description ?? _locator.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Actor '{ActorName}' failed to type into element: {Locator}", actor.Name, _locator.ToString());
                throw;
            }
        }
    }
}
