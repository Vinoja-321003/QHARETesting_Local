using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Web.Components;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Web page implementation with component support.
    /// Can inherit from ComponentPage for component-based POM or BasePage for traditional POM.
    /// </summary>
    public class WebPage : ComponentPage
    {
        private readonly TestConfiguration _config;
        // Cache the working locator for add to cart button to avoid re-detection
        private By? _cachedAddToCartLocator;

        // Locators
        private readonly By _usernameField = By.Id("user-name");
        private readonly By _passwordField = By.Id("password");
        private readonly By _loginButton = By.Id("login-button");
        private readonly By _errorMessage = By.CssSelector("[data-test='error']");
        // Alternative locators for error message (in case structure changes)
        private readonly By _errorMessageAlt1 = By.CssSelector("h3[data-test='error']");
        private readonly By _errorMessageAlt2 = By.XPath("//h3[contains(@data-test, 'error')]");
        private readonly By _errorMessageAlt3 = By.CssSelector(".error-message-container h3");
        private readonly By _inventoryContainer = By.Id("inventory_container");
        private readonly By _shoppingCartBadge = By.ClassName("shopping_cart_badge");
        private readonly By _menuButton = By.Id("react-burger-menu-btn");
        private readonly By _logoutLink = By.Id("logout_sidebar_link");
        private readonly By _backpackLink = By.Id("item_4_title_link");
        private readonly By _addToCartButton = By.Id("add-to-cart-sauce-labs-backpack");
        // Alternative locators for add to cart button (in case ID changes or page structure differs)
        private readonly By _addToCartButtonAlt1 = By.CssSelector("button[data-test='add-to-cart-sauce-labs-backpack']");
        private readonly By _addToCartButtonAlt2 = By.CssSelector("button.btn_inventory");
        private readonly By _addToCartButtonAlt3 = By.XPath("//button[contains(text(), 'Add to cart')]");
        private readonly By _inventoryItemDetail = By.ClassName("inventory_details");
        private readonly By _removeButton = By.Id("remove-sauce-labs-backpack");
        private readonly By _shoppingCartLink = By.ClassName("shopping_cart_link");
        private readonly By _checkoutButton = By.Id("checkout");
        private readonly By _firstNameField = By.Id("first-name");
        private readonly By _lastNameField = By.Id("last-name");
        private readonly By _postalCodeField = By.Id("postal-code");
        private readonly By _continueButton = By.Id("continue");
        private readonly By _finishButton = By.Id("finish");
        private readonly By _thankYouMessage = By.ClassName("complete-header");

        public WebPage() : base()
        {
            _config = TestConfiguration.Instance;
        }

        // Removed constructor with IWebDriver parameter to prevent Reqnroll from trying to inject it
        // BasePage already gets IWebDriver from EnhancedWebDriverFactory.GetDriver()
        // This constructor was unused and caused DI resolution errors for API tests

        public override void WaitForPageToLoad()
        {
            // OPTIMIZED: Check for login page first (faster, no timeout)
            // Login page has username field, home page has inventory container
            // This prevents 10-second timeout when waiting for non-existent header on login page
            try
            {
                // Fast check: Are we on login page? (username field exists)
                if (IsElementPresent(_usernameField))
                {
                    WaitForElementToBeVisible(_usernameField, timeoutSeconds: 5);
                    return;
                }
                
                // Are we on home page? (inventory container exists)
                if (IsElementPresent(_inventoryContainer))
                {
                    WaitForElementToBeVisible(_inventoryContainer, timeoutSeconds: 5);
                    // Try header component if available (non-blocking)
                    try
                    {
                        Header.WaitForVisible(timeoutSeconds: 3);
                    }
                    catch
                    {
                        // Header not critical, continue
                    }
                    return;
                }
                
                // Fallback: Wait for any visible element
                WaitForElementToBeVisible(_usernameField, timeoutSeconds: 5);
            }
            catch
            {
                // Final fallback: Just wait for page load state
                WaitForPageLoad();
            }
        }

        public override bool IsPageLoaded()
        {
            // OPTIMIZED: Fast check without waiting
            // Check login page first (most common case)
            if (IsElementDisplayed(_usernameField))
            {
                return true;
            }
            
            // Check home page
            if (IsElementDisplayed(_inventoryContainer))
            {
                return true;
            }
            
            // Fallback to header component check (non-blocking)
            try
            {
                return Header.IsVisible();
            }
            catch
            {
                return false;
            }
        }

        public void LaunchApp()
        {
            NavigateToUrl(_config.BaseUrl);
            // OPTIMIZED: Use fast page load check
            WaitForPageToLoad();
        }

        public void SwitchTabIssue()        {
            NavigateToUrl("https://the-internet.herokuapp.com/windows");
            Thread.Sleep(2000);
            string parentwindow = Driver.CurrentWindowHandle;
            ClickElement(By.XPath("//*[text()='Click Here']"));
            Thread.Sleep(5000);
            closeCurrentDriverMoveToParentWindow(parentwindow);
            Thread.Sleep(3000);
        }

        public void EnterUserName(string username)
        {
            SendKeys(_usernameField, username);
        }

        public void EnterPassword(string password)
        {
            SendKeys(_passwordField, password);
        }

        public void ClickLoginButton()
        {
            ClickElement(_loginButton);
            // OPTIMIZED: No blocking sleep - let explicit waits handle timing
        }

        public void VerifyLogin()
        {
            // Ensure the page has finished loading (important after logout/navigation)
            WaitForPageLoad();

            // Verification with a slightly higher timeout to handle slower logout redirects
            WaitForElementToBeVisible(_usernameField, timeoutSeconds: 10);
            if (!IsElementDisplayed(_usernameField))
            {
                throw new Exception("Login page is not displayed");
            }
        }

        public void VerifyHomePage()
        {
            // OPTIMIZED: Fast verification with reduced timeout
            WaitForElementToBeVisible(_inventoryContainer, timeoutSeconds: 10);
            if (!IsElementDisplayed(_inventoryContainer))
            {
                throw new Exception("Home page is not displayed");
            }
        }

        public void VerifyErrorMessage()
        {
            // ROBUST: Wait for page to stabilize after login attempt
            WaitForPageLoad();
            
            // Small delay to allow error message to appear (it's rendered dynamically)
            System.Threading.Thread.Sleep(500);
            
            // Try multiple locators with retry logic
            bool elementFound = false;
            int maxRetries = 3;
            int baseTimeout = 5;
            By[] locatorsToTry = { _errorMessage, _errorMessageAlt1, _errorMessageAlt2, _errorMessageAlt3 };
            By workingLocator = _errorMessage;
            
            for (int i = 0; i < maxRetries; i++)
            {
                foreach (var locator in locatorsToTry)
                {
                    try
                    {
                        WaitForElementToBePresent(locator, timeoutSeconds: baseTimeout * (i + 1));
                        elementFound = true;
                        workingLocator = locator;
                        Logger.Debug("Error message found using locator: {Locator}", locator);
                        break;
                    }
                    catch
                    {
                        // Try next locator
                        continue;
                    }
                }
                
                if (elementFound)
                {
                    break;
                }
                
                if (i < maxRetries - 1)
                {
                    Logger.Debug("Error message not found on attempt {Attempt}. Current URL: {Url}. Retrying...", 
                        i + 1, GetCurrentUrl());
                    System.Threading.Thread.Sleep(500);
                }
            }
            
            if (!elementFound)
            {
                // Provide detailed error message
                string pageSource = Driver.PageSource.Length > 500 ? Driver.PageSource.Substring(0, 500) + "..." : Driver.PageSource;
                throw new Exception($"Error message not found after {maxRetries} attempts with {locatorsToTry.Length} different locators. " +
                    $"Current URL: {GetCurrentUrl()}. " +
                    $"Page title: {Driver.Title}. " +
                    $"This may indicate that valid credentials were used instead of invalid ones, or the error message element structure has changed.");
            }
            
            // Verify element is visible
            WaitForElementToBeVisible(workingLocator, timeoutSeconds: 5);
            if (!IsElementDisplayed(workingLocator))
            {
                throw new Exception("Error message element exists but is not visible");
            }
        }

        public void ClickSauceLabBackPackButton()
        {
            ClickElement(_backpackLink);
            
            // OPTIMIZED: Fast navigation wait - only wait for URL change with shorter timeout
            try
            {
                WaitForUrlToContain("inventory-item", timeoutSeconds: 5);
                Logger.Debug("Successfully navigated to product detail page. URL: {Url}", GetCurrentUrl());
            }
            catch
            {
                // URL might not change in SPA, continue with minimal page load wait
                Logger.Debug("URL did not change to inventory-item, continuing. Current URL: {Url}", GetCurrentUrl());
            }
            
            // OPTIMIZED: Minimal wait - just ensure page is interactive
            WaitForPageLoad();
            // Removed Thread.Sleep - let explicit waits handle timing
        }

        public void VerifySauceLabBackPackPage()
        {
            // PERFORMANCE OPTIMIZED: Fast element detection with minimal waits
            // Use cached locator if available, otherwise detect quickly
            
            // Step 1: Quick page load check (non-blocking if already loaded)
            try
            {
                WaitForPageLoad();
            }
            catch
            {
                // Page might already be loaded, continue
            }
            
            // Step 2: Fast element detection - try cached locator first, then alternatives
            By[] locatorsToTry;
            if (_cachedAddToCartLocator != null)
            {
                // Use cached locator first for speed
                locatorsToTry = new[] { _cachedAddToCartLocator, _addToCartButton, _addToCartButtonAlt1, _addToCartButtonAlt2, _addToCartButtonAlt3 };
            }
            else
            {
                // Try most specific locators first for better performance
                locatorsToTry = new[] { _addToCartButton, _addToCartButtonAlt1, _addToCartButtonAlt2, _addToCartButtonAlt3 };
            }
            
            bool elementFound = false;
            By workingLocator = _addToCartButton;
            
            // OPTIMIZED: Single pass with short timeout (2s) - most elements appear quickly
            foreach (var locator in locatorsToTry)
            {
                try
                {
                    // Fast check: element exists and is visible (combined wait)
                    WaitForElementToBeVisible(locator, timeoutSeconds: 2);
                    elementFound = true;
                    workingLocator = locator;
                    _cachedAddToCartLocator = locator; // Cache for future use
                    Logger.Debug("Add to cart button found using locator: {Locator}", locator);
                    break;
                }
                catch
                {
                    // Try next locator
                    continue;
                }
            }
            
            // OPTIMIZED: Only retry once if first attempt failed (with slightly longer timeout)
            if (!elementFound)
            {
                foreach (var locator in locatorsToTry)
                {
                    try
                    {
                        WaitForElementToBeVisible(locator, timeoutSeconds: 5);
                        elementFound = true;
                        workingLocator = locator;
                        _cachedAddToCartLocator = locator; // Cache for future use
                        Logger.Debug("Add to cart button found on retry using locator: {Locator}", locator);
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            
            if (!elementFound)
            {
                throw new Exception($"Add to cart button not found. Current URL: {GetCurrentUrl()}. " +
                    $"Page title: {Driver.Title}.");
            }
            
            // Final verification
            if (!IsElementDisplayed(workingLocator))
            {
                throw new Exception($"Sauce Lab Backpack page is not displayed - Add to cart button not visible. Current URL: {GetCurrentUrl()}");
            }
        }

        public void ClickAddToCartButton()
        {
            // PERFORMANCE OPTIMIZED: Use cached locator from verification for instant click
            // If no cache, try most likely locators first with minimal timeout
            
            By locatorToUse;
            
            // Use cached locator if available (from VerifySauceLabBackPackPage)
            if (_cachedAddToCartLocator != null)
            {
                locatorToUse = _cachedAddToCartLocator;
                Logger.Debug("Using cached add to cart locator: {Locator}", locatorToUse);
            }
            else
            {
                // Fallback: try most specific locators first
                locatorToUse = _addToCartButton;
            }
            
            try
            {
                // OPTIMIZED: Fast click with minimal wait (element should already be visible from verification)
                WaitForElementToBeClickable(locatorToUse, timeoutSeconds: 2);
                ClickElement(locatorToUse);
                Logger.Debug("Successfully clicked add to cart button using locator: {Locator}", locatorToUse);
            }
            catch (Exception ex)
            {
                // Fallback: try alternative locators if cached one fails
                Logger.Debug("Cached locator failed, trying alternatives: {Error}", ex.Message);
                By[] fallbackLocators = { _addToCartButton, _addToCartButtonAlt1, _addToCartButtonAlt2, _addToCartButtonAlt3 };
                bool clicked = false;
                
                foreach (var locator in fallbackLocators)
                {
                    if (locator.Equals(locatorToUse)) continue; // Skip already tried
                    
                    try
                    {
                        WaitForElementToBeClickable(locator, timeoutSeconds: 2);
                        ClickElement(locator);
                        _cachedAddToCartLocator = locator; // Update cache
                        clicked = true;
                        Logger.Debug("Successfully clicked add to cart button using fallback locator: {Locator}", locator);
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
                
                if (!clicked)
                {
                    throw new Exception($"Failed to click add to cart button. Current URL: {GetCurrentUrl()}. " +
                        $"Original error: {ex.Message}");
                }
            }
        }

        public void ClickCartButton()
        {
            ClickElement(_shoppingCartLink);
            WaitForPageLoad();
        }

        public void VerifySauceLabBackPackProduct()
        {
            // Wait for page to be ready first
            WaitForPageLoad();
            
            // Now try to find the element with standard timeout
            WaitForElementToBeVisible(_removeButton, 15);
            if (!IsElementDisplayed(_removeButton))
            {
                throw new Exception("Sauce Lab Backpack product is not in cart");
            }
        }

        public void ClickRemoveButton()
        {
            ClickElement(_removeButton);
        }

        public void VerifyNoProductInCart()
        {
            if (IsElementDisplayed(_removeButton))
            {
                throw new Exception("Product is still in cart");
            }
        }

        public void ClickTopMenuButton()
        {
            ClickElement(_menuButton);
        }

        public void ClickAllItemButton()
        {
            // This would be implemented based on the actual menu structure
            // For now, we'll navigate back to the inventory page
            NavigateToUrl(_config.BaseUrl + "/inventory.html");
        }

        public void ClickSauceLabBikeLightButton()
        {
            // PERFORMANCE OPTIMIZED: Fast navigation similar to ClickSauceLabBackPackButton
            var bikeLightLink = By.Id("item_0_title_link");
            ClickElement(bikeLightLink);
            
            // OPTIMIZED: Fast navigation wait with shorter timeout
            try
            {
                WaitForUrlToContain("inventory-item", timeoutSeconds: 5);
                Logger.Debug("Successfully navigated to bike light product detail page. URL: {Url}", GetCurrentUrl());
            }
            catch
            {
                Logger.Debug("URL did not change to inventory-item, continuing. Current URL: {Url}", GetCurrentUrl());
            }
            
            // OPTIMIZED: Minimal wait
            WaitForPageLoad();
            // Removed Thread.Sleep - let explicit waits handle timing
        }

        public void VerifySauceLabBikeLightPage()
        {
            // PERFORMANCE OPTIMIZED: Fast element detection similar to VerifySauceLabBackPackPage
            
            // Step 1: Quick page load check (non-blocking if already loaded)
            try
            {
                WaitForPageLoad();
            }
            catch
            {
                // Page might already be loaded, continue
            }
            
            // Step 2: Fast element detection - try most specific locators first
            var bikeLightAddToCart = By.Id("add-to-cart-sauce-labs-bike-light");
            var bikeLightAddToCartAlt1 = By.CssSelector("button[data-test='add-to-cart-sauce-labs-bike-light']");
            var bikeLightAddToCartAlt2 = By.CssSelector("button.btn_inventory");
            var bikeLightAddToCartAlt3 = By.XPath("//button[contains(text(), 'Add to cart')]");
            
            By[] locatorsToTry = { bikeLightAddToCart, bikeLightAddToCartAlt1, bikeLightAddToCartAlt2, bikeLightAddToCartAlt3 };
            bool elementFound = false;
            By workingLocator = bikeLightAddToCart;
            
            // OPTIMIZED: Single pass with short timeout (2s) - most elements appear quickly
            foreach (var locator in locatorsToTry)
            {
                try
                {
                    // Fast check: element exists and is visible (combined wait)
                    WaitForElementToBeVisible(locator, timeoutSeconds: 2);
                    elementFound = true;
                    workingLocator = locator;
                    Logger.Debug("Bike light add to cart button found using locator: {Locator}", locator);
                    break;
                }
                catch
                {
                    // Try next locator
                    continue;
                }
            }
            
            // OPTIMIZED: Only retry once if first attempt failed (with slightly longer timeout)
            if (!elementFound)
            {
                foreach (var locator in locatorsToTry)
                {
                    try
                    {
                        WaitForElementToBeVisible(locator, timeoutSeconds: 5);
                        elementFound = true;
                        workingLocator = locator;
                        Logger.Debug("Bike light add to cart button found on retry using locator: {Locator}", locator);
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            
            if (!elementFound)
            {
                throw new Exception($"Bike light add to cart button not found. Current URL: {GetCurrentUrl()}. " +
                    $"Page title: {Driver.Title}.");
            }
            
            // Final verification
            if (!IsElementDisplayed(workingLocator))
            {
                throw new Exception($"Sauce Lab Bike Light page is not displayed - Add to cart button not visible. Current URL: {GetCurrentUrl()}");
            }
        }

        public void ClickCheckOutButton()
        {
            ClickElement(_checkoutButton);
        }

        public void EnterCheckoutInformation()
        {
            SendKeys(_firstNameField, "John");
            SendKeys(_lastNameField, "Doe");
            SendKeys(_postalCodeField, "12345");
        }

        public void ClickContinueButton()
        {
            ClickElement(_continueButton);
        }

        public void ClickFinishButton()
        {
            ClickElement(_finishButton);
        }

        public void VerifyThankYouOrderPage()
        {
            WaitForElementToBeVisible(_thankYouMessage);
            if (!IsElementDisplayed(_thankYouMessage))
            {
                throw new Exception("Thank you page is not displayed");
            }
        }

        public void ClickLogOutButton()
        {
            ClickElement(_logoutLink);
        }

        public string GetErrorMessage()
        {
            return GetText(_errorMessage);
        }

        public new string GetPageTitle()
        {
            return Driver.Title;
        }

        public new string GetCurrentUrl()
        {
            return Driver.Url;
        }

        public new void TakeScreenshot(string fileName)
        {
            base.TakeScreenshot(fileName);
        }

        public void WaitForElementToLoad(By locator)
        {
            WaitForElementToBeVisible(locator);
        }

        public void WaitForElementToDisappear(By locator)
        {
            WaitForElementToDisappear(locator);
        }

        public new bool IsElementPresent(By locator)
        {
            return base.IsElementPresent(locator);
        }

        public new void ScrollToElement(By locator)
        {
            base.ScrollToElement(locator);
        }

        public new void RefreshPage()
        {
            base.RefreshPage();
        }

        public new void GoBack()
        {
            base.GoBack();
        }

        public new void GoForward()
        {
            base.GoForward();
        }

        public new void SwitchToWindow(string windowHandle)
        {
            base.SwitchToWindow(windowHandle);
        }

        public new void SwitchToDefaultContent()
        {
            base.SwitchToDefaultContent();
        }

        public new void SwitchToFrame(int frameIndex)
        {
            base.SwitchToFrame(frameIndex);
        }

        public new void SwitchToFrame(string frameName)
        {
            base.SwitchToFrame(frameName);
        }

        public new void SwitchToFrame(IWebElement frameElement)
        {
            base.SwitchToFrame(frameElement);
        }

        public new void SwitchToParentFrame()
        {
            base.SwitchToParentFrame();
        }

        public new void AcceptAlert()
        {
            base.AcceptAlert();
        }

        public new void DismissAlert()
        {
            base.DismissAlert();
        }

        public new string GetAlertText()
        {
            return base.GetAlertText();
        }

        public new void SendKeysToAlert(string text)
        {
            base.SendKeysToAlert(text);
        }

        public new void ExecuteJavaScript(string script, params object[] args)
        {
            base.ExecuteJavaScript(script, args);
        }

        public new T ExecuteJavaScript<T>(string script, params object[] args)
        {
            return base.ExecuteJavaScript<T>(script, args);
        }

        public new void WaitForPageLoad()
        {
            base.WaitForPageLoad();
        }

        public new void WaitForAjax()
        {
            base.WaitForAjax();
        }

       
    }
}
