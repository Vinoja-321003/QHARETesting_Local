using System.Collections.Generic;
using OpenQA.Selenium;

namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Interface for keyword actions in keyword-driven testing.
    /// </summary>
    public interface IKeyword
    {
        /// <summary>
        /// Gets the keyword name.
        /// </summary>
        string KeywordName { get; }

        /// <summary>
        /// Executes the keyword action.
        /// </summary>
        /// <param name="driver">The WebDriver instance.</param>
        /// <param name="parameters">Parameters for the keyword.</param>
        /// <returns>Result of the keyword execution.</returns>
        KeywordResult Execute(IWebDriver driver, Dictionary<string, object> parameters);
    }
}
