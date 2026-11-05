# Page Objects

This directory contains Page Object Model (POM) implementations.

## Overview

The framework supports both traditional and component-based Page Object Model patterns.

## Classes

### BasePage

Abstract base class for all page objects. Provides:
- Common element interaction methods
- Wait strategies
- JavaScript execution
- Screenshot capabilities
- Window/frame/alert handling
- Navigation methods

**Usage:**
```csharp
public class LoginPage : BasePage
{
    private readonly By _usernameField = By.Id("username");
    
    public override void WaitForPageToLoad()
    {
        WaitForElementToBeVisible(_usernameField);
    }
    
    public override bool IsPageLoaded()
    {
        return IsElementDisplayed(_usernameField);
    }
    
    public void EnterUsername(string username)
    {
        SendKeys(_usernameField, username);
    }
}
```

### ComponentPage

Base class for component-based Page Object Model. Provides:
- Header component
- Footer component
- Navigation component
- Form component creation

**Usage:**
```csharp
public class HomePage : ComponentPage
{
    public override void WaitForPageToLoad()
    {
        Header.WaitForVisible();
    }
    
    public override bool IsPageLoaded()
    {
        return Header.IsVisible();
    }
    
    public void ClickLogo()
    {
        Header.ClickLogo();
    }
    
    public void FillSearchForm(string query)
    {
        var searchForm = CreateForm(
            By.Id("search-form"),
            By.Id("search-button")
        );
        searchForm.FillField("query", query);
        searchForm.Submit();
    }
}
```

### MobileAppScreen

Named locators for sample mobile scenarios. Drivers must be created through `AppiumDriverFactory`.

### WebPage

Concrete implementation for SauceDemo application. Demonstrates:
- Component-based POM usage
- Element-based interactions
- Page verification methods

## Best Practices

1. **Inheritance:** Inherit from `BasePage` for traditional POM or `ComponentPage` for component-based POM
2. **Locators:** Use `readonly By` fields for locators
3. **Methods:** Create public methods for user actions
4. **Verification:** Implement `WaitForPageToLoad()` and `IsPageLoaded()` methods
5. **Components:** Use components for reusable UI patterns
6. **Error Handling:** Let exceptions propagate or handle gracefully

## Component-Based vs Traditional POM

### Traditional POM
- Direct element access
- Page-specific methods
- Good for simple pages

### Component-Based POM
- Reusable components
- Better code organization
- Easier maintenance
- Recommended for complex applications

## Examples

See `WebPage.cs` for a complete example of component-based Page Object Model.

Desktop window samples (`CalculatorPage`, `LoginWindow`, `MainWindow`) live under `src/QualiFrame.Framework/Desktop/Pages` and inherit `DesktopPage`. Put application-specific desktop screens in this `PageObjects` folder (or a subfolder) and still inherit `DesktopPage`. Do not add AUT screens under `PageBases`. See [DESKTOP_AUTOMATION.md](../../../docs/guides/DESKTOP_AUTOMATION.md).

