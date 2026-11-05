namespace QualiFrame.Framework.Configuration
{
    /// <summary>
    /// Exclusive web self-healing backend. Only one wrapper is applied to IWebDriver.
    /// </summary>
    public enum SelfHealingProvider
    {
        None = 0,
        Healenium = 1,
        Qhare = 2
    }

    /// <summary>
    /// Resolves the active healer from <c>SelfHealing:Provider</c>, with Healenium:Enabled as the legacy fallback.
    /// </summary>
    public static class SelfHealingProviderResolver
    {
        public static SelfHealingProvider Resolve(TestConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return Resolve(config.SelfHealingProviderName, config.HealeniumEnabled, config.QhareEnabled);
        }

        public static SelfHealingProvider Resolve(string? provider, bool healeniumEnabled, bool qhareEnabled)
        {
            var raw = provider?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return healeniumEnabled ? SelfHealingProvider.Healenium : SelfHealingProvider.None;
            }

            if (IsOff(raw))
            {
                return SelfHealingProvider.None;
            }

            if (IsQhare(raw))
            {
                return qhareEnabled ? SelfHealingProvider.Qhare : SelfHealingProvider.None;
            }

            if (IsHealenium(raw))
            {
                return healeniumEnabled ? SelfHealingProvider.Healenium : SelfHealingProvider.None;
            }

            return healeniumEnabled ? SelfHealingProvider.Healenium : SelfHealingProvider.None;
        }

        private static bool IsOff(string value) =>
            value.Equals("None", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Off", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Disabled", StringComparison.OrdinalIgnoreCase);

        private static bool IsQhare(string value) =>
            value.Equals("Qhare", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("QHARE", StringComparison.OrdinalIgnoreCase);

        private static bool IsHealenium(string value) =>
            value.Equals("Healenium", StringComparison.OrdinalIgnoreCase);
    }
}
