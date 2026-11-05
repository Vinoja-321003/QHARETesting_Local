using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Drivers;

namespace QualiFrame.Framework.Desktop.Windows
{
    /// <summary>
    /// Helper for switching between desktop windows (e.g., dialogs, child windows).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowManager
    {
        public static void SwitchToLatestWindow(DesktopSession session)
        {
            session.SwitchToLatestWindow();
        }
    }
}
