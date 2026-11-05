using System;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay.Questions
{
    /// <summary>
    /// Question to get the page title.
    /// </summary>
    public class Title : IQuestion<string>
    {
        public string AnsweredBy(IActor actor)
        {
            var browseAbility = actor.Using<BrowseTheWeb>();
            var driver = browseAbility.Driver;

            try
            {
                var title = driver.Title;
                Logger.Debug("Actor '{ActorName}' retrieved page title: {Title}", actor.Name, title);
                return title;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Actor '{ActorName}' failed to get page title", actor.Name);
                throw;
            }
        }
    }
}
