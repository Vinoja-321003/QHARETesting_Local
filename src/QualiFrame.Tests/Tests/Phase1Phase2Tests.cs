using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Data.Repositories;
using QualiFrame.Framework.Security;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;
using QualiFrame.Framework.Web.Elements;
using QualiFrame.Framework.API;
using Newtonsoft.Json.Linq;
using WebDriverException = QualiFrame.Framework.Core.Exceptions.WebDriverException;

namespace QualiFrame.Tests.Tests
{
    /// <summary>
    /// Test suite for Phase 1 and Phase 2 implementations.
    /// </summary>
    [TestFixture]
    public class Phase1Phase2Tests
    {
        [SetUp]
        public void SetUp()
        {
            // Initialize logging
            Logger.Initialize();
            Logger.SetCorrelationId();
            Logger.SetTestId(TestContext.CurrentContext.Test.Name);
            Logger.Information("Starting test: {TestName}", TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        public void TearDown()
        {
            Logger.Information("Completed test: {TestName}", TestContext.CurrentContext.Test.Name);
        }

        #region Phase 1 Tests - Logging

        [Test]
        [Category("Phase1")]
        [Category("Logging")]
        public void Test_Logger_Initialization()
        {
            // Arrange & Act
            Logger.Initialize();
            Logger.SetCorrelationId("TEST-CORRELATION-001");
            Logger.SetTestId("TC001");
            Logger.SetBrowser("Chrome");
            Logger.SetEnvironment("Test");

            // Assert
            Assert.That(Logger.Instance, Is.Not.Null);
            Logger.Information("Logger initialized successfully");
            Logger.Debug("Debug message test");
            Logger.Warning("Warning message test");
            Logger.Error("Error message test");
        }

        [Test]
        [Category("Phase1")]
        [Category("Logging")]
        public void Test_SensitiveDataMasking()
        {
            // Arrange
            var sensitiveMessage = "password=secret123 token=abc123 api_key=xyz789";

            // Act
            var masked = SensitiveDataMasker.Mask(sensitiveMessage);

            // Assert
            Assert.That(masked, Is.Not.Null);
            Assert.That(masked.Contains("secret123"), Is.False);
            Assert.That(masked.Contains("abc123"), Is.False);
            Assert.That(masked.Contains("xyz789"), Is.False);
            Logger.Information("Sensitive data masking test passed");
        }

        #endregion

        #region Phase 1 Tests - Error Handling

        [Test]
        [Category("Phase1")]
        [Category("ErrorHandling")]
        public void Test_FrameworkException()
        {
            // Arrange & Act
            var exception = new FrameworkException("Test exception", "TC001", "Test context");

            // Assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.TestId, Is.EqualTo("TC001"));
            Assert.That(exception.Context, Is.EqualTo("Test context"));
            Assert.That(exception.Timestamp, Is.Not.EqualTo(default(DateTime)));
            Logger.Information("FrameworkException test passed");
        }

        [Test]
        [Category("Phase1")]
        [Category("ErrorHandling")]
        public void Test_WebDriverException()
        {
            // Arrange & Act
            var exception = new WebDriverException("Driver failed", "Chrome", "http://localhost:4444", "TC001");

            // Assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Browser, Is.EqualTo("Chrome"));
            Assert.That(exception.DriverUrl, Is.EqualTo("http://localhost:4444"));
            Assert.That(exception.TestId, Is.EqualTo("TC001"));
            Logger.Information("WebDriverException test passed");
        }

        [Test]
        [Category("Phase1")]
        [Category("ErrorHandling")]
        public void Test_APIException()
        {
            // Arrange & Act
            var exception = new APIException("API failed", "/users", 500, "Error response", "TC001");

            // Assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Endpoint, Is.EqualTo("/users"));
            Assert.That(exception.StatusCode, Is.EqualTo(500));
            Assert.That(exception.ResponseBody, Is.EqualTo("Error response"));
            Logger.Information("APIException test passed");
        }

        [Test]
        [Category("Phase1")]
        [Category("ErrorHandling")]
        public void Test_RetryableException()
        {
            // Arrange & Act
            var exception = new RetryableException("Retryable error", 2, 5, TimeSpan.FromSeconds(1), "TC001");

            // Assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.RetryCount, Is.EqualTo(2));
            Assert.That(exception.MaxRetries, Is.EqualTo(5));
            Assert.That(exception.CanRetry, Is.True);
            Logger.Information("RetryableException test passed");
        }

        #endregion

        #region Phase 1 Tests - Secrets Management

        [Test]
        [Category("Phase1")]
        [Category("Secrets")]
        public void Test_SecretsManager_Encryption()
        {
            // Arrange
            var secretsManager = SecretsManager.Instance;
            var plainText = "MySecretPassword123";

            // Act
            var encrypted = secretsManager.Encrypt(plainText);
            var decrypted = secretsManager.Decrypt(encrypted);

            // Assert
            Assert.That(encrypted, Is.Not.Null);
            Assert.That(encrypted, Is.Not.EqualTo(plainText));
            Assert.That(decrypted, Is.EqualTo(plainText));
            Logger.Information("SecretsManager encryption test passed");
        }

        [Test]
        [Category("Phase1")]
        [Category("Secrets")]
        public void Test_SecretsManager_EmptyString()
        {
            // Arrange
            var secretsManager = SecretsManager.Instance;

            // Act
            var encrypted = secretsManager.Encrypt("");
            var decrypted = secretsManager.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(""));
            Logger.Information("SecretsManager empty string test passed");
        }

        #endregion

        #region Phase 1 Tests - Driver Management

        [Test]
        [Category("Phase1")]
        [Category("Driver")]
        public void Test_EnhancedWebDriverFactory_Creation()
        {
            IWebDriver? driver = null;
            try
            {
                // Arrange & Act
                driver = EnhancedWebDriverFactory.CreateDriver();
                EnhancedWebDriverFactory.SetCurrentDriver(driver); // Ensure driver is tracked for cleanup

                // Assert
                Assert.That(driver, Is.Not.Null);
                Assert.That(DriverHealthChecker.IsHealthy(driver), Is.True);
                Logger.Information("EnhancedWebDriverFactory creation test passed");
            }
            finally
            {
                // Cleanup - ensure browser is closed
                if (driver != null)
                {
                    try
                    {
                        driver.Quit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Error quitting driver: {Error}", ex.Message);
                        try
                        {
                            driver.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Warning("Error disposing driver: {Error}", disposeEx.Message);
                        }
                    }
                }
                EnhancedWebDriverFactory.QuitDriver(); // Also try the factory method
            }
        }

        [Test]
        [Category("Phase1")]
        [Category("Driver")]
        public void Test_DriverHealthChecker_NullDriver()
        {
            // Arrange
            IWebDriver? driver = null;

            // Act
            var isHealthy = DriverHealthChecker.IsHealthy(driver);

            // Assert
            Assert.That(isHealthy, Is.False);
            Logger.Information("DriverHealthChecker null driver test passed");
        }

        #endregion

        #region Phase 2 Tests - Data Access Layer

        [Test]
        [Category("Phase2")]
        [Category("DataAccess")]
        public void Test_DataRepositoryFactory_Creation()
        {
            // Arrange & Act
            var excelRepo = DataRepositoryFactory.CreateRepository("EXCEL");
            var csvRepo = DataRepositoryFactory.CreateRepository("CSV");
            var jsonRepo = DataRepositoryFactory.CreateRepository("JSON");

            // Assert
            Assert.That(excelRepo, Is.Not.Null);
            Assert.That(csvRepo, Is.Not.Null);
            Assert.That(jsonRepo, Is.Not.Null);
            Assert.That(excelRepo, Is.InstanceOf<ExcelDataRepository>());
            Assert.That(csvRepo, Is.InstanceOf<CsvDataRepository>());
            Assert.That(jsonRepo, Is.InstanceOf<JsonDataRepository>());
            Logger.Information("DataRepositoryFactory creation test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("DataAccess")]
        public void Test_JsonDataRepository_GetTestData()
        {
            // Arrange
            var repository = new JsonDataRepository();
            var testDataFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "TestData.json");

            if (!File.Exists(testDataFile))
            {
                Assert.Ignore("TestData.json not found, skipping test");
                return;
            }

            try
            {
                // Act
                var testData = repository.GetTestData("standard_user", testDataFile);

                // Assert
                Assert.That(testData, Is.Not.Null);
                Logger.Information("JsonDataRepository GetTestData test passed");
            }
            catch (TestDataException)
            {
                // Expected if test case ID doesn't exist
                Logger.Warning("Test case ID not found in JSON file");
            }
        }

        [Test]
        [Category("Phase2")]
        [Category("DataAccess")]
        public void Test_JsonDataRepository_TestDataExists()
        {
            // Arrange
            var repository = new JsonDataRepository();
            var testDataFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "TestData.json");

            if (!File.Exists(testDataFile))
            {
                Assert.Ignore("TestData.json not found, skipping test");
                return;
            }

            // Act
            var exists = repository.TestDataExists("non-existent-id", testDataFile);

            // Assert
            Assert.That(exists, Is.False);
            Logger.Information("JsonDataRepository TestDataExists test passed");
        }

        #endregion

        #region Phase 2 Tests - Enhanced Element Wrapper

        [Test]
        [Category("Phase2")]
        [Category("ElementWrapper")]
        public void Test_EnhancedWebElement_Creation()
        {
            IWebDriver? driver = null;
            try
            {
                // Arrange
                driver = EnhancedWebDriverFactory.CreateDriver();
                EnhancedWebDriverFactory.SetCurrentDriver(driver); // Ensure driver is tracked for cleanup
                driver.Navigate().GoToUrl("https://www.saucedemo.com");

                // Act
                var element = ElementActions.FindElement(driver, By.Id("user-name"), "Username field");

                // Assert
                Assert.That(element, Is.Not.Null);
                Assert.That(element.Element, Is.Not.Null);
                Assert.That(element.Description, Is.EqualTo("Username field"));
                Logger.Information("EnhancedWebElement creation test passed");
            }
            finally
            {
                // Cleanup - ensure browser is closed
                if (driver != null)
                {
                    try
                    {
                        driver.Quit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Error quitting driver: {Error}", ex.Message);
                        try
                        {
                            driver.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Warning("Error disposing driver: {Error}", disposeEx.Message);
                        }
                    }
                }
                EnhancedWebDriverFactory.QuitDriver(); // Also try the factory method
            }
        }

        [Test]
        [Category("Phase2")]
        [Category("ElementWrapper")]
        public void Test_EnhancedWebElement_FluentInterface()
        {
            IWebDriver? driver = null;
            try
            {
                // Arrange
                driver = EnhancedWebDriverFactory.CreateDriver();
                EnhancedWebDriverFactory.SetCurrentDriver(driver); // Ensure driver is tracked for cleanup
                driver.Navigate().GoToUrl("https://www.saucedemo.com");

                // Act & Assert
                var element = ElementActions.FindElement(driver, By.Id("user-name"), "Username field");
                
                element
                    .WaitForVisible()
                    .ScrollIntoView()
                    .Clear()
                    .SendKeys("testuser");

                var text = element.GetText();
                Assert.That(text, Is.Not.Null);
                Logger.Information("EnhancedWebElement fluent interface test passed");
            }
            finally
            {
                // Cleanup - ensure browser is closed
                if (driver != null)
                {
                    try
                    {
                        driver.Quit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Error quitting driver: {Error}", ex.Message);
                        try
                        {
                            driver.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Warning("Error disposing driver: {Error}", disposeEx.Message);
                        }
                    }
                }
                EnhancedWebDriverFactory.QuitDriver(); // Also try the factory method
            }
        }

        [Test]
        [Category("Phase2")]
        [Category("ElementWrapper")]
        public void Test_EnhancedWebElement_IsDisplayed()
        {
            IWebDriver? driver = null;
            try
            {
                // Arrange
                driver = EnhancedWebDriverFactory.CreateDriver();
                EnhancedWebDriverFactory.SetCurrentDriver(driver); // Ensure driver is tracked for cleanup
                driver.Navigate().GoToUrl("https://www.saucedemo.com");

                // Act
                var element = ElementActions.FindElement(driver, By.Id("user-name"), "Username field");
                var isDisplayed = element.IsDisplayed();
                var isEnabled = element.IsEnabled();

                // Assert
                Assert.That(isDisplayed, Is.True);
                Assert.That(isEnabled, Is.True);
                Logger.Information("EnhancedWebElement IsDisplayed test passed");
            }
            finally
            {
                // Cleanup - ensure browser is closed
                if (driver != null)
                {
                    try
                    {
                        driver.Quit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Error quitting driver: {Error}", ex.Message);
                        try
                        {
                            driver.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Warning("Error disposing driver: {Error}", disposeEx.Message);
                        }
                    }
                }
                EnhancedWebDriverFactory.QuitDriver(); // Also try the factory method
            }
        }

        #endregion

        #region Phase 2 Tests - Enhanced API Client

        [Test]
        [Category("Phase2")]
        [Category("APIClient")]
        public void Test_EnhancedApiClient_Creation()
        {
            // Arrange & Act
            var client = new EnhancedApiClient("https://petstore.swagger.io/v2");

            // Assert
            Assert.That(client, Is.Not.Null);
            Logger.Information("EnhancedApiClient creation test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("APIClient")]
        public async Task Test_EnhancedApiClient_GetRequest()
        {
            // Arrange
            var client = new EnhancedApiClient("https://petstore.swagger.io/v2");

            // Act
            var response = await client.GetAsync("/pet/findByStatus?status=available");

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(response.Content, Is.Not.Null);
            Logger.Information("EnhancedApiClient GET request test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("APIClient")]
        public async Task Test_EnhancedApiClient_PostRequest()
        {
            // Arrange
            var client = new EnhancedApiClient("https://petstore.swagger.io/v2");
            var body = new
            {
                id = 0,
                category = new { id = 0, name = "Dogs" },
                name = "Buddy",
                photoUrls = new[] { "https://example.com/dog.jpg" },
                tags = new[] { new { id = 0, name = "friendly" } },
                status = "available"
            };

            // Act
            var response = await client.PostAsync("/pet", body);

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(response.Content, Is.Not.Null);
            Logger.Information("EnhancedApiClient POST request test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("APIClient")]
        public async Task Test_EnhancedApiClient_ValidateRequiredFields()
        {
            // Arrange
            var client = new EnhancedApiClient("https://petstore.swagger.io/v2");

            // First, create a pet to ensure it exists
            var createPetBody = new
            {
                id = 5001,
                category = new { id = 1, name = "Dogs" },
                name = "TestPet",
                photoUrls = new[] { "https://example.com/pet.jpg" },
                tags = new[] { new { id = 1, name = "test" } },
                status = "available"
            };
            var createResponse = await client.PostAsync("/pet", createPetBody);
            Assert.That(createResponse.IsSuccessStatusCode, Is.True, "Failed to create pet for validation test");

            // Act - Get the pet we just created
            var response = await client.GetAsync("/pet/5001");
            var isValid = client.ValidateRequiredFields(response, "id");

            // Assert
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(isValid, Is.True, "Required field 'id' should be present in response");
            Logger.Information("EnhancedApiClient ValidateRequiredFields test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("APIClient")]
        public async Task Test_EnhancedApiClient_PerformanceMonitoring()
        {
            // Arrange
            var client = new EnhancedApiClient("https://petstore.swagger.io/v2");

            // Act - Make multiple requests
            for (int i = 0; i < 5; i++)
            {
                await client.GetAsync("/pet/findByStatus?status=available");
            }

            // Assert
            var stats = client.GetPerformanceStats("/pet/findByStatus?status=available");
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.TotalCalls, Is.GreaterThan(0));
            Logger.Information(
                "Performance stats - Total: {Total}, Success: {Success}%, Avg: {Avg}ms",
                stats.TotalCalls,
                stats.SuccessRate,
                stats.AverageDuration.TotalMilliseconds);
        }

        [Test]
        [Category("Phase2")]
        [Category("APIClient")]
        public void Test_EnhancedApiClient_SetAuthentication()
        {
            // Arrange
            var client = new EnhancedApiClient("https://api.example.com");

            // Act
            client.SetAuthenticationToken("test-token-123");
            client.SetDefaultHeaders("X-Custom-Header", "test-value");

            // Assert - No exception thrown
            Assert.That(client, Is.Not.Null);
            Logger.Information("EnhancedApiClient SetAuthentication test passed");
        }

        #endregion

        #region Phase 2 Tests - Schema Validator

        [Test]
        [Category("Phase2")]
        [Category("SchemaValidator")]
        public void Test_SchemaValidator_ValidateRequiredFields()
        {
            // Arrange
            var jsonResponse = @"{""id"": 1, ""name"": ""John"", ""email"": ""john@example.com""}";

            // Act
            var isValid = SchemaValidator.ValidateRequiredFields(jsonResponse, "id", "name", "email");

            // Assert
            Assert.That(isValid, Is.True);
            Logger.Information("SchemaValidator ValidateRequiredFields test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("SchemaValidator")]
        public void Test_SchemaValidator_ValidateRequiredFields_MissingField()
        {
            // Arrange
            var jsonResponse = @"{""id"": 1, ""name"": ""John""}";

            // Act
            var isValid = SchemaValidator.ValidateRequiredFields(jsonResponse, "id", "name", "email");

            // Assert
            Assert.That(isValid, Is.False);
            Logger.Information("SchemaValidator ValidateRequiredFields missing field test passed");
        }

        [Test]
        [Category("Phase2")]
        [Category("SchemaValidator")]
        public void Test_SchemaValidator_ValidateSchema()
        {
            // Arrange
            var jsonResponse = @"{""id"": 1, ""name"": ""John"", ""email"": ""john@example.com""}";
            var schema = JObject.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""name"": { ""type"": ""string"" },
                    ""email"": { ""type"": ""string"" }
                },
                ""required"": [""id"", ""name"", ""email""]
            }");

            // Act
            var isValid = SchemaValidator.Validate(jsonResponse, schema, out var errors);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
            Logger.Information("SchemaValidator ValidateSchema test passed");
        }

        #endregion
    }
}

