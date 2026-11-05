using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DeviceFarm;
using Amazon.DeviceFarm.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// AWS Device Farm cloud platform implementation.
    /// </summary>
    public class AwsDeviceFarmPlatform : BaseCloudPlatform
    {
        private readonly string _region;
        private readonly AmazonDeviceFarmClient? _deviceFarmClient;

        public override string PlatformName => "AWS Device Farm";

        public AwsDeviceFarmPlatform(string accessKeyId, string secretAccessKey, string region = "us-west-2") 
            : base(accessKeyId, secretAccessKey, $"https://devicefarm.{region}.amazonaws.com")
        {
            _region = region;

            try
            {
                var config = new AmazonDeviceFarmConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
                _deviceFarmClient = new AmazonDeviceFarmClient(accessKeyId, secretAccessKey, config);
                Logger.Information("AWS Device Farm client initialized for region: {Region}", region);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to initialize AWS Device Farm client: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Creates a WebDriver instance for AWS Device Farm.
        /// Note: AWS Device Farm uses a different execution model - tests are uploaded and run on devices.
        /// This method creates a WebDriver for local testing that can be used to prepare tests.
        /// </summary>
        public override IWebDriver CreateWebDriver(string browser, Dictionary<string, object>? options = null)
        {
            Logger.Warning("AWS Device Farm uses a different execution model. " +
                "Tests should be uploaded via API. Creating local driver for test preparation.");

            var driverOptions = CreateDriverOptions(browser, options);
            return browser.ToLower() switch
            {
                "chrome" => new ChromeDriver((ChromeOptions)driverOptions),
                "firefox" => new FirefoxDriver((FirefoxOptions)driverOptions),
                "edge" => new EdgeDriver((EdgeOptions)driverOptions),
                _ => throw new System.ArgumentException($"Browser '{browser}' is not supported for AWS Device Farm")
            };
        }

        /// <summary>
        /// Uploads a test run to AWS Device Farm (async version).
        /// </summary>
        public async Task<string?> UploadTestRunAsync(string projectArn, string devicePoolArn, string testPackagePath, string? name = null)
        {
            if (_deviceFarmClient == null)
            {
                Logger.Error("AWS Device Farm client is not initialized");
                return null;
            }

            try
            {
                var uploadRequest = new CreateUploadRequest
                {
                    ProjectArn = projectArn,
                    Name = name ?? $"QualiFrame Test {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    Type = UploadType.APPIUM_NODE_TEST_PACKAGE
                };

                var uploadResponse = await _deviceFarmClient.CreateUploadAsync(uploadRequest);
                var uploadArn = uploadResponse.Upload.Arn;

                Logger.Information("Test package uploaded to AWS Device Farm. Upload ARN: {Arn}", uploadArn);

                var scheduleRequest = new ScheduleRunRequest
                {
                    ProjectArn = projectArn,
                    DevicePoolArn = devicePoolArn,
                    Test = new ScheduleRunTest
                    {
                        Type = TestType.APPIUM_NODE,
                        TestPackageArn = uploadArn
                    },
                    Name = name ?? $"QualiFrame Test Run {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
                };

                var scheduleResponse = await _deviceFarmClient.ScheduleRunAsync(scheduleRequest);
                Logger.Information("Test run scheduled on AWS Device Farm. Run ARN: {Arn}", scheduleResponse.Run.Arn);

                return scheduleResponse.Run.Arn;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to upload test run to AWS Device Farm");
                return null;
            }
        }

        protected override ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options)
        {
            return driverOptions.ToCapabilities();
        }

        /// <summary>
        /// Uploads a test run to AWS Device Farm (synchronous wrapper).
        /// </summary>
        public string? UploadTestRun(string projectArn, string devicePoolArn, string testPackagePath, string? name = null)
        {
            return UploadTestRunAsync(projectArn, devicePoolArn, testPackagePath, name).GetAwaiter().GetResult();
        }
    }
}
