using QualiFrame.Framework.Configuration;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// Factory for creating cloud platform instances based on configuration.
    /// </summary>
    public static class CloudPlatformFactory
    {
        /// <summary>
        /// Gets the active cloud platform based on configuration priority.
        /// Priority order: BrowserStack > LambdaTest > SauceLabs > Perfecto > Experitest > AWS Device Farm
        /// </summary>
        /// <param name="config">Test configuration instance.</param>
        /// <returns>Active cloud platform instance or null if none is enabled.</returns>
        public static ICloudPlatform? GetActivePlatform(TestConfiguration config)
        {
            // Priority order: BrowserStack > LambdaTest > SauceLabs > Perfecto > Experitest > AWS Device Farm
            if (config.BrowserStackEnabled)
            {
                if (!string.IsNullOrWhiteSpace(config.BrowserStackUsername) && 
                    !string.IsNullOrWhiteSpace(config.BrowserStackAccessKey))
                {
                    return new BrowserStackPlatform(config.BrowserStackUsername, config.BrowserStackAccessKey);
                }
            }

            if (config.LambdaTestEnabled)
            {
                if (!string.IsNullOrWhiteSpace(config.LambdaTestUsername) && 
                    !string.IsNullOrWhiteSpace(config.LambdaTestAccessKey))
                {
                    return new LambdaTestPlatform(config.LambdaTestUsername, config.LambdaTestAccessKey);
                }
            }

            if (config.SauceLabsEnabled)
            {
                if (!string.IsNullOrWhiteSpace(config.SauceLabsUsername) && 
                    !string.IsNullOrWhiteSpace(config.SauceLabsAccessKey))
                {
                    return new SauceLabsPlatform(config.SauceLabsUsername, config.SauceLabsAccessKey, config.SauceLabsRegion);
                }
            }

            if (config.PerfectoEnabled)
            {
                if (!string.IsNullOrWhiteSpace(config.PerfectoUsername) && 
                    !string.IsNullOrWhiteSpace(config.PerfectoAccessKey) &&
                    !string.IsNullOrWhiteSpace(config.PerfectoCloudName))
                {
                    return new PerfectoPlatform(config.PerfectoUsername, config.PerfectoAccessKey, config.PerfectoCloudName);
                }
            }

            if (config.ExperitestEnabled)
            {
                if (!string.IsNullOrWhiteSpace(config.ExperitestUsername) && 
                    !string.IsNullOrWhiteSpace(config.ExperitestAccessKey))
                {
                    return new ExperitestPlatform(config.ExperitestUsername, config.ExperitestAccessKey);
                }
            }

            if (config.AwsDeviceFarmEnabled)
            {
                if (!string.IsNullOrWhiteSpace(config.AwsDeviceFarmAccessKeyId) && 
                    !string.IsNullOrWhiteSpace(config.AwsDeviceFarmSecretAccessKey))
                {
                    return new AwsDeviceFarmPlatform(config.AwsDeviceFarmAccessKeyId, config.AwsDeviceFarmSecretAccessKey, config.AwsDeviceFarmRegion);
                }
            }

            return null;
        }
    }
}
