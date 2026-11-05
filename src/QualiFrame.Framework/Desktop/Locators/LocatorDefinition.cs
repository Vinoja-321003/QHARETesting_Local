namespace QualiFrame.Framework.Desktop.Locators
{
    /// <summary>
    /// Defines multiple locator strategies for a desktop UI element.
    /// Enables self-healing by trying alternative locator types when one fails.
    /// AccessibilityId maps to UIA AutomationId.
    /// </summary>
    public class LocatorDefinition
    {
        /// <summary>
        /// UIA AutomationId. Kept as AccessibilityId for compatibility with existing page objects.
        /// </summary>
        public string? AccessibilityId { get; set; }

        /// <summary>
        /// Optional alias for <see cref="AccessibilityId"/>.
        /// </summary>
        public string? AutomationId { get; set; }

        public string? Name { get; set; }

        public string? ClassName { get; set; }

        /// <summary>
        /// Optional UIA control type name (e.g. Button, Edit, DataGrid).
        /// </summary>
        public string? ControlType { get; set; }

        public string? ResolvedAutomationId =>
            !string.IsNullOrWhiteSpace(AccessibilityId) ? AccessibilityId : AutomationId;
    }
}
