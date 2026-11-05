# Desktop Automation (FlaUI)

QualiFrame desktop automation uses **[FlaUI](https://github.com/FlaUI/FlaUI)** — a .NET wrapper around Microsoft UI Automation (UIA2 / UIA3). Sessions talk to Windows UI Automation in-process; no extra desktop driver server is required.

Desktop tests remain **Windows-only**. `@Desktop` scenarios are marked inconclusive on other operating systems.

---

## Architecture

| Layer | Types | Role |
|-------|--------|------|
| Session | `DesktopSessionFactory`, `DesktopSession` | Launch / attach the AUT, hold `Application` + `AutomationBase` + current `Window` |
| Locators | `LocatorDefinition`, `SelfHealingLocatorEngine` | AutomationId → Name → ClassName → ControlType |
| Elements | `DesktopElement` | Lazy, retry-safe wrapper around a FlaUI `AutomationElement` |
| Actions | `DesktopActions`, plus Grid / Tree / Menu / Tab / Dialog / Window / Ribbon / File / Tray helpers | Logged, retried, Allure-aware operations |
| Pages | `DesktopPage`, `CalculatorPage`, `LoginWindow`, `MainWindow` | Window/page objects |
| Tests | `Desktop.feature`, `DesktopSteps`, `CalculatorSteps`, `DesktopHooks` | BDD glue |

Call **`DesktopSessionFactory.GetSession()`** from hooks, page constructors, or generated desktop sessions. Do **not** construct `UIA3Automation` / `Application` in step classes.

---

## Configuration

Keys live under `Desktop` in `src/QualiFrame.Framework/Configuration/appsettings.json`:

| Key | Purpose |
|-----|---------|
| `AppId` | Win32/WPF: path or name of the `.exe`. UWP/Store: AUMID (contains `!`) |
| `Arguments` | Optional launch arguments |
| `WorkingDirectory` | Optional working directory for Win32 launches |
| `ProcessName` | Optional process name (no `.exe`) used to attach |
| `WindowTitle` | Optional title used to resolve the main window (helpful for Store apps) |
| `AutomationType` | `UIA3` (default, WPF/WinUI/Store) or `UIA2` (some WinForms apps) |
| `LaunchMode` | `Auto` (default), `Launch`, `Attach`, or `StoreApp` |
| `LaunchTimeoutSeconds` | Wait for the main window |
| `CaptureActionScreenshots` | Per-action failure screenshots (default `false`) |

`LaunchMode=Auto`:

1. Attach if `ProcessName` (or the exe stem of `AppId`) is already running.
2. If `AppId` looks like an AUMID (`Publisher.App_hash!App`), call `Application.LaunchStoreApp`.
3. Otherwise launch the executable with FlaUI.

Shipped sample targets Windows Calculator:

```json
"Desktop": {
  "AppId": "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
  "WindowTitle": "Calculator",
  "AutomationType": "UIA3",
  "LaunchMode": "Auto",
  "LaunchTimeoutSeconds": 30
}
```

---

## Session lifecycle

1. `@Desktop` `BeforeScenario` (`DesktopHooks`) calls `DesktopSessionFactory.GetSession()`.
2. Pages/actions search from `session.SearchRoot` (the current window).
3. On failure, a window screenshot is attached to Extent and Allure.
4. `AfterScenario` calls `DesktopSessionFactory.Quit()` which closes the window and disposes UI Automation.

No process is started from `BeforeTestRun`. Web/API/PDF runs never launch a desktop app.

---

## Locators and actions

`LocatorDefinition.AccessibilityId` maps to **UIA AutomationId**. `AutomationId` is an optional alias.

```csharp
var login = new DesktopElement(session, new LocatorDefinition
{
    AccessibilityId = "LoginButton",
    Name = "Login"
});
DesktopActions.Click(login, "Submit login");
```

`DesktopActions` covers click, double-click, right-click, hover, send keys, clear, text/property reads, displayed/enabled/selected, focus, scroll-into-view, drag-and-drop, and highlight. Specialized helpers use FlaUI control patterns (Grid, Tree, Tab, Menu, Window, Dialog, Ribbon, file dialogs, tray).

Retries go through `SmartDesktopActions` / `RetryDecisionEngine` (COM / not-found / offscreen / FlaUI exceptions).

---

## Page objects

Desktop window classes live under `QualiFrame.Framework/Desktop/Pages` (shared samples) or `QualiFrame.Tests/PageObjects` for application-specific screens. Inherit `DesktopPage` and take a `DesktopSession`.

The Calculator sample (`CalculatorPage` + `@Calculator` scenario) is the end-to-end reference: launch via AUMID, click AutomationIds (`num4Button`, `plusButton`, `equalButton`, `CalculatorResults`).

Application-specific desktop screens for a customer AUT belong under `src/QualiFrame.Tests/PageObjects` and should still inherit `DesktopPage`.

---

## Tests

Both scenarios live in `src/QualiFrame.Tests/Features/Desktop.feature` and are tagged `@Desktop` (NUnit `Category=Desktop`). There are no separate `ModularTestCase` desktop tests.

| Scenario | Tags | Bindings | Status with shipped config |
|----------|------|----------|----------------------------|
| Perform addition in Windows Calculator | `@Desktop` `@Calculator` | `CalculatorSteps` → `CalculatorPage` | Runnable. Shipped `Desktop:AppId` is Calculator. |
| Desktop login with valid credentials | `@Desktop` | `DesktopSteps` → `DesktopLoginFlows` → `LoginWindow` / `MainWindow` | Template only. Locators (`UsernameTextBox`, `LoginButton`, `MainContentRoot`) are placeholders, not Calculator controls. |

### Run on Windows

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Desktop"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Calculator"
```

Non-Windows hosts are marked inconclusive by `DesktopHooks`. `@Desktop` / `@Calculator` skip WebDriver startup.

To automate a real login app, set `Desktop:AppId` (and optional `WindowTitle` / `ProcessName`) to that AUT and update the login page locators.

---

## UIA2 vs UIA3

From the [FlaUI project](https://github.com/FlaUI/FlaUI):

- **UIA3** (default) — best for WPF, WinUI, and Store/UWP apps.
- **UIA2** — often more reliable for WinForms. Set `Desktop:AutomationType` to `UIA2`.

FlaUI packages target Windows TFMs (`net8.0-windows` / `net9.0-windows` / `net10.0-windows`). The framework multi-targets `net8.0;net9.0;net10.0` with `EnableWindowsTargeting` so Linux CI can still restore/compile; desktop APIs throw `PlatformNotSupportedException` / inconclusive on non-Windows.
