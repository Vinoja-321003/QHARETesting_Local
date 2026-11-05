using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Pages;
using QualiFrame.Framework.Desktop.Windows;

namespace QualiFrame.Framework.Desktop.Flows
{
    /// <summary>
    /// High-level reusable business flows for desktop login scenarios.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DesktopLoginFlows
    {
        private readonly DesktopSession _session;
        private readonly LoginWindow _loginWindow;
        private readonly MainWindow _mainWindow;

        public DesktopLoginFlows()
        {
            _session = DesktopSessionFactory.GetSession();
            _loginWindow = new LoginWindow(_session);
            _mainWindow = new MainWindow(_session);
        }

        public void LoginAs(string username, string password)
        {
            _loginWindow.WaitForLoaded();
            _loginWindow.EnterCredentials(username, password);
            _loginWindow.ClickLogin();

            WindowManager.SwitchToLatestWindow(_session);

            _mainWindow.WaitForLoaded();
        }

        public bool IsMainWindowVisible()
        {
            return _mainWindow.IsVisible();
        }
    }
}
