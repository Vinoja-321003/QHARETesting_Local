using System;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay.Tasks
{
    /// <summary>
    /// Task to navigate to a URL.
    /// </summary>
    public class Navigate : ITask
    {
        private readonly string _url;

        public Navigate(string url)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public void PerformAs(IActor actor)
        {
            var browseAbility = actor.Using<BrowseTheWeb>();
            var driver = browseAbility.Driver;

            try
            {
                driver.Navigate().GoToUrl(_url);
                Logger.Information("Actor '{ActorName}' navigated to: {Url}", actor.Name, _url);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Actor '{ActorName}' failed to navigate to: {Url}", actor.Name, _url);
                throw;
            }
        }
    }
}
