using System;
using System.Text.RegularExpressions;

namespace QualiFrame.Framework.Utilities.Logging
{
    /// <summary>
    /// Utility class for masking sensitive data in log messages.
    /// </summary>
    public static class SensitiveDataMasker
    {
        private static readonly Regex[] SensitivePatterns = new[]
        {
            new Regex(@"password[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"pwd[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"token[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"api[_-]?key[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"secret[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"access[_-]?key[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"auth[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"authorization[""']?\s*[:=]\s*[""']?(Bearer\s+)?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"apikey[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase),
            new Regex(@"apisecret[""']?\s*[:=]\s*[""']?([^""'\s]+)", RegexOptions.IgnoreCase)
        };

        public static string Mask(string message, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            var maskedMessage = message;

            foreach (var pattern in SensitivePatterns)
            {
                maskedMessage = pattern.Replace(maskedMessage, match =>
                {
                    var value = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
                        return match.Value;

                    var masked = new string('*', value.Length - visibleChars) + value.Substring(value.Length - visibleChars);
                    return match.Value.Replace(value, masked);
                });
            }

            return maskedMessage;
        }
    }
}
