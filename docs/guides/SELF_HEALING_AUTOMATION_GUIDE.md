# Self-Healing Test Automation Guide

**QualiFrame C# NUnit Framework v3.1**

---

## 📋 Table of Contents

- [Overview](#overview)
- [What is Self-Healing Automation?](#what-is-self-healing-automation)
- [How It Works](#how-it-works)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [How It Works Internally](#how-it-works-internally)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Limitations](#limitations)
- [Advanced Usage](#advanced-usage)

---

## 🎯 Overview

The QualiFrame framework includes **Healenium** integration for self-healing test automation. This feature automatically fixes broken locators when elements cannot be found, reducing test maintenance overhead and improving test stability.

### Key Benefits

- ✅ **Automatic Locator Recovery**: When an element cannot be found, Healenium attempts to find an alternative locator
- ✅ **Transparent Integration**: No code changes required - works automatically when enabled
- ✅ **Confidence-Based Healing**: Only uses healed locators with high confidence scores (>0.5)
- ✅ **Comprehensive Logging**: Detailed logs for debugging and monitoring
- ✅ **Zero Code Impact**: Existing tests work without modification

---

## 🤔 What is Self-Healing Automation?

Self-healing automation is a technique that automatically fixes broken test locators when elements cannot be found on a web page. Instead of failing immediately, the system:

1. **Detects** when a locator fails to find an element
2. **Analyzes** the page structure and the original locator
3. **Generates** alternative locators using AI/ML algorithms
4. **Validates** the new locator with a confidence score
5. **Uses** the healed locator if confidence is high enough

### Problem It Solves

Traditional test automation fails when:
- Developers change element IDs, classes, or structure
- Dynamic content changes element attributes
- CSS selectors become invalid after UI updates
- XPath expressions break due to DOM changes

Self-healing automation **automatically adapts** to these changes, reducing test maintenance by up to 80%.

---

## 🔧 How It Works

### High-Level Flow

```
┌─────────────────┐
│  Test Execution │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ FindElement(By locator) │
└────────┬────────────────┘
         │
         ▼
    ┌─────────┐
    │ Success? │
    └────┬────┘
         │
    ┌────┴────┐
    │         │
   YES       NO
    │         │
    │         ▼
    │  ┌──────────────────────┐
    │  │ NoSuchElementException│
    │  └──────────┬────────────┘
    │             │
    │             ▼
    │  ┌──────────────────────┐
    │  │ Healenium Enabled?    │
    │  └──────────┬────────────┘
    │             │
    │        ┌────┴────┐
    │        │         │
    │       YES       NO
    │        │         │
    │        │         ▼
    │        │  ┌──────────────┐
    │        │  │ Throw Exception│
    │        │  └──────────────┘
    │        │
    │        ▼
    │  ┌──────────────────────┐
    │  │ Send Healing Request  │
    │  │ to Healenium Backend │
    │  └──────────┬────────────┘
    │             │
    │             ▼
    │  ┌──────────────────────┐
    │  │ Receive Healed Locator│
    │  │ with Confidence Score │
    │  └──────────┬────────────┘
    │             │
    │             ▼
    │  ┌──────────────────────┐
    │  │ Confidence > 0.5?    │
    │  └──────────┬────────────┘
    │             │
    │        ┌────┴────┐
    │        │         │
    │       YES       NO
    │        │         │
    │        │         ▼
    │        │  ┌──────────────┐
    │        │  │ Log Warning   │
    │        │  │ Throw Exception│
    │        │  └──────────────┘
    │        │
    │        ▼
    │  ┌──────────────────────┐
    │  │ Try Healed Locator    │
    │  └──────────┬────────────┘
    │             │
    │        ┌────┴────┐
    │        │         │
    │      Success    Fail
    │        │         │
    │        │         ▼
    │        │  ┌──────────────┐
    │        │  │ Throw Exception│
    │        │  └──────────────┘
    │        │
    │        ▼
    └─── Return Element
```

---

## 🏗️ Architecture

### Components

#### 1. **HealeniumDriverWrapper**
- **Location**: `src/QualiFrame.Framework/Utilities/WebDriver/HealeniumDriverWrapper.cs`
- **Purpose**: Wraps the base WebDriver and intercepts `FindElement`/`FindElements` calls
- **Key Features**:
  - Implements `IWebDriver` interface (transparent wrapper)
  - Catches `NoSuchElementException`
  - Delegates healing to `HealeniumClient`
  - Logs all healing attempts

#### 2. **HealeniumClient**
- **Location**: Internal class in `HealeniumDriverWrapper.cs`
- **Purpose**: Communicates with Healenium backend service
- **Key Features**:
  - HTTP client for backend communication
  - Locator conversion (By → string → By)
  - Confidence threshold validation
  - Error handling and logging

#### 3. **HealeniumConfiguration**
- **Location**: `src/QualiFrame.Framework/Configuration/HealeniumConfiguration.cs`
- **Purpose**: Configuration model for Healenium settings
- **Properties**:
  - `Enabled`: Enable/disable self-healing
  - `BackendUrl`: Healenium backend service URL
  - `TimeoutSeconds`: Request timeout
  - `LogHealingAttempts`: Enable detailed logging
  - `MaxHealingAttempts`: Maximum retry attempts

#### 4. **EnhancedWebDriverFactory**
- **Location**: `src/QualiFrame.Framework/Utilities/WebDriver/EnhancedWebDriverFactory.cs`
- **Purpose**: Automatically wraps drivers with Healenium when enabled
- **Integration Point**: Lines 119-125

### Integration Flow

```
TestConfiguration
    │
    ├── Reads appsettings.json
    │
    ├── Creates HealeniumConfiguration
    │
    └── Passes to EnhancedWebDriverFactory
            │
            ├── Creates base WebDriver (Chrome/Firefox/Edge)
            │
            └── Wraps with HealeniumDriverWrapper (if enabled)
                    │
                    └── Returns wrapped driver to tests
```

---

## 📦 Prerequisites

### 1. Healenium Backend Service

You need a running Healenium backend service. Options:

#### Option A: In-repo Docker Compose (Recommended)

This repository ships an official-style **web** stack (Postgres + `hlm-backend` + selector-imitator) under `healenium/`. QualiFrame talks to the backend HTTP API at `Healenium:BackendUrl` (`http://localhost:7878`). Tests do **not** start these containers.

```bash
cd healenium
docker compose up -d
```

Report UI: `http://localhost:7878/healenium/report`. Stop with `docker compose down`. See `healenium/README.md`.

If you only need the backend image without compose:

```bash
docker pull healenium/hlm-backend:3.5.3
```

That image still needs Postgres and the selector-imitator; prefer the in-repo compose file.

#### Option B: Local Installation

1. Download Healenium backend from [Healenium GitHub](https://github.com/healenium/healenium)
2. Follow installation instructions
3. Start the backend service on port 7878 (default)

#### Option C: Cloud/Remote Service

If you have a remote Healenium instance, configure the URL in `appsettings.json`.

### 2. Verify Backend is Running

```bash
# Check if backend is accessible
curl http://localhost:7878/health

# Or open in browser
# http://localhost:7878
```

### 3. Framework Dependencies

The framework already includes the required dependencies:
- `System.Net.Http.Json` - For HTTP communication with backend
- `System.Text.Json` - For JSON serialization

No additional NuGet packages required!

---

## ⚙️ Configuration

### JSON Configuration (appsettings.json)

Shipped `src/QualiFrame.Framework/Configuration/appsettings.json` already includes this section with **`Enabled` set to `true`**. Start the Healenium backend at `BackendUrl` before browser tests, or set `Enabled` to `false` for local runs without healing. If the key is omitted, the getter falls back to `false`.

```json
{
  "Healenium": {
    "Enabled": true,
    "BackendUrl": "http://localhost:7878",
    "TimeoutSeconds": 10,
    "LogHealingAttempts": true,
    "MaxHealingAttempts": 3,
    "ScoreCap": 0.5,
    "EvidencePath": "Results/HealeniumEvidence"
  }
}
```

Successful runs write versioned baselines under `EvidencePath/baselines`. Healing audits (accepted and rejected) go to `EvidencePath/audits`. Reqnroll promotes a new trusted baseline only after the step passes.

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` (shipped JSON); `false` if key missing | Enable/disable self-healing automation |
| `BackendUrl` | `string` | `http://localhost:7878` | URL of Healenium backend service |
| `ImitatorUrl` | `string` | `http://localhost:8000` | Selector-imitator used as extra candidate source |
| `TimeoutSeconds` | `int` | `10` | Timeout for healing requests (seconds) |
| `LogHealingAttempts` | `bool` | `true` | Log detailed healing attempts to console/logs |
| `MaxHealingAttempts` | `int` | `3` | Maximum number of healing attempts per element |
| `ScoreCap` | `double` | `0.5` | Minimum confidence to accept a healed locator |
| `EvidencePath` | `string` | `Results/HealeniumEvidence` | Versioned locator evidence and healing audits |

### Environment-Specific Configuration

You can override settings per environment:

**appsettings.Development.json**:
```json
{
  "Healenium": {
    "Enabled": true,
    "BackendUrl": "http://localhost:7878",
    "LogHealingAttempts": true
  }
}
```

**appsettings.Production.json**:
```json
{
  "Healenium": {
    "Enabled": true,
    "BackendUrl": "http://healenium-backend.prod:7878",
    "LogHealingAttempts": false
  }
}
```

### Programmatic Configuration

You can also configure Healenium programmatically:

```csharp
var config = new HealeniumConfiguration
{
    Enabled = true,
    BackendUrl = "http://localhost:7878",
    TimeoutSeconds = 15,
    LogHealingAttempts = true,
    MaxHealingAttempts = 5
};

var driver = EnhancedWebDriverFactory.CreateDriver();
var wrappedDriver = new HealeniumDriverWrapper(driver, config);
```

---

## 💡 Usage Examples

### Example 1: Basic Usage (Automatic)

**No code changes required!** When Healenium is enabled in configuration, it works automatically:

```csharp
// In your test or page object
public class LoginPage : ComponentPage
{
    private readonly By _usernameField = By.Id("username");
    private readonly By _passwordField = By.Id("password");
    private readonly By _loginButton = By.Id("login-button");

    public void Login(string username, string password)
    {
        // If element not found, Healenium automatically attempts to heal
        FindElement(_usernameField).SendKeys(username);
        FindElement(_passwordField).SendKeys(password);
        FindElement(_loginButton).Click();
    }
}
```

**What happens:**
1. If `By.Id("username")` fails, Healenium intercepts the exception
2. Sends healing request to backend with page source and original locator
3. Receives healed locator (e.g., `By.CssSelector("#user-name")`)
4. Retries with healed locator
5. Test continues if healing succeeds

### Example 2: With EnhancedWebDriverFactory

```csharp
[Binding]
public class WebSteps
{
    private IWebDriver _driver;
    private LoginPage _loginPage;

    [BeforeScenario]
    public void Setup()
    {
        // Driver is automatically wrapped with Healenium if enabled
        _driver = EnhancedWebDriverFactory.CreateDriver();
        _loginPage = new LoginPage(_driver);
    }

    [When(@"I login with username ""(.*)"" and password ""(.*)""")]
    public void WhenILoginWithCredentials(string username, string password)
    {
        // Self-healing happens automatically if element not found
        _loginPage.Login(username, password);
    }

    [AfterScenario]
    public void Teardown()
    {
        _driver?.Quit();
    }
}
```

### Example 3: Manual Wrapper (Advanced)

If you need more control, you can manually wrap the driver:

```csharp
var baseDriver = new ChromeDriver();
var healeniumConfig = new HealeniumConfiguration
{
    Enabled = true,
    BackendUrl = "http://localhost:7878",
    TimeoutSeconds = 15,
    LogHealingAttempts = true,
    MaxHealingAttempts = 5
};

var driver = new HealeniumDriverWrapper(baseDriver, healeniumConfig);

// Use driver as normal - healing happens automatically
driver.Navigate().GoToUrl("https://example.com");
driver.FindElement(By.Id("search-box")).SendKeys("test");
```

### Example 4: With Page Object Model

```csharp
public class ShoppingCartPage : ComponentPage
{
    // Original locators - may become invalid
    private readonly By _addToCartButton = By.Id("add-to-cart");
    private readonly By _checkoutButton = By.CssSelector(".checkout-btn");
    private readonly By _itemCount = By.XPath("//span[@class='cart-count']");

    public void AddItemToCart()
    {
        // Healenium will heal if locator fails
        FindElement(_addToCartButton).Click();
    }

    public void ProceedToCheckout()
    {
        // Automatic healing on failure
        FindElement(_checkoutButton).Click();
    }

    public int GetCartItemCount()
    {
        var countText = FindElement(_itemCount).Text;
        return int.Parse(countText);
    }
}
```

### Example 5: Reqnroll Feature File

```gherkin
Feature: User Login
  As a user
  I want to login to the application
  So that I can access my account

  @smoke @healing
  Scenario: Login with valid credentials
    Given I navigate to the login page
    When I enter username "testuser" and password "password123"
    And I click the login button
    Then I should be logged in successfully
```

**Step Definitions** (no changes needed):
```csharp
[When(@"I click the login button")]
public void WhenIClickTheLoginButton()
{
    // If button locator is broken, Healenium heals it automatically
    _loginPage.ClickLogin();
}
```

---

## 🔍 How It Works Internally

### Step-by-Step Process

#### 1. **Element Lookup Attempt**

```csharp
// Original call
var element = driver.FindElement(By.Id("username"));
```

#### 2. **Exception Interception**

When `NoSuchElementException` is thrown:

```csharp
catch (NoSuchElementException ex) when (_healingEnabled && _healeniumClient != null)
{
    Logger.Warning("Element not found with locator: {Locator}. Attempting to heal...", locator);
    // Healing process begins
}
```

#### 3. **Healing Request**

The wrapper sends a request to Healenium backend:

```json
{
  "locator": "id=username",
  "pageSource": "<html>...</html>",
  "url": "https://example.com/login",
  "sessionId": "abc123..."
}
```

#### 4. **Backend Processing**

Healenium backend:
- Analyzes the page source
- Identifies similar elements
- Generates alternative locators
- Calculates confidence scores

#### 5. **Healing Response**

Backend returns:

```json
{
  "healedLocator": "css=#user-name",
  "confidence": 0.85,
  "message": "Healed successfully"
}
```

#### 6. **Locator Validation**

```csharp
if (healResponse?.HealedLocator != null && healResponse.Confidence > 0.5)
{
    // Use healed locator
    return _baseDriver.FindElement(ParseLocator(healResponse.HealedLocator));
}
```

#### 7. **Retry with Healed Locator**

```csharp
try
{
    return _baseDriver.FindElement(healedLocator);
}
catch (NoSuchElementException)
{
    Logger.Error("Healed locator also failed. Original locator: {Locator}", locator);
    throw; // Re-throw original exception
}
```

### Locator Conversion

The framework converts between Selenium `By` objects and Healenium string format:

**Selenium → Healenium**:
```csharp
By.Id("username") → "id=username"
By.CssSelector(".btn-primary") → "css=.btn-primary"
By.XPath("//button[@type='submit']") → "xpath=//button[@type='submit']"
```

**Healenium → Selenium**:
```csharp
"id=username" → By.Id("username")
"css=.btn-primary" → By.CssSelector(".btn-primary")
"xpath=//button[@type='submit']" → By.XPath("//button[@type='submit']")
```

---

## ✅ Best Practices

### 1. **Start with Healing Disabled**

Shipped `appsettings.json` has `Healenium:Enabled` set to `true`. When writing new tests, set it to `false` so real locator issues fail instead of being healed:

```json
{
  "Healenium": {
    "Enabled": false
  }
}
```

### 2. **Enable for Maintenance Mode**

Enable healing when:
- Tests are stable and passing
- You want to reduce maintenance overhead
- Dealing with frequently changing UIs

### 3. **Monitor Healing Logs**

Always enable logging to understand what's being healed:

```json
{
  "Healenium": {
    "LogHealingAttempts": true
  }
}
```

Review logs regularly to identify patterns and update locators proactively.

### 4. **Set Appropriate Timeouts**

For slower networks or remote backends:

```json
{
  "Healenium": {
    "TimeoutSeconds": 15
  }
}
```

### 5. **Use Stable Locators First**

Even with healing, prefer stable locators:
- ✅ **Good**: `By.Id("submit-button")` (if ID is stable)
- ✅ **Good**: `By.Name("email")` (if name is stable)
- ⚠️ **Avoid**: `By.XPath("//div[3]/span[2]/button")` (fragile)

### 6. **Combine with Explicit Waits**

Healing works best with proper waits:

```csharp
var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
var element = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("button")));
// Healing will attempt if element not found after wait
```

### 7. **Test Healing in CI/CD**

Include healing-enabled tests in your CI/CD pipeline to catch issues early.

### 8. **Review Healed Locators**

Periodically review which locators are being healed and update your test code with the healed versions for better performance.

### 9. **Use Confidence Thresholds**

The default confidence threshold is `Healenium:ScoreCap` (`0.5`). Candidates below that threshold are rejected, the original failure is preserved, and an audit record is written. A healed locator becomes a new trusted baseline only after the Reqnroll step passes; older versions remain in `History`.

```json
{
  "Healenium": {
    "ScoreCap": 0.7
  }
}
```

### 10. **Handle Healing Failures Gracefully**

Always have proper error handling:

```csharp
try
{
    var element = driver.FindElement(By.Id("button"));
    element.Click();
}
catch (NoSuchElementException ex)
{
    // Log the failure (healing already attempted)
    Logger.Error("Failed to find element even after healing: {Message}", ex.Message);
    // Take screenshot, report, etc.
    throw;
}
```

---

## 🔧 Troubleshooting

### Issue 1: Healing Not Working

**Symptoms**: Tests still fail with `NoSuchElementException` even when healing is enabled.

**Solutions**:

1. **Check Configuration**:
   ```json
   {
     "Healenium": {
       "Enabled": true  // Must be true
     }
   }
   ```

2. **Verify Backend is Running**:
   ```bash
   curl http://localhost:7878/health
   ```

3. **Check Logs**:
   Look for messages like:
   ```
   [INFO] Healenium self-healing enabled. Backend URL: http://localhost:7878
   [WARN] Element not found with locator: id=username. Attempting to heal...
   ```

4. **Verify Driver Wrapping**:
   Ensure you're using `EnhancedWebDriverFactory.CreateDriver()` or manually wrapping with `HealeniumDriverWrapper`.

### Issue 2: Slow Test Execution

**Symptoms**: Tests run slower when healing is enabled.

**Solutions**:

1. **Increase Timeout** (if backend is slow):
   ```json
   {
     "Healenium": {
       "TimeoutSeconds": 5  // Reduce from default 10
     }
   }
   ```

2. **Disable Logging** (for production):
   ```json
   {
     "Healenium": {
       "LogHealingAttempts": false
     }
   }
   ```

3. **Use Healing Selectively**:
   Only enable for tests that need it, not all tests.

### Issue 3: Low Confidence Healing

**Symptoms**: Logs show "Healing returned low confidence" warnings.

**Solutions**:

1. **Check Page Source**: Ensure the page is fully loaded before element lookup.

2. **Verify Element Exists**: The element might genuinely not exist on the page.

3. **Improve Original Locator**: Use more specific locators that are easier to heal.

4. **Review Backend Logs**: Check Healenium backend logs for details.

### Issue 4: Backend Connection Errors

**Symptoms**: Errors like "Failed to connect to Healenium backend".

**Solutions**:

1. **Verify Backend URL**:
   ```json
   {
     "Healenium": {
       "BackendUrl": "http://localhost:7878"  // Check this is correct
     }
   }
   ```

2. **Check Network/Firewall**: Ensure backend is accessible from test machine.

3. **Test Backend Manually**:
   ```bash
   curl -X POST http://localhost:7878/healenium/report/init/test-session \
     -H "Content-Type: application/json"
   ```

### Issue 5: Healed Locator Also Fails

**Symptoms**: Healing succeeds but healed locator also fails.

**Solutions**:

1. **Check Element Visibility**: Element might exist but not be visible.

2. **Add Explicit Waits**: Wait for element to be present/visible before lookup.

3. **Review Page State**: Element might be in a different state than expected.

4. **Check for Dynamic Content**: Element might load after initial page load.

### Issue 6: Healing Works But Tests Are Unstable

**Symptoms**: Tests pass but are flaky.

**Solutions**:

1. **Update Original Locators**: Replace with the healed locators in your code.

2. **Add Explicit Waits**: Don't rely solely on healing - use proper waits.

3. **Review Test Logic**: Ensure tests handle dynamic content properly.

4. **Monitor Healing Frequency**: If healing happens frequently, update locators.

---

## ⚠️ Limitations

### 1. **Backend Dependency**

Healing requires a running Healenium backend service. Tests will fail if backend is unavailable (unless healing is disabled).

### 2. **Performance Impact**

Healing adds overhead:
- Network request to backend
- Backend processing time
- Locator parsing and retry

**Impact**: ~100-500ms per healing attempt

### 3. **Confidence Threshold**

Only locators with confidence > 0.5 are used. Low-confidence healings are rejected.

### 4. **Remote WebDriver Compatibility**

Healing works with:
- ✅ Local WebDriver instances
- ✅ Remote WebDriver (Selenium Grid)
- ⚠️ Some cloud providers (may require session ID handling)

### 5. **Not a Replacement for Good Locators**

Healing is a **safety net**, not a replacement for:
- Stable locators
- Proper waits
- Good test design

### 6. **Limited Locator Types**

Supported locator types:
- ✅ ID
- ✅ Name
- ✅ ClassName
- ✅ CssSelector
- ✅ XPath
- ✅ TagName
- ✅ LinkText
- ✅ PartialLinkText

### 7. **Single Element Healing**

Healing works per element lookup. If multiple elements fail, each is healed independently.

---

## 🚀 Advanced Usage

### Custom Healing Logic

You can extend the healing behavior by creating a custom wrapper:

```csharp
public class CustomHealingWrapper : HealeniumDriverWrapper
{
    public CustomHealingWrapper(IWebDriver baseDriver, HealeniumConfiguration config)
        : base(baseDriver, config)
    {
    }

    public override IWebElement FindElement(By locator)
    {
        try
        {
            return base.FindElement(locator);
        }
        catch (NoSuchElementException ex)
        {
            // Custom logic before healing
            Logger.Information("Custom pre-healing logic");
            
            // Call base healing
            var element = base.FindElement(locator);
            
            // Custom logic after healing
            Logger.Information("Custom post-healing logic");
            
            return element;
        }
    }
}
```

### Healing Metrics Collection

Track healing statistics:

```csharp
public class HealingMetrics
{
    public int TotalHealingAttempts { get; set; }
    public int SuccessfulHealings { get; set; }
    public int FailedHealings { get; set; }
    public Dictionary<string, int> LocatorHealingCounts { get; set; } = new();
}

// In your test framework
var metrics = new HealingMetrics();
// Collect metrics from logs or extend HealeniumClient
```

### Conditional Healing

Enable/disable healing based on conditions:

```csharp
public class ConditionalHealingWrapper : IWebDriver
{
    private readonly HealeniumDriverWrapper _healingWrapper;
    private readonly IWebDriver _baseDriver;
    private readonly Func<bool> _shouldHeal;

    public ConditionalHealingWrapper(IWebDriver baseDriver, HealeniumConfiguration config, Func<bool> shouldHeal)
    {
        _baseDriver = baseDriver;
        _shouldHeal = shouldHeal;
        _healingWrapper = new HealeniumDriverWrapper(baseDriver, config);
    }

    public IWebElement FindElement(By locator)
    {
        if (_shouldHeal())
        {
            return _healingWrapper.FindElement(locator);
        }
        return _baseDriver.FindElement(locator);
    }
    
    // Implement other IWebDriver members...
}
```

### Integration with Reporting

Add healing information to test reports:

```csharp
[AfterScenario]
public void AfterScenario(ScenarioContext scenarioContext)
{
    // Check if healing occurred (from logs or custom tracking)
    if (HealingOccurred)
    {
        ExtentReportManager.LogWarning("Self-healing was used during this scenario");
        AllureApi.AddAttachment("healing-info", "text/plain", Encoding.UTF8.GetBytes(HealingDetails));
    }
}
```

---

## 📚 Additional Resources

### Official Documentation

- [Healenium GitHub](https://github.com/healenium/healenium)
- [Healenium Documentation](https://healenium.io/)

### Framework Documentation

- [QualiFrame README.md](../README.md)
- [Configuration Guide](../README.md#configuration)
- [Best Practices](../README.md#best-practices)

### Related Features

- [Enhanced WebDriver Factory](../README.md#web-automation)
- [Driver Pooling](../README.md#parallel-execution)
- [Logging](../README.md#logging--monitoring)

---

## 📝 Summary

Self-healing automation in QualiFrame:

✅ **Automatic**: Works transparently when enabled  
✅ **Configurable**: Easy to enable/disable via configuration  
✅ **Intelligent**: Uses confidence scores to validate healed locators  
✅ **Logged**: Comprehensive logging for debugging  
✅ **Zero Code Changes**: Existing tests work without modification  

**When to Use**:
- ✅ Stable test suites needing maintenance reduction
- ✅ Frequently changing UIs
- ✅ Large test suites with many locators
- ✅ Maintenance mode (reduce manual locator updates)

**When Not to Use**:
- ❌ New test development (catch real issues first)
- ❌ Performance-critical tests
- ❌ Tests requiring exact locator validation
- ❌ When backend is unavailable

---

**Last Updated**: January 2026  
**Framework Version**: 3.0.0  
**Healenium Integration**: Phase 1 Modernization

---

**Questions or Issues?**  
Please refer to the [Troubleshooting](#troubleshooting) section or create an issue in the repository.
