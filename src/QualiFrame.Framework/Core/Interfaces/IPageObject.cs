namespace QualiFrame.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for page object implementations.
    /// Note: Driver property is intentionally not included as it's typically protected in Page Object Model.
    /// </summary>
    public interface IPageObject
    {
        /// <summary>
        /// Waits for the page to load.
        /// </summary>
        void WaitForPageToLoad();

        /// <summary>
        /// Checks if the page is loaded.
        /// </summary>
        bool IsPageLoaded();
    }
}
